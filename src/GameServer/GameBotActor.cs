using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Common.Logging;
using Domain.Game;
using Domain.Interfaced;

namespace GameServer
{
    [Log]
    public class GameBotActor : InterfacedActor<GameBotActor>, IGameObserver
    {
        private ILog _logger;
        private GameRef _game;
        private string _userId;
        private GamePlayerRef _gamePlayer;
        private int _playerId;
        private int[,] _boardGridMarks = new int[Rule.BoardSize, Rule.BoardSize];
        private bool _isPlaying;

        public GameBotActor(ClusterNodeContext clusterContext,
                            GameRef game, string userId)
        {
            _logger = LogManager.GetLogger($"GameBotActor({userId})");
            _game = game;
            _userId = userId;
        }

        protected override async Task OnPreStart()
        {
            try
            {
                var observerId = IssueObserverId();
                AddObserver(observerId, this);
                var info = await _game.Join(_userId, new GameObserver(Self, observerId));
                _gamePlayer = new GamePlayerRef(_game.Actor, this, null);
                _playerId = info.PlayerNames.IndexOf(_userId) + 1;
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Failed to join game({0})", e, _game.Actor.Path);
                Self.Tell(InterfacedPoisonPill.Instance);
            }
        }

        protected override async Task OnPreStop()
        {
            try
            {
                await _game.Leave(_userId);
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Failed to leave game({0})", e, _game.Actor.Path);
            }
        }

        private async Task ThinkAndMakeMoveAsync()
        {
            await Task.Delay(1000);

            var newPos = Logic.DetermineMove(_boardGridMarks, _playerId);
            _gamePlayer.WithNoReply().MakeMove(newPos, _userId);
        }

        void IGameObserver.Join(int playerId, string userId)
        {
        }

        void IGameObserver.Leave(int playerId)
        {
        }

        void IGameObserver.Begin(int playerId)
        {
            _logger.TraceFormat("Game begun");

            _isPlaying = true;

            if (playerId == _playerId)
                RunTask(() => ThinkAndMakeMoveAsync());
        }

        void IGameObserver.MakeMove(int playerId, PlacePosition pos)
        {
            _boardGridMarks[pos.X, pos.Y] = playerId;
            if (_isPlaying && playerId != _playerId)
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
