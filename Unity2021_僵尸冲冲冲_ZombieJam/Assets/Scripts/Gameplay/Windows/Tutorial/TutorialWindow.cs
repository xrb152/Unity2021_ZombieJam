using Gameplay.Tutorial;
using Infrastructure.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Gameplay.Windows.Tutorial
{
    public class TutorialWindow : Window
    {
        [SerializeField] private Image _bg;
        [SerializeField] private RectTransform _messageContent;
        [SerializeField] private TextMeshProUGUI _messageText;

        [Inject] private ITutorialService _tutorialService;

        private void OnEnable()
        {
            // 预制体里 AlphaBG 是默认激活的全屏黑色遮罩（raycastTarget=1）。
            // 窗口一被打开就会挡住底栏和所有按钮，所以统一以“无遮罩”作为初始状态，
            // 只有 ShowMessage(..., isActiveBg: true) 才会显式打开它。
            if (_bg != null) _bg.gameObject.SetActive(false);

            if (_tutorialService != null) _tutorialService.Message += SetMessage;
        }

        private void OnDisable()
        {
            if (_tutorialService != null) _tutorialService.Message -= SetMessage;

            if (_messageContent != null) _messageContent.gameObject.SetActive(false);
        }

        private void SetMessage(string message, Vector2 position, bool isActiveBg)
        {
            if (_bg != null) _bg.gameObject.SetActive(isActiveBg);

            if (_messageContent != null)
            {
                _messageContent.gameObject.SetActive(true);
                _messageContent.anchoredPosition = position;
            }

            if (_messageText != null) _messageText.text = message;
        }
    }
}