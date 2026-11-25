using Gameplay.Enums;
using Gameplay.Tutorial.States.Shop.Box;
using Gameplay.Windows.Gameplay;
using Infrastructure.Events;
using Infrastructure.Windows;
using UnityEngine;

namespace Gameplay.Tutorial.States.SwipeState
{
    public class SwipeTutorialState : TutorialState
    {
        // 原来 Enter 里打开引导层那行被注释掉了，第一步引导完全没有 UI，
        // 玩家看到的就是“没有引导、直接进游戏”。这里补回来。
        private const string WAITING_MESSAGE = "准备出发";
        private const string SWIPE_MESSAGE = "滑动屏幕，指挥僵尸突围";

        private readonly Vector2 _messagePosition = new(0, 600);

        private IGameplayModel _gameplayModel;

        public SwipeTutorialState(ITutorialService tutorialService, IWindowService windowService,
            IEventsManager eventsManager, IGameplayModel gameplayModel, ETutorialState state = ETutorialState.Swipe)
            : base(tutorialService, windowService, eventsManager, state)
        {
            _gameplayModel = gameplayModel;
        }

        public override void Enter()
        {
            base.Enter();

            _gameplayModel.OnToTheNextWave += WaveCompleted;
            _gameplayModel.OnStartWave += OnStartWave;

            Debug.Log($"[引导] 进入 Swipe 状态，已在波次中={_gameplayModel.IsStartWave}");

            ShowPanel(_gameplayModel.IsStartWave ? SWIPE_MESSAGE : WAITING_MESSAGE);
        }

        public override void Exit()
        {
            base.Exit();

            _gameplayModel.OnToTheNextWave -= WaveCompleted;
            _gameplayModel.OnStartWave -= OnStartWave;

            if (_windowService.IsOpen(WindowType.Tutorial))
            {
                _windowService.Close(WindowType.Tutorial);
            }
        }

        private void OnStartWave(ERegionType regionType, int waveIndex)
        {
            ShowPanel(SWIPE_MESSAGE);
        }

        private void ShowPanel(string message)
        {
            if (!_windowService.IsOpen(WindowType.Tutorial))
            {
                _windowService.Open(WindowType.Tutorial);
            }

            _tutorialService.ShowMessage(message, _messagePosition, false);
        }

        private void WaveCompleted(ERegionType regionType, int waveIndex)
        {
            if (regionType == ERegionType.Awakening && waveIndex == 1)
            {
                _stateMachine.Enter<ShopBoxTutorialState>();
            }
        }
    }
}
