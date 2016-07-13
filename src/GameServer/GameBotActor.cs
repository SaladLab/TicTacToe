using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain;

namespace GameServer
{
    [Log]
    public class GameBotActor : InterfacedActor, IGameObserver
    {
        private ILog _logger;
        private GameRef _game;
        private long _userId;
        private string _userName;
        private GamePlayerRef _gamePlayer;
        private int _playerId;
        private int[,] _boardGridMarks = new int[Rule.BoardSize, Rule.BoardSize];
        private bool _isPlaying;

        public GameBotActor(ClusterNodeContext clusterContext,
                            GameRef game, long userId, string userName)
        {
            _logger = LogManager.GetLogger($"GameBotActor({userId}, {userName})");
            _game = game;
            _userId = userId;
            _userName = userName;
        }

        protected override async Task OnStart(bool restarted)
        {
            try
            {
                var observer = CreateObserver<IGameObserver>();
                var ret = await _game.Join(_userId, _userName, observer, null);
                _gamePlayer = _game.Cast<GamePlayerRef>().WithRequestWaiter(this);
                _playerId = ret.Item1;
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Failed to join game({0})", e, _game.CastToIActorRef().Path);
                Self.Tell(InterfacedPoisonPill.Instance);
            }
        }

        protected override async Task OnGracefulStop()
        {
            try
            {
                if (_gamePlayer != null)
                    await _game.Leave(_userId);
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Failed to leave game({0})", e, _game.CastToIActorRef().Path);
            }
        }

        private async Task ThinkAndMakeMoveAsync()
        {
            await Task.Delay(1000);

            // It's required to check isPlaying. Because this function could be called by IGameObserver.MakeMove.
            // But when game is finished, IGameObserver.End will be called after MakeMove notification.
            // To prevent call MakeMove after game ended, this check should be done.
            if (_isPlaying == false)
                return;

            var newPos = Logic.DetermineMove(_boardGridMarks, _playerId);
            _gamePlayer.WithNoReply().MakeMove(newPos, _userId);
        }

        void IGameObserver.Join(int playerId, long userId, string userName)
        {
        }

        void IGameObserver.Leave(int playerId)
        {
        }

        void IGameObserver.Begin(int currentPlayerId)
        {
            _logger.TraceFormat("Game begun");

            _isPlaying = true;

            if (currentPlayerId == _playerId)
                RunTask(() => ThinkAndMakeMoveAsync());
        }

        void IGameObserver.MakeMove(int playerId, PlacePosition pos, int nextTurnPlayerId)
        {
            _boardGridMarks[pos.X, pos.Y] = playerId;
            if (_isPlaying && nextTurnPlayerId == _playerId)
                RunTask(() => ThinkAndMakeMoveAsync());
        }

        void IGameObserver.Say(int playerId, string msg)
        {
        }

        void IGameObserver.End(int winnerPlayerId)
        {
            _logger.TraceFormat("Game ended and I will stop");

            _isPlaying = false;
            Self.Tell(InterfacedPoisonPill.Instance);
        }

        void IGameObserver.Abort()
        {
            _logger.TraceFormat("Game aborted and I will stop");

            _isPlaying = false;
            Self.Tell(InterfacedPoisonPill.Instance);
        }
    }
}
