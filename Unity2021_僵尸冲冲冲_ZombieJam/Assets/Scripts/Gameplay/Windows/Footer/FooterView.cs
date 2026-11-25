using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Tutorial;
using Infrastructure.Windows;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils.ZenjectInstantiateUtil;
using Zenject;

namespace Gameplay.Windows.Footer
{
    public class FooterView : MonoBehaviour
    {
        [SerializeField] private List<FooterTab> _footerTabs = new();
        [SerializeField] private Sprite _selectedImage;
        [SerializeField] private Sprite _idleImage;
        [SerializeField] private Sprite _disabledImage;

        /// <summary>
        /// 排查用：点击屏幕时打印「输入是否到达 Unity / 坐标 / 该点命中的 UI 链」。
        /// 不影响功能，定位完可以关掉。
        /// </summary>
        [SerializeField] private bool _debugPointer = true;

        private IWindowService _windowService;
        private ITutorialService _tutorialService;

        public List<FooterTab> FooterTabs => _footerTabs;

        /// <summary>
        /// 拿不到引导服务时按 Completed 处理：
        /// 底栏是导航，宁可放开也不要因为注入失败变成一堵死墙。
        /// </summary>
        private ETutorialState CurrentState =>
            _tutorialService != null ? _tutorialService.CurrentState : ETutorialState.Completed;

        [Inject]
        private void Construct(IWindowService windowService, ITutorialService tutorialService)
        {
            _windowService = windowService;
            _tutorialService = tutorialService;
        }

        private void Awake()
        {
            EnsureServices();
        }

        private void Start()
        {
            Debug.Log("[底栏] Start 进入");

            // 这一行原来是裸调用：ZenjectInstantiateSpawner 没初始化时抛 NRE，
            // Start 直接中断 → 底栏不做任何初始化 → 点哪个都没反应且外观毫无变化。
            try
            {
                InjectService.Instance.Inject(this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[底栏] InjectService 注入异常，改用容器兜底：{exception.Message}");
            }

            EnsureServices();

            Debug.Log($"[底栏] 初始化 tabs={_footerTabs?.Count ?? 0} " +
                      $"windowService={_windowService != null} tutorialService={_tutorialService != null} " +
                      $"state={CurrentState}");

            if (_windowService != null) _windowService.OnOpen += OnOpenedView;
            if (_tutorialService != null) _tutorialService.СhangedState += OnChangedTutorialState;

            OnChangedTutorialState(CurrentState);

            if (CurrentState == ETutorialState.Completed)
            {
                var lobbyTab = _footerTabs.Find(x => x != null && x.WindowType == WindowType.Lobby);

                if (lobbyTab != null) SelectedTab(lobbyTab);
            }

            LogRaycastDiagnostics();
        }

        /// <summary>
        /// 正常情况下 Zenject 会在实例化时注入 Construct；窗口如果是从场景/其它途径来的，
        /// 这里再从 ProjectContext 兜底取一次，避免服务为空导致整条点击链静默失效。
        /// </summary>
        private void EnsureServices()
        {
            if (_windowService != null && _tutorialService != null) return;

            // 注意：ProjectContext.Instance 的 getter 在没有实例时会顺手新建一个空容器，必须先判 HasInstance。
            if (!ProjectContext.HasInstance) return;

            var container = ProjectContext.Instance.Container;
            if (container == null) return;

            if (_windowService == null) _windowService = container.TryResolve<IWindowService>();
            if (_tutorialService == null) _tutorialService = container.TryResolve<ITutorialService>();
        }

        private IWindowService ResolveWindowService()
        {
            if (_windowService == null) EnsureServices();
            return _windowService;
        }

        /// <summary>
        /// 一次性诊断：把每个 tab 中心点上方真正接收点击的 UI 打印出来。
        /// 如果第一名不是 FooterButton 的子节点，说明底栏被别的全屏透明 Image 挡住了。
        /// </summary>
        private void LogRaycastDiagnostics()
        {
            if (EventSystem.current == null)
            {
                Debug.LogError("[底栏诊断] 场景里没有启用中的 EventSystem，所有 UI 都不会响应点击");
                return;
            }

            var eventData = new PointerEventData(EventSystem.current);
            var results = new List<RaycastResult>();

            foreach (var tab in _footerTabs)
            {
                if (tab == null) continue;

                eventData.position = RectTransformUtility.WorldToScreenPoint(null, tab.transform.position);
                results.Clear();
                EventSystem.current.RaycastAll(eventData, results);

                var top = results.Count == 0
                    ? "（空：这个位置没有任何可点击 UI）"
                    : string.Join(" > ", results.Take(4).Select(x => x.gameObject.name));

                Debug.Log($"[底栏诊断] {tab.name} interactable={tab.IsInteractable} 命中：{top}");
            }
        }

        /// <summary>
        /// 平台差异排查：区分「点击根本没进 Unity」/「坐标缩放错位」/「被别的 UI 挡住」/「按钮被禁用」。
        /// </summary>
        private void Update()
        {
            if (!_debugPointer) return;
            if (!Input.GetMouseButtonDown(0)) return;

            var position = Input.mousePosition;
            var overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            var top = "（无 EventSystem）";

            if (EventSystem.current != null)
            {
                var eventData = new PointerEventData(EventSystem.current) { position = position };
                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);
                top = results.Count == 0
                    ? "（空）"
                    : string.Join(" > ", results.Take(4).Select(x => x.gameObject.name));
            }

            Debug.Log($"[底栏指针] screen={Screen.width}x{Screen.height} dpi={Screen.dpi} " +
                      $"pos={position} overUI={overUI} touchCount={Input.touchCount} 命中：{top}");
        }

