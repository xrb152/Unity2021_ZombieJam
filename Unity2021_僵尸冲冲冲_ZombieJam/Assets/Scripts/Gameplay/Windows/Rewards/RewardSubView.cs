using System;
using DG.Tweening;
using Gameplay.Boosters;
using Gameplay.Configs.Zombies;
using Gameplay.Enums;
using Infrastructure.Windows.MVVM.SubView;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Windows.Rewards
{
    public class RewardSubView : SubView<RewardSubViewData>
    {
        [SerializeField] private Image _sprite;
        [SerializeField] private Image _lightFrame;
        [SerializeField] private GameObject _lightParticle;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private GameObject _tooltip;
        [SerializeField] private TextMeshProUGUI _tooltipText;
        [SerializeField] private Button _button;

        public int Value { get; set; }
        private string _id;
        private bool _isRandomCard;
        private EResourceType _eResourceType;

        private Tween _tween;

        public override void Initialize(RewardSubViewData data)
        {
            _eResourceType = data.ResourceType;
            _id = data.ID;
            _isRandomCard = data.isRandomCard;

            Value = data.Value;
            _valueText.text = data.Value.ToString();
            _sprite.sprite = data.Sprite;
            _lightFrame.gameObject.SetActive(data.ResourceType == EResourceType.Card);
            _lightParticle.SetActive(data.isRandomCard);

            _tooltipText.text = GetTooltipText();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(ShowTooltip);
            _tooltip.SetActive(false);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(ShowTooltip);
        }

        private void ShowTooltip()
        {
            _tooltip.SetActive(!_tooltip.gameObject.activeSelf);
            _tween?.Kill();
            
            if (!_tooltip.activeSelf) return;

            _tween = DOVirtual.DelayedCall(3, () => { _tooltip.SetActive(false); });
        }

        private string GetTooltipText()
        {
            switch (_eResourceType)
            {
                case EResourceType.Booster:
                    Enum.TryParse<EBoosterType>(_id, out var boosterType);
                    return GetBoosterText(boosterType);
                case EResourceType.Currency:
                    Enum.TryParse<ECurrencyType>(_id, out var type);
                    return GetCurrencyText(type);

                case EResourceType.Card:
                    Enum.TryParse<EZombieNames>(_id, out var card);
                    return _isRandomCard ? "从已解锁的僵尸中随机获得一个" : GetCardText(card);

                default:
                    return null;
            }
        }

        private string GetCardText(EZombieNames zombieNames)
        {
            switch (zombieNames)
            {
                case EZombieNames.Hitchhiker:
                    return "搭车客，攻击力提升";
                case EZombieNames.Zombie:
                    return "最常见的僵尸";
                case EZombieNames.BrainThrower:
                    return "投脑手，远程伤害";
                case EZombieNames.WalkingCoffin:
                    return "行走棺材，生命值提升";
                case EZombieNames.ArmoredZombie:
                    return "铁甲僵尸，生命值提升";
                case EZombieNames.ZombieScooter:
                    return "僵尸滑板车，攻击力提升";
                default:
                    throw new ArgumentOutOfRangeException(nameof(zombieNames), zombieNames, null);
            }
        }

        private string GetBoosterText(EBoosterType boosterType)
        {
            switch (boosterType)
            {
                case EBoosterType.Relocation:
                    return "洗牌道具会改变场上僵尸的站位";
                case EBoosterType.IncreaseAttack:
                    return "道具「伤害提升」提高僵尸的伤害";
                case EBoosterType.IncreaseAttackSpeed:
                    return "道具「攻速提升」提高僵尸的攻击速度";
                case EBoosterType.IncreaseHP:
                    return "道具「生命提升」提高僵尸的生命值";
                case EBoosterType.IncreaseTravelSpeed:
                    return "道具「加速」提高僵尸的移动速度";
                default:
                    throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, null);
            }
        }


        private string GetCurrencyText(ECurrencyType currencyType)
        {
            switch (currencyType)
            {
                case ECurrencyType.HardCurrency:
                    return "钻石用于升级僵尸和购买宝箱";
                case ECurrencyType.SoftCurrency:
                    return "金币用于强化你的僵尸";

                default:
                    throw new ArgumentOutOfRangeException(nameof(currencyType), currencyType, null);
            }
        }
    }
}