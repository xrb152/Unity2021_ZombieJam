using System;
using Infrastructure.Windows;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.Windows.Footer
{
    /// <summary>
    /// 继承 IPointerClickHandler 是为了在按钮被禁用时也能收到点击：
    /// Button.interactable=false 时 Unity 会直接吞掉点击，什么都不打印，
    /// 表现就是「点了没反应，也不知道为什么」。
    /// </summary>
    public class FooterTab : MonoBehaviour, IPointerClickHandler
    {
        public event Action<FooterTab> Click;

        private Button _button;

        [SerializeField] private WindowType _windowType;
        [SerializeField] private Image _lockImage;
        [SerializeField] private Image _alphaBg;
        [SerializeField] private Image _tutorialFinger;

        public bool IsInteractable;
        public bool IsSelected;

        public WindowType WindowType => _windowType;

        private void Awake()
        {
            if (_button == null) _button = gameObject.GetComponent<Button>();

            if (_button != null) _button.interactable = IsInteractable;

            if (_lockImage != null) _lockImage.gameObject.SetActive(!IsInteractable);

            // 预制体里 AlphaBG（白 68.6% 全屏遮罩 + Lock 的父节点）默认是激活的，
            // 只有走 SetInteractable 才会被关掉。开局这里必须同步一次，
            // 否则「可交互」的 tab 也会一直蒙着禁用遮罩，看起来就是灰的。
            if (_alphaBg != null) _alphaBg.gameObject.SetActive(!IsInteractable);
        }

        private void OnEnable()
        {
            if (_button == null) _button = gameObject.GetComponent<Button>();
            if (_button == null)
            {
                Debug.LogError($"[底栏] {name} 上找不到 Button 组件，这个 tab 永远点不动");
                return;
            }

            _button.onClick.AddListener(ClickButton);
        }

        private void OnDisable()
        {
            if (_button == null) return;

            _button.onClick.RemoveListener(ClickButton);
        }

        private void ClickButton()
        {
            var subscribers = Click != null ? Click.GetInvocationList().Length : 0;
            Debug.Log($"[底栏] 收到点击：{name}（{_windowType}），订阅者数={subscribers}");
            Click?.Invoke(this);
        }

        public void Selected(bool isSelected)
        {
            IsSelected = isSelected;

            SetScale();
        }

        public void SetScale()
        {
            if (IsSelected)
            {
                gameObject.transform.localScale = new Vector3(1.15f, 1.15f);
            }
            else
            {
                gameObject.transform.localScale = new Vector3(1, 1);
            }
        }

        public void SetInteractable(bool isInteractable)
        {
            // 原来是 if (IsInteractable == false) return; —— 字段从不被赋值，
            // 于是「初始锁定」的 tab 永远解不开（按钮永远 interactable=false）。
            IsInteractable = isInteractable;

            if (_button == null) _button = gameObject.GetComponent<Button>();

            if (_button != null) _button.interactable = isInteractable;
            if (_alphaBg != null) _alphaBg.gameObject.SetActive(!isInteractable);

            // Lock 是 AlphaBG 的子节点，Awake 之后就一直没人再开它，
            // 结果锁定的 tab 只有一层白雾、没有锁 —— 玩家看不出是「锁住了」。
            if (_lockImage != null) _lockImage.gameObject.SetActive(!isInteractable);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsInteractable) return;

            Debug.Log($"[底栏] {name}（{_windowType}）处于锁定状态，点击被忽略。" +
                      "锁定通常来自两处：引导未完成，或该功能还没解锁（地图/地牢）");
        }

        public void SetImage(Sprite image)
        {
            if (_button == null) _button = gameObject.GetComponent<Button>();
            if (_button == null || _button.image == null) return;

            _button.image.sprite = image;
        }

        public void SetActiveTutorialFinger(bool isActive)
        {
            if (_tutorialFinger == null) return;

            _tutorialFinger.gameObject.SetActive(isActive);
        }
    }
}