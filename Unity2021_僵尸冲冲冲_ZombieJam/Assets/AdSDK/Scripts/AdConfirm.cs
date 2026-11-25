using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 激励广告弹窗：展示「奖励标题 + 数量 + 图标」，点确认才真正播放抖音激励视频，播放完成才回调发奖。
///
/// 弹窗结构（Resources/AdConfirm.prefab）：
///   panel/award/tips (1)  标题，如「增加钻石」
///   panel/award/value     数量，如「+50」
///   panel/award/Image     货币图标
///   panel/award/tips      说明，如「看广告获得奖励」
/// </summary>
public class AdConfirm : MonoBehaviour
{
    private static AdConfirm instance;

    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnConfirm;

    [Header("奖励展示（留空则运行时按路径自动查找）")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text amountText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private Text tipsText;

    [Header("场景里直接 SetActive 打开时发放的默认奖励")]
    [SerializeField] private int defaultDiamondReward = AdRewardService.DefaultDiamondReward;

    private Action onCompleted;
    private Action<string> onFail;

    /// <summary>本次弹窗是否由 <see cref="Show(string,string,Sprite,Action,Action{string})"/> 打开。</summary>
    private bool _openedByShow;

    /// <summary>
    /// 场景里预放的 AdConfirm 可能是未激活状态，此时 Awake 要等到 Show 里 SetActive(true) 才执行。
    /// 如果不打这个标记，Awake 末尾的 SetActive(false) 会把刚打开的弹窗立刻关掉。
    /// </summary>
    private bool _suppressHideOnAwake;

    private Vector2 _amountPosWithIcon;
    private bool _posCached;

    /// <summary>弹窗当前是否可见（含场景里直接 SetActive 打开的情况）。</summary>
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    /// <summary>
    /// 兼容旧调用：只关心「看完广告后做什么」，弹窗沿用预制体里的默认文案。
    /// </summary>
    public static void Show(Action callBack)
    {
        Show(null, null, null, callBack, null);
    }

    /// <summary>
    /// 展示激励广告弹窗。
    /// </summary>
    /// <param name="title">标题，如「增加钻石」；null 表示不改动预制体默认值。</param>
    /// <param name="amount">数量文案，如「+50」；null 表示隐藏数量。</param>
    /// <param name="icon">货币图标；null 表示隐藏图标。</param>
    /// <param name="onCompleted">广告播放完成后回调（此时才发奖）。</param>
    /// <param name="onFail">弹窗被关闭或广告失败时回调。</param>
    public static void Show(string title, string amount, Sprite icon, Action onCompleted,
        Action<string> onFail = null)
    {
        var ins = EnsureInstance();
        if (ins == null)
        {
            onFail?.Invoke("弹窗加载失败");
            return;
        }

        ins.onCompleted = onCompleted;
        ins.onFail = onFail;
        ins._openedByShow = true;
        ins._suppressHideOnAwake = true;

        ins.ApplyContent(title, amount, icon);

        ins.gameObject.SetActive(true);
        ins.transform.SetAsLastSibling();
    }

    private static AdConfirm EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        // 场景里可能已经摆了一个 AdConfirm（Gameplay 场景里就有），优先复用它。
        instance = FindObjectOfType<AdConfirm>(true);
        if (instance != null)
        {
            instance.EnsureReferences();
            return instance;
        }

        var prefab = Resources.Load<GameObject>("AdConfirm");
        if (prefab == null)
        {
            Debug.LogError("AdConfirm 预制体未找到，请确保其位于 Resources 文件夹中。");
            return null;
        }

        var go = Instantiate(prefab);
        instance = go.GetComponent<AdConfirm>();
        if (instance == null)
        {
            Debug.LogError("AdConfirm 脚本未附加到预制体上，请检查预制体设置。");
            Destroy(go);
            return null;
        }

        instance.EnsureReferences();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureReferences();

        if (btnClose != null)
        {
            btnClose.onClick.AddListener(OnClose);
        }

        if (btnConfirm != null)
        {
            btnConfirm.onClick.AddListener(OnConfirm);
        }

        // 只有「不是 Show 打开的」才需要自我隐藏；否则会把刚弹出来的窗口立刻关掉。
        if (!_suppressHideOnAwake)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>按路径补齐没有在 Inspector 里拖入的引用，避免改预制体也能用。</summary>
    private void EnsureReferences()
    {
        if (titleText == null)
        {
            titleText = FindText("panel/award/tips (1)");
        }

        if (amountText == null)
        {
            amountText = FindText("panel/award/value");
        }

        if (tipsText == null)
        {
            tipsText = FindText("panel/award/tips");
        }

        if (rewardIcon == null)
        {
            var t = transform.Find("panel/award/Image");
            if (t != null)
            {
                rewardIcon = t.GetComponent<Image>();
            }
        }

        if (!_posCached && amountText != null)
        {
            _amountPosWithIcon = amountText.rectTransform.anchoredPosition;
            _posCached = true;
        }
    }

    private Text FindText(string path)
    {
        var t = transform.Find(path);
        return t != null ? t.GetComponent<Text>() : null;
    }

    private void ApplyContent(string title, string amount, Sprite icon)
    {
        var hasAmount = !string.IsNullOrEmpty(amount);
        var hasIcon = icon != null;

        if (titleText != null && !string.IsNullOrEmpty(title))
        {
            titleText.text = title;
        }

        if (tipsText != null)
        {
            tipsText.text = "看广告获得奖励";
        }

        if (amountText != null)
        {
            amountText.gameObject.SetActive(hasAmount);
            if (hasAmount)
            {
                amountText.text = amount;
                // 没有图标时把数字挪回居中，否则会偏右留一个空位
                amountText.rectTransform.anchoredPosition = hasIcon
                    ? _amountPosWithIcon
                    : new Vector2(0f, _amountPosWithIcon.y);
            }
        }

        if (rewardIcon != null)
        {
            rewardIcon.gameObject.SetActive(hasIcon);
            if (hasIcon)
            {
                rewardIcon.sprite = icon;
            }
        }
    }

    /// <summary>
    /// 场景里如果直接用按钮把 AdConfirm SetActive(true) 打开（没有调用 Show），
    /// 这里补上默认的「看广告领 50 钻石」行为，否则看完广告什么都不会发。
    /// </summary>
    private void OnEnable()
    {
        if (_openedByShow)
        {
            return;
        }

        ApplyDefaultReward();
    }

    private void ApplyDefaultReward()
    {
        var reward = AdReward.Diamond(defaultDiamondReward);

        ApplyContent(
            reward.Title,
            $"+{reward.Amount}",
            AdRewardService.GetSprite(reward.Kind));

        onCompleted = () =>
        {
            AdRewardService.Grant(reward);
            Debug.Log($"[激励广告] 默认奖励已发放：{reward}，当前余额 {AdRewardService.GetBalance(reward.Kind)}");
        };
        onFail = null;
    }

    private void OnClose()
    {
        var fail = onFail;
        ClearCallbacks();
        ResetButton();
        gameObject.SetActive(false);
        fail?.Invoke(AdRewardService.CancelReason);
    }

    private void OnConfirm()
    {
        if (btnConfirm != null)
        {
            btnConfirm.interactable = false;
        }

#if UNITY_EDITOR
        // 编辑器里没有 TTSDK，直接视为播放完成，方便调试发奖链路。
        Debug.Log("[激励广告] 编辑器模式：跳过真实广告，直接发放奖励");
        FinishSuccess();
#else
        if (AdManager.Instance == null)
        {
            Debug.LogError("[广告] 场景里没有 AdManager，无法播放广告");
            ResetButton();
            OnAdError("广告服务未初始化");
            return;
        }

        AdManager.Instance.ShowRewardedVideoAd(OnAdWatched, OnAdError);
#endif
    }

    private void OnAdWatched()
    {
        Debug.Log("[激励广告] 广告观看完成，开始发奖");
        FinishSuccess();
    }

    private void FinishSuccess()
    {
        var done = onCompleted;
        ClearCallbacks();
        ResetButton();

        // 先发奖再隐藏，避免发奖过程中玩家重复点击确认。
        done?.Invoke();
        gameObject.SetActive(false);
    }

    private void OnAdError(string errorMessage)
    {
        Debug.LogError($"广告播放失败: {errorMessage}");
        ResetButton();

        var fail = onFail;
        ClearCallbacks();
        gameObject.SetActive(false);

        ShowErrorToast();
        fail?.Invoke(errorMessage);
    }

    private void ResetButton()
    {
        if (btnConfirm != null)
        {
            btnConfirm.interactable = true;
        }
    }

    private void ClearCallbacks()
    {
        onCompleted = null;
        onFail = null;
        _openedByShow = false;
    }

    private void ShowErrorToast()
    {
        var adErrorPrefab = Resources.Load<GameObject>("AdError");
        if (adErrorPrefab != null)
        {
            var adErrorInstance = Instantiate(adErrorPrefab, transform.position, Quaternion.identity);
            Destroy(adErrorInstance, 1.5f);
        }
        else
        {
            Debug.LogError("AdError 预制体未找到，请确保其位于 Resources 文件夹中。");
        }
    }
}
