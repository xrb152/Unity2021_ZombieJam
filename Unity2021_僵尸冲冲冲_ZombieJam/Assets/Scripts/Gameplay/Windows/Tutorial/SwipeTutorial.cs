using System;
using System.Collections;
using DG.Tweening;
using Gameplay.Enums;
using Gameplay.Parking;
using Gameplay.Tutorial.States.SwipeState;
using Infrastructure.Input;
using UnityEngine;
using Utils.ZenjectInstantiateUtil;
using Zenject;

namespace Gameplay.Windows.Tutorial
{
    // ПРОСТИТЕ МЕНЯ, ЕСЛИ КТО УВИДИТ ЭТО
    public class SwipeTutorial : MonoBehaviour
    {
        // 原来用 async void + Task.Delay(2000)。在 WebGL / 抖音小游戏里
        // 如果 SynchronizationContext 没挂好，await 之后的代码永远不会执行，
        // 表现就是「编辑器有引导、打包后什么都没有」。改用协程，必定执行。
        private const float StartDelay = 2f;
        private const float ArrowDistance = 1.75f;
        private const float ArrowDuration = 1.75f;

        // 判定「滑的是不是当前高亮的那只僵尸」的容差。
        // 原来用的是 transform.position 精确相等（!=），只要单位坐标差一点点
        // （换过预制体、被 parking 偏移过）就永远不相等，滑动全被吞掉，
        // 表现就是「摄像头一动不动，卡在这里」。
        private const float PositionTolerance = 0.75f;

        [SerializeField] private ArrowTutorial _secondArrow;
        [SerializeField] private ArrowTutorial _firstArrow;
        [SerializeField] private ArrowTutorial _thirdArrow;
        [SerializeField] private ArrowTutorial _fourtArrow;

        [SerializeField] private GameObject _firstFieldIllumination;
        [SerializeField] private GameObject _secondFieldIllumination;
        [SerializeField] private GameObject _tutorialBG;

        [SerializeField] private ZombieSpawner _zombieSpawner;

        [Inject] private SwipeManager _swipeManager;

        private readonly Vector3 _firstUnitPosition = new(-1.5f, 0.5f, 0.5f);
        private readonly Vector3 _secondUnitPosition = new(0.49999994f, 0.5f, 0.49999994f);
        private readonly Vector3 _thirdUnitPosition = new(0.5f, 0.5f, -1.5f);
        private readonly Vector3 _fourthUnitPosition = new(2.5f, 0.5f, -1.5f);

        private Tween _tween;
        private int _unitIndex;
        private bool _swipeSubscribed;
        private bool _zombieSubscribed;
        private bool _shown;
        private Coroutine _routine;
        private Sprite _fallbackBackground;

        // 只在首次初始化时隐藏引导元素。
        // 原来是写在 OnEnable 里的 —— 对象每次启停都会被重置，
        // 而显示逻辑只跑一次，重新激活后引导就再也不出现了。
        private void Awake()
        {
            EnsureBackgroundSprite();
            HideAll();
        }

        // _tutorialBG 的 SpriteRenderer 引用的是
        // Library/PackageCache/com.unity.2d.sprite/.../Editor/.../Square.png，
        // 也就是「编辑器专用」目录下的资源 —— 打包时 Editor 目录会被剔除，
        // 这层 72% 不透明的黑色暗底就没了，白字直接糊在场景上看不清。
        // 运行时补一张 1x1 白图（PPU=1，正好 1 世界单位，和原图 256/256 等价）。
        private void EnsureBackgroundSprite()
        {
            if (_tutorialBG == null) return;

            var spriteRenderer = _tutorialBG.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null) return;
            if (spriteRenderer.sprite != null) return;

            if (_fallbackBackground == null)
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "SwipeTutorialBG_Runtime"
                };

                texture.SetPixel(0, 0, Color.white);
                texture.Apply();

