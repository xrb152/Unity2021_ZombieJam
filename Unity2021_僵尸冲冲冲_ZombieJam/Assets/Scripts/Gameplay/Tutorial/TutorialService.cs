using System;
using DG.Tweening;
using Gameplay.Cards;
using Gameplay.Enums;
using Gameplay.Shop;
using Gameplay.Tutorial.States;
using Gameplay.Tutorial.States.Card;
using Gameplay.Tutorial.States.Shop;
using Gameplay.Tutorial.States.Shop.Box;
using Gameplay.Tutorial.States.SwipeState;
using Gameplay.Windows.Gameplay;
using Infrastructure.Events;
using Infrastructure.PersistenceProgress;
using Infrastructure.StateMachine;
using Infrastructure.Windows;
using UnityEngine;

namespace Gameplay.Tutorial
{
    public class TutorialService : ITutorialService
    {
        public event Action<ETutorialState> СhangedState;
        public event Action<EUnitClass> OnOpenCardPopUp;
        public event Action<string, Vector2, bool> Message; 

        private readonly StateMachine _stateMachine = new();
        private readonly IProgressService _progressService;
        private readonly IWindowService _windowService;
        private readonly IShopModel _shopModel;
        private readonly ICardsModel _cardsModel;
        private readonly IEventsManager _eventsManager;
        private IGameplayModel _gameplayModel;

        public TutorialService(IProgressService progressService, IWindowService windowService,
            IShopModel shopModel, ICardsModel cardsModel, IEventsManager eventsManager)
        {
            _windowService = windowService;
            _progressService = progressService;
            _shopModel = shopModel;
            _cardsModel = cardsModel;
            _eventsManager = eventsManager;
        }

        public ETutorialState CurrentState => _progressService.PlayerProgress.CurrentTutorialState;

        public void Initialize(IGameplayModel gameplayModel)
        {
            _gameplayModel = gameplayModel;
            SetState(_progressService.PlayerProgress.CurrentTutorialState);
            InitializeStates();
        }

        public void SetState(ETutorialState tutorialState)
        {
            if (tutorialState == CurrentState) return;

            _progressService.PlayerProgress.CurrentTutorialState = tutorialState;
            СhangedState?.Invoke(tutorialState);
        }

        private void InitializeStates()
        {
            if (CurrentState == ETutorialState.Completed) return;

            var swipe = new SwipeTutorialState(this, _windowService, _eventsManager, _gameplayModel);
            var shopBoxTutorialState = new ShopBoxTutorialState(this, _windowService, _shopModel, _eventsManager);
            // var shopCurrency = new ShopCurrencyTutorialState(this, _windowService, _shopModel, _eventsManager);
            var startCardTutorialState = new StartCardTutorialState(this, _windowService, _cardsModel, _eventsManager);
            var finishCard = new FinishCardTutorialState(this, _windowService, _eventsManager);
            var completed = new CompletedTutorialState(this, _windowService, _eventsManager);
            var play = new PlayButtonTutorialState(this, _windowService, _eventsManager, _gameplayModel);
            _stateMachine.AddState(swipe);
            _stateMachine.AddState(shopBoxTutorialState);
            //  _stateMachine.AddState(shopCurrency);
            _stateMachine.AddState(startCardTutorialState);
            _stateMachine.AddState(finishCard);
            _stateMachine.AddState(play);
            _stateMachine.AddState(completed);

            // 原来这里是 async void + await Task.Delay(250)。
            // 在 WebGL / 抖音小游戏里，await 的 continuation 依赖 SynchronizationContext，
            // 有概率永远不恢复 —— 于是 RunState() 不执行，状态机根本没启动。
            // 表现就是：CurrentState 明明是 Swipe，但整个游戏里一点引导都没有。
            // 换成 DOTween 的计时，它由主循环驱动，不吃这套。
            Debug.Log($"[引导] 初始化完成，250ms 后进入状态 {CurrentState}");

            DOVirtual.DelayedCall(0.25f, RunState);
        }

        private void RunState()
        {
            Debug.Log($"[引导] RunState 进入 {CurrentState}");

            try
            {
                switch (CurrentState)
                {
                    case ETutorialState.Swipe:
                        _stateMachine.Enter<SwipeTutorialState>();
                        break;
                    case ETutorialState.ShopBox:
                        _stateMachine.Enter<ShopBoxTutorialState>();
                        break;
                    case ETutorialState.StartCard:
                        _stateMachine.Enter<StartCardTutorialState>();
                        break;
                    case ETutorialState.FinishCard:
                        _stateMachine.Enter<FinishCardTutorialState>();
                        break;
                    case ETutorialState.PlayButton:
                        _stateMachine.Enter<PlayButtonTutorialState>();
                        break;
                    case ETutorialState.Completed:
                        Debug.Log("[引导] 已是完成态，不再启动");
                        break;
                    default:
                        // ShopCurrency 状态已从状态机移除（AddState 被注释），
                        // 存档若停在这里，再 Enter 会抛「状态未注册」，改成走商店第一步。
                        Debug.LogWarning($"[引导] 状态 {CurrentState} 没有对应实现，回退到 ShopBox");
                        _stateMachine.Enter<ShopBoxTutorialState>();
                        break;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[引导] 进入状态 {CurrentState} 失败：{exception}");
            }
        }


        public void OpenCardPopUp(EUnitClass unitClass)
        {
            OnOpenCardPopUp?.Invoke(unitClass);
        }

        public void StartFinishCardTutorial()
        {
            _stateMachine.Enter<FinishCardTutorialState>();
        }

        public void ShowMessage(string message, Vector2 messagePosition, bool isActiveBg)
        {
            Message?.Invoke(message, messagePosition,isActiveBg);
        }

        public PlayerProgress GetPlayerProgress()
        {
            return _progressService.PlayerProgress;
        }
    }
}