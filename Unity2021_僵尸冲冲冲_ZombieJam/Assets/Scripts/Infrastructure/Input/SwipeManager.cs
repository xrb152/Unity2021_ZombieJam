using System;
using Gameplay.Enums;
using Gameplay.Tutorial;
using Gameplay.Units;
using Gameplay.Windows.Gameplay;
using UnityEngine;
using Zenject;

namespace Infrastructure.Input
{
    public class SwipeManager : MonoBehaviour
    {
        public event Action<TutorialSwipeInfo> OnSwipe;

        private IGameplayModel _gameplayModel;

        private Vector3 _startPosition;
        private RaycastDetector _raycastDetector;

        private UnitSwipe _swipeObject;
        private TutorialSwipeInfo _tutorialSwipeInfo;

        private bool _isWasSwipe;
        private bool _loggedMissingModel;

        private Material _material;

        public bool IsSwipeTutorialCompleted { get; set; }

        // 只有这一层的碰撞体参与滑动拾取，改动这里等于改玩法
        private const int SwipeLayer = 10;

        [Inject]
        public void Construct(IGameplayModel gameplayModel)
        {
            _gameplayModel = gameplayModel;
        }

        public void Initialize()
        {
            _raycastDetector = new RaycastDetector();
            IsSwipeTutorialCompleted = _gameplayModel.TutorialState != ETutorialState.Swipe;
        }

        private void Update()
        {
            if (_gameplayModel == null)
            {
                if (!_loggedMissingModel)
                {
                    _loggedMissingModel = true;
                    Debug.LogError("[滑动] IGameplayModel 没有注入成功，SwipeManager 完全不会响应触摸");
                }

                return;
            }

            // 只在「按下」这一帧打诊断，不会刷屏。
            // 卡住时第一眼就能看出是没进波次、还是射线没打到僵尸。
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                LogPressDiagnostics();
            }

            if (!_gameplayModel.IsStartWave) return;

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                _startPosition = UnityEngine.Input.mousePosition;
                SetSwipedObject();
            }

            if (UnityEngine.Input.GetMouseButton(0))
            {
                DetectSwipe();
            }

            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                if (_swipeObject != null)
                {
                    _swipeObject.ResetSwipeObject();
                    _swipeObject = null;
                }
                _isWasSwipe = false;
                _tutorialSwipeInfo.Reset();
            }
        }

        private void LogPressDiagnostics()
        {
            if (_raycastDetector == null) _raycastDetector = new RaycastDetector();

            var contactInfo = _raycastDetector.RayCast(SwipeLayer);
            var hitName = contactInfo.Collider != null
                ? $"{contactInfo.Collider.name}(layer={contactInfo.Collider.gameObject.layer})"
                : "没打到任何 collider";

            Debug.Log($"[滑动] 按下 pos={UnityEngine.Input.mousePosition} " +
                      $"IsStartWave={_gameplayModel.IsStartWave} " +
                      $"TutorialState={_gameplayModel.TutorialState} " +
                      $"引导滑动已完成={IsSwipeTutorialCompleted} " +
                      $"射线={hitName} cam={(Camera.main != null ? Camera.main.name : "null")}");
        }

        private void DetectSwipe()
        {
            if (_swipeObject != null && !_isWasSwipe)
            {
                var swipe = DefineSwipe();

                if (swipe != ESwipeSide.None)
                {
                    _isWasSwipe = true;

                    Debug.Log($"[滑动] 识别到 {swipe}，目标={_swipeObject.name} " +
                              $"TutorialState={_gameplayModel.TutorialState} " +
                              $"引导滑动已完成={IsSwipeTutorialCompleted}");

                    if (_gameplayModel.TutorialState == ETutorialState.Swipe && !IsSwipeTutorialCompleted)
                    {
                        _tutorialSwipeInfo.SwipeSide = swipe;
                        OnSwipe?.Invoke(_tutorialSwipeInfo);
                        return;
                    }

                    _swipeObject.Swipe(swipe);
                    OnSwipe?.Invoke(_tutorialSwipeInfo);
                }
            }
        }

        private void SetSwipedObject()
        {
            if (_raycastDetector == null) _raycastDetector = new RaycastDetector();

            var contactInfo = _raycastDetector.RayCast(SwipeLayer);
            if (contactInfo.Collider == null) return;

            _swipeObject = contactInfo.Collider.GetComponent<UnitSwipe>();
            if (_swipeObject == null)
            {
                Debug.LogWarning($"[滑动] 打到了 {contactInfo.Collider.name}，但它身上没有 UnitSwipe");
                return;   // 点到没有 UnitSwipe 的碰撞体时不要再往下走，否则空引用
            }

            _swipeObject.SetSwipeObject();
            _tutorialSwipeInfo.SwipeGameObject = contactInfo.Collider.gameObject;
            _tutorialSwipeInfo.SwipeDirection = _swipeObject.SwipeDirection;
            _tutorialSwipeInfo.UnitSwipe = _swipeObject;
        }

        private ESwipeSide DefineSwipe()
        {
            var currentPosition = UnityEngine.Input.mousePosition;
            if (_startPosition != currentPosition)
            {
                var deltaPosition = _startPosition - currentPosition;
                var deltaHorizontalSwipe = Math.Abs(_startPosition.x - currentPosition.x);
                var deltaVerticalSwipe = Math.Abs(_startPosition.y - currentPosition.y);

                if (deltaPosition.x != 0 && deltaHorizontalSwipe > deltaVerticalSwipe)
                {
                    var swipeSide = deltaPosition.x > 0 ? ESwipeSide.Left : ESwipeSide.Right;
                    return swipeSide;
                }

                if (deltaPosition.y != 0)
                {
                    var swipeSide = deltaPosition.y > 0 ? ESwipeSide.Back : ESwipeSide.Forward;
                    return swipeSide;
                }
            }


            return ESwipeSide.None;
        }
    }
}