        private void OnEnable()
        {
            foreach (var footerTab in _footerTabs)
            {
                if (footerTab == null)
                {
                    Debug.LogError("[底栏] _footerTabs 里有空引用，请检查 FooterWindow 预制体的 tab 列表");
                    continue;
                }

                footerTab.Click += SelectedTab;
            }
        }

        private void OnDisable()
        {
            foreach (var footerTab in _footerTabs)
            {
                if (footerTab == null) continue;

                footerTab.Click -= SelectedTab;
            }
        }

        private void SelectedTab(FooterTab selected)
        {
            if (selected == null)
            {
                Debug.LogWarning("[底栏] SelectedTab 收到空 tab，已忽略");
                return;
            }

            var state = CurrentState;

            if (selected.WindowType == WindowType.MainMenu)
            {
                Debug.LogWarning($"[底栏] {selected.name} 没有配置窗口类型（_windowType 还是默认 MainMenu），点击不生效");
                return;
            }

            if (state == ETutorialState.StartCard && selected.WindowType == WindowType.Shop) return;

            foreach (var footerTab in _footerTabs.Where(x => x != null && x.IsInteractable))
            {
                if (footerTab.WindowType != selected.WindowType)
                {
                    footerTab.SetImage(_idleImage);
                    footerTab.Selected(false);
                    continue;
                }

                selected.Selected(true);
                selected.SetImage(_selectedImage);

                if (state != ETutorialState.Completed)
                {
                    selected.SetActiveTutorialFinger(false);
                }
            }

            var windowService = ResolveWindowService();

            if (windowService == null)
            {
                Debug.LogError($"[底栏] IWindowService 取不到，无法打开 {selected.WindowType} 窗口");
                return;
            }

            Debug.Log($"[底栏] 打开窗口 {selected.WindowType}");
            windowService.Open(selected.WindowType);
        }

        private void OnChangedTutorialState(ETutorialState tutorialState)
        {
            if (_windowService != null &&
                _windowService.IsOpen(WindowType.Shop) &&
                tutorialState == ETutorialState.ShopCurrency) return;

            foreach (var footerTab in _footerTabs)
            {
                if (footerTab == null) continue;

                // 单个 tab 出问题不能拖垮整条底栏。
                try
                {
                    ApplyTutorialState(footerTab, tutorialState);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[底栏] 处理 {footerTab.name} 的引导状态 {tutorialState} 时出错：{exception}");
                }
            }
        }

        private void ApplyTutorialState(FooterTab footerTab, ETutorialState tutorialState)
        {
            SetTutorialInteractableTab(footerTab, false);
            footerTab.SetActiveTutorialFinger(false);

            switch (tutorialState)
            {
                case ETutorialState.Completed:
                    // 地牢按钮的 _windowType 还是默认的 MainMenu（枚举里没有 Dungeon），
                    // 放开也打不开任何窗口，保持锁定更诚实。
                    if (footerTab.WindowType != WindowType.MainMenu)
                    {
                        SetTutorialInteractableTab(footerTab, true);
                    }

                    break;

                case ETutorialState.Swipe:
                    break;

                case ETutorialState.ShopBox:
                case ETutorialState.ShopCurrency:
                    if (footerTab.WindowType == WindowType.Shop)
                    {
                        SetTutorialInteractableTab(footerTab, true);
                        footerTab.SetActiveTutorialFinger(true);
                    }

                    break;

                case ETutorialState.StartCard:
                    if (footerTab.WindowType == WindowType.Cards)
                    {
                        SetTutorialInteractableTab(footerTab, true);
                        footerTab.SetActiveTutorialFinger(true);
                    }

                    if (footerTab.WindowType == WindowType.Shop)
                    {
                        SetTutorialInteractableTab(footerTab, true);
                    }

                    break;

                case ETutorialState.FinishCard:
                    if (footerTab.WindowType == WindowType.Lobby)
                    {
                        SetTutorialInteractableTab(footerTab, true);
                        footerTab.SetActiveTutorialFinger(true);
                    }

                    break;

                case ETutorialState.PlayButton:
                    break;

                default:
                    // 原来是 throw new ArgumentOutOfRangeException：
                    // 枚举新增值（例如被注释掉的 ShopCurrency）会让 Start 直接崩，
                    // 底栏从此完全不响应，外观也不会有任何变化。
                    Debug.LogWarning($"[底栏] 未处理的引导状态 {tutorialState}，按全部禁用处理");
                    break;
            }
        }

        private void SetTutorialInteractableTab(FooterTab footerTab, bool isInteractable)
        {
            footerTab.SetInteractable(isInteractable);

            if (!isInteractable)
            {
                footerTab.Selected(false);
            }

            footerTab.SetImage(isInteractable ? _idleImage : _disabledImage);
            footerTab.SetScale();
        }

        private void OnOpenedView(WindowType windowType)
        {
            if (CurrentState != ETutorialState.Completed) return;

            var footerTab = _footerTabs.Find(x => x != null && x.WindowType == windowType);

            if (footerTab == null || footerTab.IsSelected) return;

            if (windowType == WindowType.Lobby || windowType == WindowType.Region)
            {
                SelectedTab(footerTab);
            }
        }
    }
}