                _fallbackBackground = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f), 1f);
                _fallbackBackground.name = "SwipeTutorialBG_Runtime";
            }

            spriteRenderer.sprite = _fallbackBackground;

            Debug.Log("[滑动引导] 暗底贴图丢失（编辑器专用资源），已用运行时白图补上");
        }

        private void OnEnable()
        {
            EnsureSwipeManager();
            Subscribe();
            TryStartShowRoutine();
        }

        private void Start()
        {
            EnsureSwipeManager();
            Subscribe();
            TryStartShowRoutine();

            Debug.Log($"[滑动引导] Start spawner={(_zombieSpawner != null)} " +
                      $"zombies={(CountZombies())} swipeManager={(_swipeManager != null)} shown={_shown}");
        }

        private void OnDisable()
        {
            Unsubscribe();

            _tween?.Kill();
            _tween = null;
            _routine = null;
        }

        private void TryStartShowRoutine()
        {
            if (_shown || _routine != null) return;
            if (!isActiveAndEnabled) return;

            _routine = StartCoroutine(ShowFirstStepRoutine());
        }

        private IEnumerator ShowFirstStepRoutine()
        {
            // 用 Realtime，关卡加载时 timeScale 可能为 0
            yield return new WaitForSecondsRealtime(StartDelay);

            _routine = null;
            _shown = true;

            Debug.Log("[滑动引导] 2 秒计时结束，显示第一步");

            ShowFirstStep();
        }

        private void ShowFirstStep()
        {
            SetActive(_tutorialBG, true);
            SetActive(_firstFieldIllumination, true);
            SetActive(_firstArrow, true);

            StartArrowTween(_firstArrow, true);
        }

        private void StartArrowTween(ArrowTutorial arrow, bool vertical)
        {
            if (arrow == null) return;

            _tween?.Kill();

            var target = arrow.transform;

            _tween = vertical
                ? target.DOLocalMoveZ(target.position.z + ArrowDistance, ArrowDuration).SetLoops(-1, LoopType.Restart)
                : target.DOLocalMoveX(target.position.x - ArrowDistance, ArrowDuration).SetLoops(-1, LoopType.Restart);
        }

        private void OnInitializePathfinder()
        {
            _tween?.Kill();
            _unitIndex++;

            Debug.Log($"[滑动引导] 僵尸已就位，推进到第 {_unitIndex} 个提示");

            switch (_unitIndex)
            {
                case 1:
                    SetActive(_firstFieldIllumination, false);
                    SetActive(_secondFieldIllumination, true);
                    SetActive(_secondArrow, true);
                    StartArrowTween(_secondArrow, false);
                    break;

                case 2:
                    SetActive(_secondFieldIllumination, false);
                    SetActive(_tutorialBG, false);
                    SetActive(_thirdArrow, true);
                    StartArrowTween(_thirdArrow, true);
                    break;

                case 3:
                    SetActive(_fourtArrow, true);
                    StartArrowTween(_fourtArrow, false);
                    break;

                case 4:
                    if (_swipeManager != null)
                    {
                        _swipeManager.IsSwipeTutorialCompleted = true;
                    }

                    Debug.Log("[滑动引导] 四个单位全部就位，滑动教学完成");
                    break;
            }
        }

        private void OnSwipe(TutorialSwipeInfo swipeObject)
        {
            // TutorialSwipeInfo 是 struct，不能与 null 比较，只能逐个判断引用字段
            if (swipeObject.SwipeGameObject == null || swipeObject.UnitSwipe == null)
            {
                Debug.LogWarning("[滑动引导] 收到滑动但 SwipeGameObject / UnitSwipe 为空，忽略");
                return;
            }

            if (_unitIndex >= 4)
            {
                swipeObject.UnitSwipe.Swipe(swipeObject.SwipeSide);
                return;
            }

            var unitPosition = GetUnitPosition(_unitIndex);
            var actual = swipeObject.SwipeGameObject.transform.position;
            var distance = Vector3.Distance(actual, unitPosition);

            if (distance > PositionTolerance)
            {
                // 位置对不上时绝对不能把这次滑动吞掉，否则玩家再也动不了，
                // 引导永久卡死（就是现在这个现象）。这里只跳过引导推进，真实滑动照做。
                Debug.LogWarning($"[滑动引导] 第 {_unitIndex} 步期望 {unitPosition}，实际 {actual}" +
                                 $"（差 {distance:F2} 单位），已跳过引导校验、直接放行这次滑动");
                swipeObject.UnitSwipe.Swipe(swipeObject.SwipeSide);
                return;
            }

            if (swipeObject.SwipeDirection == ESwipeDirection.Vertical)
            {
                VerticalSwiped(swipeObject, unitPosition);
            }
            else if (swipeObject.SwipeDirection == ESwipeDirection.Horizontal)
            {
                HorizontalSwipe(swipeObject, unitPosition);
            }
            else
            {
                Debug.LogWarning($"[滑动引导] SwipeDirection={swipeObject.SwipeDirection}，" +
                                 "无法判断朝向，直接放行这次滑动");
                swipeObject.UnitSwipe.Swipe(swipeObject.SwipeSide);
            }
        }

        private void VerticalSwiped(TutorialSwipeInfo swipeObject, Vector3 unitPosition)
        {
            if (swipeObject.SwipeSide != ESwipeSide.Forward && swipeObject.SwipeSide != ESwipeSide.Back)
            {
                Debug.Log($"[滑动引导] 本步要前后滑，收到 {swipeObject.SwipeSide}，忽略");
                return;
            }

            swipeObject.UnitSwipe.Swipe(swipeObject.SwipeSide);
            HideArrow();
        }

        private void HorizontalSwipe(TutorialSwipeInfo swipeObject, Vector3 unitPosition)
        {
            if (swipeObject.SwipeSide != ESwipeSide.Left && swipeObject.SwipeSide != ESwipeSide.Right)
            {
                Debug.Log($"[滑动引导] 本步要左右滑，收到 {swipeObject.SwipeSide}，忽略");
                return;
            }

            swipeObject.UnitSwipe.Swipe(swipeObject.SwipeSide);
            HideArrow();
        }

        private void HideArrow()
        {
            switch (_unitIndex)
            {
                case 0:
                    SetActive(_firstArrow, false);
                    break;
                case 1:
                    SetActive(_secondArrow, false);
                    break;
                case 2:
                    SetActive(_thirdArrow, false);
                    break;
                case 3:
                    SetActive(_fourtArrow, false);
                    break;
            }
        }

        private Vector3 GetUnitPosition(int unitIndex)
        {
            return unitIndex switch
            {
                0 => _firstUnitPosition,
                1 => _secondUnitPosition,
                2 => _thirdUnitPosition,
                3 => _fourthUnitPosition,
                _ => Vector3.zero
            };
        }

        private void HideAll()
        {
            SetActive(_firstFieldIllumination, false);
            SetActive(_secondFieldIllumination, false);
            SetActive(_tutorialBG, false);
            SetActive(_firstArrow, false);
            SetActive(_secondArrow, false);
            SetActive(_thirdArrow, false);
            SetActive(_fourtArrow, false);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target == null) return;
            if (target.activeSelf == active) return;

            target.SetActive(active);
        }

        private static void SetActive(ArrowTutorial arrow, bool active)
        {
            if (arrow == null) return;

            SetActive(arrow.gameObject, active);
        }

        private int CountZombies()
        {
            if (_zombieSpawner == null || _zombieSpawner.Zombies == null) return -1;

            return _zombieSpawner.Zombies.Count;
        }

        // InjectService 是纯 C# 单例，打包后初始化时序与编辑器不同。
        // 原来裸调用一旦抛异常，后面的 StartAnimation() 就不会执行，
        // 引导从头到尾都不会出现 —— 这里改为失败也能继续。
        private void EnsureSwipeManager()
        {
            if (_swipeManager != null) return;

            try
            {
                InjectService.Instance.Inject(this);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[滑动引导] InjectService 注入失败，改用容器兜底：{exception.Message}");
            }

            if (_swipeManager != null) return;

            if (!ProjectContext.HasInstance) return;

            try
            {
                _swipeManager = ProjectContext.Instance.Container.TryResolve<SwipeManager>();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[滑动引导] 容器取 SwipeManager 失败：{exception.Message}");
            }

            if (_swipeManager == null)
            {
                Debug.LogError("[滑动引导] SwipeManager 为 null，滑动操作不会推进引导");
            }
        }

        private void Subscribe()
        {
            if (!_swipeSubscribed && _swipeManager != null)
            {
                _swipeManager.OnSwipe += OnSwipe;
                _swipeSubscribed = true;
            }

            if (_zombieSubscribed) return;
            if (_zombieSpawner == null || _zombieSpawner.Zombies == null) return;

            foreach (var zombie in _zombieSpawner.Zombies)
            {
                if (zombie != null)
                {
                    zombie.OnInitializePath += OnInitializePathfinder;
                }
            }

            _zombieSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (_swipeSubscribed && _swipeManager != null)
            {
                _swipeManager.OnSwipe -= OnSwipe;
            }

            _swipeSubscribed = false;

            if (_zombieSubscribed && _zombieSpawner != null && _zombieSpawner.Zombies != null)
            {
                foreach (var zombie in _zombieSpawner.Zombies)
                {
                    if (zombie != null)
                    {
                        zombie.OnInitializePath -= OnInitializePathfinder;
                    }
                }
            }

            _zombieSubscribed = false;
        }
    }
}
