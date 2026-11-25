using System;
using System.Collections;
using Gameplay.CinemachineCamera;
using Gameplay.Tutorial;
using Gameplay.Windows.Gameplay;
using Infrastructure.Windows;
using UnityEngine;
using UnityEngine.UI;
using Utils.ZenjectInstantiateUtil;
using Zenject;

namespace Gameplay.Windows
{
    public class PlayButton : MonoBehaviour
    {
        [SerializeField] private Image _tutorialFinger;

        private Button _button;

        private CameraSelector _cameraSelector;
        private IWindowService _windowService;
        private IGameplayModel _gameplayModel;
        private ITutorialService _tutorialService;

        [Inject]
        private void Construct(IWindowService windowService, IGameplayModel gameplayModel,
            ITutorialService tutorialService)
        {
            _windowService = windowService;
            _gameplayModel = gameplayModel;
            _tutorialService = tutorialService;
        }

        private void Awake()
        {
            if (_button == null) _button = gameObject.GetComponent<Button>();
        }

        // 原来是 async void + await Task.Delay(10)。WebGL / 抖音小游戏里
        // await 的续体要靠 SynchronizationContext 回来，一旦没回来，
        // StartPlay() 就永远不执行 —— 表现就是镜头停在 Intro 不动、
        // 波次也没真正开始。改用协程，必定继续往下走。
        private IEnumerator Start()
        {
            Debug.Log($"[开始游戏] Start 进入 state={(_tutorialService != null ? _tutorialService.CurrentState.ToString() : "服务为null")}");

            try
            {
                InjectService.Instance.Inject(this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[开始游戏] InjectService 注入异常：{exception.Message}");
            }

            if (_tutorialService == null)
            {
                Debug.LogError("[开始游戏] ITutorialService 为空，引导流程无法继续（按钮保持可用，避免卡死）");

                if (_button != null) _button.interactable = true;

                yield break;
            }

            if (_tutorialService.CurrentState == ETutorialState.Swipe)
            {
                yield return null;   // 等一帧，让 OnEnable 里的锁定状态先生效

                if (_button != null) _button.interactable = false;

                Debug.Log("[开始游戏] 引导停在 Swipe，自动开始波次");
                StartPlay();
            }

            OnChangedState(_tutorialService.CurrentState);
        }

        private void OnEnable()
        {
            if (_button == null) _button = gameObject.GetComponent<Button>();

            if (_button != null) _button.onClick.AddListener(Play);

            if (_tutorialService == null)
            {
                Debug.LogWarning("[开始游戏] ITutorialService 为空，OnEnable 跳过引导判定");
                return;
            }

            _tutorialService.СhangedState += OnChangedState;

            if (_tutorialService.CurrentState == ETutorialState.StartCard)
            {
                var waveIndex = _gameplayModel.GetCurrentRegionProgress().GetCurrentRegion().CurrentWaweIndex;
                _button.interactable = waveIndex < 2;
            }

            else if (_tutorialService.CurrentState == ETutorialState.PlayButton
                     || _tutorialService.CurrentState == ETutorialState.Completed)
            {
                _button.interactable = true;
            }
            else
            {
                // Swipe 状态下这里会禁用按钮，随后由 Start() 里的 StartPlay() 自动进关。
                // 一旦 StartPlay 挂了按钮就永久禁用 → 玩家卡死在主界面，所以 StartPlay 里必须兜底。
                _button.interactable = false;
            }
        }

        private void OnDisable()
        {
            if (_tutorialService != null) _tutorialService.СhangedState -= OnChangedState;

            if (_button != null) _button.onClick.RemoveListener(Play);
        }

        private void Play()
        {
            StartPlay();
        }

        private void StartPlay()
        {
            Debug.Log("[开始游戏] StartPlay 进入");

            try
            {
                _cameraSelector = FindObjectOfType<CameraSelector>(true);

                if (_cameraSelector == null)
                {
                    Debug.LogError("[开始游戏] 场景里找不到 CameraSelector，镜头切不过去（不阻断后续流程）");
                }
                else
                {
                    _cameraSelector.ChangeCamera(ECameraType.Park);
                    Debug.Log("[开始游戏] 镜头已切换到 Park");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[开始游戏] 切换镜头失败：{exception}");
            }

            try
            {
                _windowService?.Close(WindowType.Footer);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[开始游戏] 关闭底栏失败：{exception}");
            }

            try
            {
                _gameplayModel?.StartWave();
                Debug.Log("[开始游戏] StartWave 已调用");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[开始游戏] StartWave 失败：{exception}");
            }

            if (_tutorialFinger != null) _tutorialFinger.gameObject.SetActive(false);

            // 兜底：Swipe 引导仍未推进说明上面某步没生效，把按钮放开，
            // 否则玩家会停在主界面且底栏被引导全锁，完全无法操作。
            if (_button != null && _tutorialService != null &&
                _tutorialService.CurrentState == ETutorialState.Swipe)
            {
                Debug.LogWarning("[开始游戏] 引导仍停在 Swipe，按钮恢复可点以便重试");
                _button.interactable = true;
            }
        }

        private void OnChangedState(ETutorialState state)
        {
            if (_button == null) return;

            if (state != ETutorialState.Completed && state != ETutorialState.PlayButton)
            {
                _button.interactable = false;
                return;
            }

            if (state == ETutorialState.PlayButton && _tutorialFinger != null)
            {
                _tutorialFinger.gameObject.SetActive(true);
            }

            _button.interactable = true;
        }
    }
}
