using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 上的「免费奖励」按钮：点击弹出激励广告确认框，看完抖音激励视频后发放钻石（默认 50）。
///
/// 早先的实现依赖 HyperWe.CurrencyHolder / njector 这套外部依赖注入框架（本项目没有），
/// 后来改成写 PlayerPrefs —— 但那样玩家在 Header 上看到的钻石不会变，等于没发。
/// 现在统一走 <see cref="AdRewardService"/>：真实货币系统加钱 + 广告失败/取消不发奖。
/// </summary>
[RequireComponent(typeof(Button))]
public class FreeCoinButton : MonoBehaviour
{
    [Tooltip("奖励种类：钻石或金币")]
    [SerializeField] private AdRewardKind rewardKind = AdRewardKind.Diamond;

    [Tooltip("每次看完广告发放的数量")]
    [SerializeField] private int rewardAmount = AdRewardService.DefaultDiamondReward;

    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }
    }

    public void OnClick()
    {
        Debug.Log($"[激励广告] 点击免费奖励按钮，请求 {rewardKind} +{rewardAmount}");

        AdRewardService.ShowReward(
            new AdReward(rewardKind, rewardAmount),
            null,
            () => Debug.Log($"[激励广告] 奖励发放完成，当前余额 {AdRewardService.GetBalance(rewardKind)}"),
            error => Debug.Log($"[激励广告] 未发放奖励：{error}"));
    }
}
