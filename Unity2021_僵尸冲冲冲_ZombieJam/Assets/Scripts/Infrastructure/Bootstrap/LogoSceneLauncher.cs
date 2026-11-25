using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Project.Scripts.Infrastructure.Bootstrap
{
    /// <summary>
    /// Logo 展示场景控制器：淡入 + 轻微放大 → 停留 → 淡出 → 加载下一个场景（默认 Bootstrap）。
    /// 全程使用 unscaled time，不受 Time.timeScale 影响；点击 / 触摸可跳过。
    /// </summary>
    [DisallowMultipleComponent]
    public class LogoSceneLauncher : MonoBehaviour
    {
        [Header("引用（留空会自动兜底）")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _logoRect;
        [SerializeField] private Image _logoImage;

        [Header("流程")]
        [SerializeField] private string _nextSceneName = "Bootstrap";
        [SerializeField] private float _fadeInDuration = 0.45f;
        [SerializeField] private float _holdDuration = 1.1f;
        [SerializeField] private float _fadeOutDuration = 0.3f;

        [Header("交互")]
        [Tooltip("进场景后这段时间内忽略点击，避免把加载完成时的残留点击当成跳过")]
        [SerializeField] private float _skipProtectDuration = 0.25f;
        [SerializeField] private bool _allowSkip = true;

        private bool _isLoading;
        private bool _skipRequested;
        private float _elapsed;

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_logoImage == null)
            {
                // 优先取名为 Logo 的子物体，避免误取到 Background
                Transform logoTransform = transform.Find("Logo");

                if (logoTransform != null)
                {
                    _logoImage = logoTransform.GetComponent<Image>();
                }

                if (_logoImage == null)
                {
                    _logoImage = GetComponentInChildren<Image>(true);
                }
            }

            if (_logoRect == null && _logoImage != null)
            {
                _logoRect = _logoImage.rectTransform;
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            if (_logoRect != null)
            {
                _logoRect.localScale = new Vector3(0.94f, 0.94f, 1f);
            }
        }

        private void Start()
        {
            Debug.Log($"[Logo] 展示开始，约 {_fadeInDuration + _holdDuration + _fadeOutDuration:F2}s 后进入 {_nextSceneName}");

            StartCoroutine(PlayRoutine());
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;

            if (!_allowSkip || _isLoading || _skipRequested)
            {
                return;
            }

            if (_elapsed < _skipProtectDuration)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                _skipRequested = true;
                Debug.Log("[Logo] 收到点击 / 触摸，跳过停留时间");
            }
        }

        private IEnumerator PlayRoutine()
        {
            yield return FadeInRoutine();

            yield return HoldRoutine();

            yield return FadeOutRoutine();

            LoadNextScene();
        }

        private IEnumerator FadeInRoutine()
        {
            float duration = Mathf.Max(0f, _fadeInDuration);
            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;

                float progress = duration <= 0f ? 1f : Mathf.Clamp01(time / duration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);

                _canvasGroup.alpha = eased;

                if (_logoRect != null)
                {
                    float scale = Mathf.LerpUnclamped(0.94f, 1f, eased);
                    _logoRect.localScale = new Vector3(scale, scale, 1f);
                }

                yield return null;
            }

            _canvasGroup.alpha = 1f;

            if (_logoRect != null)
            {
                _logoRect.localScale = Vector3.one;
            }
        }

        private IEnumerator HoldRoutine()
        {
            float time = 0f;

            while (time < _holdDuration)
            {
                if (_skipRequested)
                {
                    yield break;
                }

                time += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator FadeOutRoutine()
        {
            // 跳过时快速淡出，避免手感拖沓
            float duration = _skipRequested ? Mathf.Min(_fadeOutDuration, 0.12f) : Mathf.Max(0f, _fadeOutDuration);
            float startAlpha = _canvasGroup.alpha;
            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;

                float progress = duration <= 0f ? 1f : Mathf.Clamp01(time / duration);
                _canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, 0f, Mathf.SmoothStep(0f, 1f, progress));

                yield return null;
            }

            _canvasGroup.alpha = 0f;
        }

        private void LoadNextScene()
        {
            if (_isLoading)
            {
                return;
            }

            _isLoading = true;

            if (string.IsNullOrWhiteSpace(_nextSceneName))
            {
                Debug.LogError("[Logo] 下一个场景名称为空，回退到 Bootstrap");
                _nextSceneName = "Bootstrap";
            }

            if (Application.CanStreamedLevelBeLoaded(_nextSceneName))
            {
                Debug.Log($"[Logo] 加载场景：{_nextSceneName}");
                SceneManager.LoadScene(_nextSceneName);
                return;
            }

            Debug.LogError($"[Logo] 场景「{_nextSceneName}」不在 Build Settings 中。请检查 Project Settings > Build Settings 是否包含该场景。");

            // 兜底：不要 LoadScene(0)，那会加载 Logo 自己造成死循环
            if (SceneManager.sceneCountInBuildSettings > 1)
            {
                Debug.LogWarning("[Logo] 回退加载 Build Settings 索引 1 的场景");
                SceneManager.LoadScene(1);
            }
            else
            {
                Debug.LogError("[Logo] Build Settings 只包含 1 个场景，无法跳转。");
            }
        }
    }
}
