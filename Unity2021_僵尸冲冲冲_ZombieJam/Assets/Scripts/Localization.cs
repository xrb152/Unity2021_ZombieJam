using Gameplay.Ad;
using Gameplay.Configs.Zombies;
using Gameplay.Enums;

namespace Utils
{
    /// <summary>
    /// 界面中文化：把枚举与代码里的英文文案统一映射成简体中文。
    /// 只影响显示层，不改变枚举本身的存储值。
    /// </summary>
    public static class Localization
    {
        public static string ToCn(this ERegionType type)
        {
            switch (type)
            {
                case ERegionType.Awakening:
                    return "觉醒之地";
                case ERegionType.Cemetery:
                    return "墓地";
                case ERegionType.Park:
                    return "公园";
                case ERegionType.Parking:
                    return "停车场";
                case ERegionType.ResidentialAreas:
                    return "住宅区";
                default:
                    return "未知区域";
            }
        }

        public static string ToCn(this EZombieNames name)
        {
            switch (name)
            {
                case EZombieNames.Zombie:
                    return "僵尸";
                case EZombieNames.BrainThrower:
                    return "投脑手";
                case EZombieNames.WalkingCoffin:
                    return "行走棺材";
                case EZombieNames.Hitchhiker:
                    return "搭车客";
                case EZombieNames.ArmoredZombie:
                    return "铁甲僵尸";
                case EZombieNames.ZombieScooter:
                    return "僵尸滑板车";
                default:
                    return "僵尸";
            }
        }

        public static string ToCn(this EParameter parameter)
        {
            switch (parameter)
            {
                case EParameter.Health:
                    return "生命";
                case EParameter.Damage:
                    return "伤害";
                case EParameter.RadiusAttack:
                    return "范围伤害";
                case EParameter.AttackRate:
                    return "攻击间隔";
                case EParameter.TravelSpeed:
                    return "移动速度";
                case EParameter.AttackSpeed:
                    return "攻击速度";
                case EParameter.AgressiveRange:
                    return "攻击范围";
                default:
                    return "属性";
            }
        }

        public static string ToCn(this EShopProductType type)
        {
            switch (type)
            {
                case EShopProductType.SimpleBox:
                    return "普通宝箱";
                case EShopProductType.RareBox:
                    return "稀有宝箱";
                case EShopProductType.EpicBox:
                    return "史诗宝箱";
                case EShopProductType.NoAds:
                    return "去除广告";
                case EShopProductType.LittleSoft:
                    return "少量金币";
                case EShopProductType.MediumSoft:
                    return "中量金币";
                case EShopProductType.ManySoft:
                    return "大量金币";
                case EShopProductType.LittleHard:
                    return "少量钻石";
                case EShopProductType.MediumHard:
                    return "中量钻石";
                case EShopProductType.ManyHard:
                    return "大量钻石";
                default:
                    return "商品";
            }
        }

        public static string ToCn(this EAdsType type)
        {
            switch (type)
            {
                case EAdsType.Reward:
                    return "激励视频";
                case EAdsType.Interstitial:
                    return "插屏广告";
                default:
                    return "无";
            }
        }
    }
}
