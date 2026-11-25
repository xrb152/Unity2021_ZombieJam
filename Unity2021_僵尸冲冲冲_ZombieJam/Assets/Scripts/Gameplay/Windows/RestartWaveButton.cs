using Gameplay.CinemachineCamera;
using Gameplay.Tutorial;
using Gameplay.Windows.Gameplay;
using Infrastructure.Windows;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace Gameplay.Windows
{
    public class RestartWaveButton : MonoBehaviour
    {
        private Button _button;
        private CameraSelector _cameraSelector;

        [Inject] private IWindowService _windowService;
        [Inject] private ITutorialService _tutorialService;
        [Inject] private IGameplayModel _gameplayModel;

        private void Awake()
        {
            _button = gameObject.GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(Restart);

            _button.interactable = _tutorialService.CurrentState != ETutorialState.Swipe;
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(Restart);
        }

        // 原 async void + await Task.Delay(50)：WebGL 下续体不恢复，
        // 重开波次后镜头不切、StartWave 不调用 → 卡在白场景。
        private void Restart()
        {
            _gameplayModel.StopWave();

            SceneManager.UnloadSceneAsync("Gameplay");
            SceneManager.LoadScene($"Gameplay");
            _windowService.Close(WindowType.Gameplay);

            DelayedCall.After(50, () =>
            {
                _cameraSelector = FindObjectOfType<CameraSelector>();

                if (_cameraSelector != null)
                {
                    _cameraSelector.ChangeCamera(ECameraType.Park);
                }

                _windowService.Open(WindowType.Gameplay);
                _gameplayModel.StartWave();
            });
        }
    }
}