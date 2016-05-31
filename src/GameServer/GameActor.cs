using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Utility;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain;

namespace GameServer
{
    [Log(LogFilterTarget.Request)]
    [ResponsiveException(typeof(ResultException))]
    public class GameActor : InterfacedActor, IExtendedInterface<IGame, IGamePlayer>
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
            public GameUserObserver ObserverForUserActor;
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

        private void NotifyToAllObservers(Action<int, GameObserver> notifyAction)
        {
            for (var i = 0; i < _players.Count; i++)
            {
                if (_players[i].Observer != null)
                    notifyAction(i + 1, _players[i].Observer);
            }
        }

        private void NotifyToAllObserversForUserActor(Action<int, GameUserObserver> notifyAction)
        {
            for (var i = 0; i < _players.Count; i++)
            {
                if (_players[i].ObserverForUserActor != null)
                    notifyAction(i + 1, _players[i].ObserverForUserActor);
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

            NotifyToAllObservers((id, o) => o.Begin(_currentPlayerId));
            NotifyToAllObserversForUserActor((id, o) => o.Begin(_id));
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

            NotifyToAllObservers((id, o) => o.End(winnerPlayerId));

            if (winnerPlayerId == 0)
            {
                NotifyToAllObserversForUserActor(
                    (id, o) => o.End(_id, GameResult.Draw));
            }
            else
            {
                NotifyToAllObserversForUserActor(
                    (id, o) => o.End(_id, id == winnerPlayerId ? GameResult.Win : GameResult.Lose));
            }

            if (_turnTimeout != null)
            {
                _turnTimeout.Cancel();
                _turnTimeout = null;
            }
        }

        [ExtendedHandler]
        private Tuple<int, GameInfo> Join(long userId, string userName, IGameObserver observer, IGameUserObserver observerForUserActor)
        {
            if (_state != GameState.WaitingForPlayers)
                throw new ResultException(ResultCodeType.GameStarted);

            if (_players.Count > 2)
                throw new ResultException(ResultCodeType.GamePlayerFull);

            var playerId = _players.Count + 1;
            NotifyToAllObservers((id, o) => o.Join(playerId, userId, userName));

            _players.Add(new Player
            {
                UserId = userId,
                UserName = userName,
                Observer = (GameObserver)observer,
                ObserverForUserActor = (GameUserObserver)observerForUserActor,
            });

            if (_players.Count == 2)
                RunTask(() => BeginGame());

            return Tuple.Create(playerId, GetGameInfo());
        }

        [ExtendedHandler]
        private void Leave(long userId)
        {
            var playerId = GetPlayerId(userId);

            var player = _players[playerId - 1];
            _players[playerId - 1].Observer = null;

            NotifyToAllObservers((id, o) => o.Leave(playerId));

            if (_state != GameState.Ended)
            {
                // TODO: STATE
                _state = GameState.Aborted;
                NotifyToAllObservers((id, o) => o.Abort());
                NotifyToAllObserversForUserActor((id, o) => o.End(_id, GameResult.None));
            }

            if (_players.Count(p => p.Observer != null) == 0)
            {
                Self.Tell(InterfacedPoisonPill.Instance);
            }
        }

        [ExtendedHandler]
        private void MakeMove(PlacePosition pos, long playerUserId)
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

        private void MakeMove(PlacePosition pos)
        {
            _boardGridMarks[pos.X, pos.Y] = _currentPlayerId;
            _movePositions.Add(pos);

            var matchedRow = Logic.FindMatchedRow(_boardGridMarks);
            var drawed = _movePositions.Count >= Rule.BoardSize * Rule.BoardSize;
            var nextTurnPlayerId = (matchedRow == null && drawed == false) ? 3 - _currentPlayerId : 0;

            NotifyToAllObservers((id, o) => o.MakeMove(_currentPlayerId, pos, nextTurnPlayerId));

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
        private void Say(string msg, long playerUserId)
        {
            // TODO:
        }
    }
}
