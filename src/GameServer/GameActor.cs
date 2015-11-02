using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Common.Logging;
using Domain.Game;
using Domain.Interfaced;

namespace GameServer
{
    [Log]
    public class GameActor : InterfacedActor<GameActor>, IExtendedInterface<IGame, IGamePlayer>
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private long _id;
        private GameState _state;

        private class Player
        {
            public long UserId;
            public string UserName;
            public GameObserver Observer;
        }

        private List<Player> _players = new List<Player>();

        private int _currentPlayerId;
        private int[,] _boardGridMarks = new int[Rule.BoardSize, Rule.BoardSize];
        private List<PlacePosition> _movePositions = new List<PlacePosition>();
        private ICancelable _turnTimeout;

        public GameActor(ClusterNodeContext clusterContext, long id, CreateGameParam param)
        {
            _logger = LogManager.GetLogger($"GameActor({id})");
            _clusterContext = clusterContext;
            _id = id;
        }

        private GameInfo GetGameInfo()
        {
            return new GameInfo
            {
                Id = _id,
                State = _state,
                PlayerNames = _players.Select(p => p.UserName).ToList(),
                FirstMovePlayerId = 1,
                Positions = _movePositions
            };
        }

        private void NotifyToAllObservers(Action<GameObserver> notifyAction)
        {
            foreach (var player in _players)
            {
                if (player.Observer != null)
                    notifyAction(player.Observer);
            }
        }

        private int GetPlayerId(long userId)
        {
            var index = _players.FindIndex(p => p.UserId == userId);
            if (index == -1)
                throw new ResultException(ResultCodeType.NeedToBeInGame);
            return index + 1;
        }

        private void BeginGame()
        {
            if (_state != GameState.WaitingForPlayers)
                return;

            _state = GameState.Playing;
            _currentPlayerId = new Random().Next(1, 3);

            ScheduleTurnTimeout(_movePositions.Count);

            NotifyToAllObservers(o => o.Begin(_currentPlayerId));
        }

        private class TurnTimeout
        {
            public int Turn;
        }

        private void ScheduleTurnTimeout(int turn)
        {
            if (_turnTimeout != null)
            {
                _turnTimeout.Cancel();
                _turnTimeout = null;
            }

            _turnTimeout = Context.System.Scheduler.ScheduleTellOnceCancelable(
                (int)Rule.TurnTimeout.TotalMilliseconds, Self, new TurnTimeout { Turn = turn }, null);
        }

        [MessageHandler]
        private void OnTurnTimeout(TurnTimeout message)
        {
            if (_movePositions.Count > message.Turn)
                return;

            var newPos = Logic.DetermineMove(_boardGridMarks, _currentPlayerId);
            if (newPos != null)
                MakeMove(newPos);
        }

        private void EndGame(int winnerPlayerId)
        {
            if (_state != GameState.Playing)
                return;

            _state = GameState.Ended;
            _currentPlayerId = 0;

            NotifyToAllObservers(o => o.End(winnerPlayerId));

            if (_turnTimeout != null)
            {
                _turnTimeout.Cancel();
                _turnTimeout = null;
            }
        }

        [ExtendedHandler]
        GameInfo Join(long userId, string userName, IGameObserver observer)
        {
            if (_state != GameState.WaitingForPlayers)
                throw new ResultException(ResultCodeType.GameStarted);

            if (_players.Count > 2)
                throw new ResultException(ResultCodeType.GamePlayerFull);

            var playerId = _players.Count + 1;
            NotifyToAllObservers(o => o.Join(playerId, userId, userName));

            _players.Add(new Player { UserId = userId, UserName = userName, Observer = (GameObserver)observer });

            if (_players.Count == 2)
                RunTask(() => BeginGame());

            return GetGameInfo();
        }

        [ExtendedHandler]
        void Leave(long userId)
        {
            var playerId = GetPlayerId(userId);

            var player = _players[playerId - 1];
            _players[playerId - 1].Observer = null;

            NotifyToAllObservers(o => o.Leave(playerId));

            if (_state != GameState.Ended)
            {
                // TODO: STATE
                _state = GameState.Aborted;
                NotifyToAllObservers(o => o.Abort());
            }

            if (_players.Count(p => p.Observer != null) == 0)
            {
                _clusterContext.GameDirectory.WithNoReply().RemoveGame(_id);
                Self.Tell(InterfacedPoisonPill.Instance);
            }
        }

        [ExtendedHandler]
        void MakeMove(PlacePosition pos, long playerUserId)
        {
            var playerId = GetPlayerId(playerUserId);
            if (playerId != _currentPlayerId)
                throw new ResultException(ResultCodeType.NotYourTurn);

            if (pos.X < 0 || pos.X >= Rule.BoardSize ||
                pos.Y < 0 || pos.Y >= Rule.BoardSize ||
                _boardGridMarks[pos.X, pos.Y] != 0)
            {
                throw new ResultException(ResultCodeType.BadPosition);
            }

            MakeMove(pos);
        }

        void MakeMove(PlacePosition pos)
        {
            _boardGridMarks[pos.X, pos.Y] = _currentPlayerId;
            _movePositions.Add(pos);

            var matchedRow = Logic.FindMatchedRow(_boardGridMarks);
            var drawed = _movePositions.Count >= Rule.BoardSize * Rule.BoardSize;
            var nextTurnPlayerId = (matchedRow == null && drawed == false) ? 3 - _currentPlayerId : 0;

            NotifyToAllObservers(o => o.MakeMove(_currentPlayerId, pos, nextTurnPlayerId));

            if (matchedRow != null)
            {
                EndGame(_currentPlayerId);
            }
            else if (drawed)
            {
                EndGame(0);
            }
            else
            {
                ScheduleTurnTimeout(_movePositions.Count);
                _currentPlayerId = nextTurnPlayerId;
            }
        }

        [ExtendedHandler]
        void Say(string msg, long playerUserId)
        {
            // TODO:
        }
    }
}
