using System;
using Gameplay.Configs.Sprites;
using Gameplay.Curencies;
using Gameplay.Enums;
using Infrastructure.StaticData;
using UnityEngine;
using Zenject;

/// <summary>
/// 激励视频奖励的种类。
/// 对应项目里 ECurrencyType：Diamond -> HardCurrency（钻石），Coin -> SoftCurrency（金币）。
/// </summary>
public enum AdRewardKind
{
    Diamond,
    Coin,
}

/// <summary>
/// 一条激励视频奖励：种类 + 数量。
/// 例：AdReward.Diamond(50) 表示看完广告发 50 钻石。
/// </summary>
[Serializable]
public struct AdReward
{
    public AdRewardKind Kind;
    public int Amount;

    public AdReward(AdRewardKind kind, int amount)
    {
        Kind = kind;
        Amount = amount;
    }

    public static AdReward Diamond(int amount) => new AdReward(AdRewardKind.Diamond, amount);
    public static AdReward Coin(int amount) => new AdReward(AdRewardKind.Coin, amount);

    /// <summary>不发放任何货币（只播广告，例如「看广告免费复活」）。</summary>
    public static AdReward None => new AdReward(AdRewardKind.Diamond, 0);

    public bool IsEmpty => Amount <= 0;

    public ECurrencyType CurrencyType =>
        Kind == AdRewardKind.Diamond ? ECurrencyType.HardCurrency : ECurrencyType.SoftCurrency;

    /// <summary>弹窗标题文案。</summary>
    public string Title => Kind == AdRewardKind.Diamond ? "增加钻石" : "增加金币";

    public override string ToString() => $"{Title} +{Amount}";
}

/// <summary>
/// 激励视频统一入口：弹确认框 → 播放抖音激励视频 → 播放完成才发奖。
///
/// 设计要点：
/// 1. 发奖走项目真实的货币系统（<see cref="ICurrenciesModel"/> / Zenject 容器），
///    而不是自己用 PlayerPrefs 存一份，否则玩家在 Header 上看到的钻石不会变。
/// 2. 只有广告真正播放完成（onSuccess）才发奖；用户关掉弹窗、广告加载失败都不发。
/// 3. 全局防重入：弹窗还在的时候重复点击不会叠加弹窗、也不会重复发奖。
/// 4. 容器取不到货币模型时（例如单独打开某个场景调试）退化为 PlayerPrefs 存储，保证不报错。
/// </summary>
public static class AdRewardService
{
    /// <summary>默认钻石奖励：50。</summary>
    public const int DefaultDiamondReward = 50;

    /// <summary>默认金币奖励：200。</summary>
    public const int DefaultCoinReward = 200;

    /// <summary>玩家主动关掉弹窗时回传的原因，调用方可用它区分「自己不想看」和「广告真的失败了」。</summary>
    public const string CancelReason = "用户取消观看广告";

    private const string FallbackDiamondKey = "PlayerDiamonds";
    private const string FallbackCoinKey = "PlayerCoins";

    /// <summary>广告回调最长等待时间，超时后自动解锁，避免 SDK 不回调导致奖励入口永久卡死。</summary>
    private const float ShowTimeoutSeconds = 30f;

    private static bool _isShowing;
    private static float _showStartTime;

    /// <summary>当前是否有激励视频弹窗正在显示（含场景里直接 SetActive 打开的情况）。</summary>
    public static bool IsShowing => _isShowing || AdConfirm.IsOpen;

    /// <summary>看广告领钻石（默认 50）。</summary>
    public static void ShowDiamond(int amount = DefaultDiamondReward, Action onGranted = null,
        Action<string> onFail = null)
    {
        ShowReward(AdReward.Diamond(amount), null, onGranted, onFail);
    }

    /// <summary>看广告领金币（默认 200）。</summary>
    public static void ShowCoin(int amount = DefaultCoinReward, Action onGranted = null,
        Action<string> onFail = null)
    {
        ShowReward(AdReward.Coin(amount), null, onGranted, onFail);
    }

    /// <summary>
    /// 看广告领奖励（支持主奖励 + 若干附加奖励）。
    /// 弹窗上只显示主奖励，附加奖励在播放完成后一并静默发放。
    /// </summary>
    /// <param name="main">主奖励，决定弹窗标题与数字。</param>
    /// <param name="extra">附加奖励，可为 null。</param>
    /// <param name="onGranted">播放完成且已发奖后的回调。</param>
    /// <param name="onFail">弹窗关闭、广告失败等未能发奖时的回调，参数为原因。</param>
    public static void ShowReward(AdReward main, AdReward[] extra = null, Action onGranted = null,
        Action<string> onFail = null)
    {
        var title = main.IsEmpty ? "免费奖励" : main.Title;
        var amount = main.IsEmpty ? "免费" : $"+{main.Amount}";
        var icon = main.IsEmpty ? null : GetCurrencySprite(main.Kind);

        ShowInternal(title, amount, icon, main, extra, onGranted, onFail);
    }

    /// <summary>
    /// 只播广告不发奖（例如「看广告免费复活」「看广告解锁道具」）。
    /// </summary>
    public static void ShowVideo(string title, Action onWatched, Action<string> onFail = null)
    {
        ShowInternal(string.IsNullOrEmpty(title) ? "免费奖励" : title, "免费", null, AdReward.None, null,
            onWatched, onFail);
    }

