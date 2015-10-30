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
        private List<Tuple<string, GameObserver>> _players = new List<Tuple<string, GameObserver>>();
        private int _currentPlayerId;
        private int[,] _boardGridMarks = new int[3, 3];
        private List<PlacePosition> _movePositions = new List<PlacePosition>();

        public GameActor(ClusterNodeContext clusterContext, long id)
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
                PlayerNames = _players.Select(p => p.Item1).ToList(),
                FirstMovePlayerId = 1,
                Positions = _movePositions
            };
        }

        private void NotifyToAllObservers(Action<GameObserver> notifyAction)
        {
            // _logger.TraceFormat("Notify {0}", notifyAction.Method.Name);
            foreach (var player in _players)
            {
                if (player.Item2 != null)
                    notifyAction(player.Item2);
            }
        }

        private int GetPlayerId(string userId)
        {
            var index = _players.FindIndex(p => p.Item1 == userId);
            if (index == -1)
                throw new ResultException(ResultCodeType.NeedToBeInGame);
            return index + 1;
        }

        private void BeginGame()
        {
            if (_state != GameState.WaitingForPlayers)
                return;

            _state = GameState.Playing;
            _currentPlayerId = 1;

            NotifyToAllObservers(o => o.Begin(_currentPlayerId));
        }

        private void EndGame(int winnerPlayerId)
        {
            if (_state != GameState.Playing)
                return;

            _state = GameState.Ended;
            _currentPlayerId = 0;

            NotifyToAllObservers(o => o.End(winnerPlayerId));
        }

        [ExtendedHandler]
        GameInfo Join(string userId, IGameObserver observer)
        {
            if (_state != GameState.WaitingForPlayers)
                throw new ResultException(ResultCodeType.GameStarted);

            if (_players.Count > 2)
                throw new ResultException(ResultCodeType.GamePlayerFull);

            var playerId = _players.Count + 1;
            NotifyToAllObservers(o => o.Join(playerId, userId));

            _players.Add(Tuple.Create(userId, (GameObserver)observer));

            if (_players.Count == 1)
                RunTask(() => Join("bot", null));

            if (_players.Count == 2)
                RunTask(() => BeginGame());

            return GetGameInfo();
        }

        [ExtendedHandler]
        void Leave(string userId)
        {
            var playerId = GetPlayerId(userId);

            var player = _players[playerId - 1];
            _players[playerId - 1] = Tuple.Create(player.Item1, (GameObserver)null);

            NotifyToAllObservers(o => o.Leave(playerId));

            if (_state != GameState.Ended)
            {
                // TODO: STATE
                _state = GameState.Aborted;
                NotifyToAllObservers(o => o.Abort());
            }

            if (_players.Count(p => p.Item2 != null) == 0)
            {
                _clusterContext.GameDirectory.WithNoReply().RemoveGame(_id);
                Self.Tell(InterfacedPoisonPill.Instance);
            }
        }

        [ExtendedHandler]
        void MakeMove(PlacePosition pos, string playerUserId)
        {
            var playerId = GetPlayerId(playerUserId);
            if (playerId != _currentPlayerId)
                throw new ResultException(ResultCodeType.NotYourTurn);

            if (pos.X < 0 || pos.X >= 3 || pos.Y < 0 || pos.Y >= 3 || _boardGridMarks[pos.X, pos.Y] != 0)
                throw new ResultException(ResultCodeType.BadPosition);

            _boardGridMarks[pos.X, pos.Y] = _currentPlayerId;
            _movePositions.Add(pos);

            NotifyToAllObservers(o => o.MakeMove(_currentPlayerId, pos));

            var matched = Logic.FindMatchedRow(_boardGridMarks);
            if (matched != null)
            {
                EndGame(_currentPlayerId);
                return;
            }

            if (_movePositions.Count < 9)
            {
                // give a turn to another player
                _currentPlayerId = 3 - _currentPlayerId;

                // bot move
                if (_currentPlayerId == 2)
                {
                    var newPos = Logic.DetermineMove(_boardGridMarks, 2);
                    if (newPos != null)
                    {
                        RunTask(async () =>
                        {
                            await Task.Delay(1000);
                            MakeMove(newPos, "bot");
                        });
                    }
                }
            }
            else
            {
                // end of game. draw ?
                EndGame(0);
            }
        }

        [ExtendedHandler]
        void Say(string msg, string playerUserId)
        {
            // TODO:
        }
    }
}