    private static void ShowInternal(string title, string amount, Sprite icon, AdReward main, AdReward[] extra,
        Action onGranted, Action<string> onFail)
    {
        ResetIfTimeout();

        // 同时判断弹窗本身是否已经打开：场景里可能有按钮直接 SetActive 打开了 AdConfirm。
        if (_isShowing || AdConfirm.IsOpen)
        {
            Debug.LogWarning("[激励广告] 已有一个激励视频弹窗在显示，忽略本次请求");
            onFail?.Invoke("广告正在处理中");
            return;
        }

        _isShowing = true;
        _showStartTime = Time.realtimeSinceStartup;

        AdConfirm.Show(
            title,
            amount,
            icon,
            () =>
            {
                _isShowing = false;
                Grant(main);
                if (extra != null)
                {
                    for (var i = 0; i < extra.Length; i++)
                    {
                        Grant(extra[i]);
                    }
                }

                onGranted?.Invoke();
            },
            error =>
            {
                _isShowing = false;
                onFail?.Invoke(error);
            });
    }

    /// <summary>给玩家发一条奖励，返回是否成功。</summary>
    public static bool Grant(AdReward reward)
    {
        if (reward.IsEmpty)
        {
            return false;
        }

        var currencies = ResolveCurrencies();
        if (currencies != null)
        {
            try
            {
                currencies.Add(reward.CurrencyType, reward.Amount);
                Debug.Log($"[激励广告] 发放 {reward}，当前余额 {GetBalance(reward.Kind)}");
                return true;
            }
            catch (Exception e)
            {
                // 进度还没加载完时 CurrenciesModel 内部字段可能还是 null，这里兜一下，
                // 不让玩家「看完广告却什么都没拿到」。
                Debug.LogWarning($"[激励广告] 货币系统写入失败（{e.Message}），改用本地存储兜底");
            }
        }

        // 兜底：容器里没有货币模型（例如直接单场景调试），退化为本地存储，至少不丢奖励。
        var key = reward.Kind == AdRewardKind.Diamond ? FallbackDiamondKey : FallbackCoinKey;
        var total = Mathf.Max(0, PlayerPrefs.GetInt(key, 0) + reward.Amount);
        PlayerPrefs.SetInt(key, total);
        PlayerPrefs.Save();
        Debug.LogWarning($"[激励广告] 未取到 ICurrenciesModel，改用 PlayerPrefs 发放 {reward}，当前 {total}");
        return true;
    }

    /// <summary>读取当前余额；取不到货币系统时读本地兜底值。</summary>
    public static int GetBalance(AdRewardKind kind)
    {
        var currencies = ResolveCurrencies();
        if (currencies != null)
        {
            try
            {
                var type = kind == AdRewardKind.Diamond ? ECurrencyType.HardCurrency : ECurrencyType.SoftCurrency;
                return currencies.CurrenciesProgress.GetOrCreate(type).Value;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[激励广告] 读取余额失败：{e.Message}");
            }
        }

        var key = kind == AdRewardKind.Diamond ? FallbackDiamondKey : FallbackCoinKey;
        return PlayerPrefs.GetInt(key, 0);
    }

    /// <summary>SDK 长时间不回调时自动解锁，避免奖励入口再也点不动。</summary>
    private static void ResetIfTimeout()
    {
        if (!_isShowing)
        {
            return;
        }

        if (Time.realtimeSinceStartup - _showStartTime < ShowTimeoutSeconds)
        {
            return;
        }

        _isShowing = false;
        Debug.LogWarning($"[激励广告] 上一次广告请求超过 {ShowTimeoutSeconds} 秒没有回调，已自动解锁");
    }

    private static ICurrenciesModel ResolveCurrencies()
    {
        // 注意：ProjectContext.Instance 的 getter 在没有实例时会「顺手创建」一个新的容器，
        // 所以必须先判 HasInstance，否则会在运行时凭空多出一个容器，把模型重新构造一遍。
        if (!ProjectContext.HasInstance)
        {
            return null;
        }

        try
        {
            return ProjectContext.Instance.Container.TryResolve<ICurrenciesModel>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[激励广告] 解析 ICurrenciesModel 失败：{e.Message}");
            return null;
        }
    }

    /// <summary>取默认钻石奖励的图标；取不到返回 null。</summary>
    public static Sprite GetSprite(AdRewardKind kind)
    {
        return GetCurrencySprite(kind);
    }

    private static Sprite GetCurrencySprite(AdRewardKind kind)
    {
        if (!ProjectContext.HasInstance)
        {
            return null;
        }

        try
        {
            var staticData = ProjectContext.Instance.Container.TryResolve<GameStaticData>();
            if (staticData == null || staticData.SpritesConfig == null)
            {
                return null;
            }

            var type = kind == AdRewardKind.Diamond ? ECurrencyType.HardCurrency : ECurrencyType.SoftCurrency;
            return staticData.SpritesConfig.GetCurrencySprite(type);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[激励广告] 读取货币图标失败：{e.Message}");
            return null;
        }
    }
}
