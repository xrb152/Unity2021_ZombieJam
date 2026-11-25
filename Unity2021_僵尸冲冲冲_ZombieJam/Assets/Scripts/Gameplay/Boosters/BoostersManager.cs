using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Ad;
using Gameplay.Configs.Boosters;
using Gameplay.Curencies;
using Gameplay.Enums;
using Gameplay.Windows.Gameplay;
using Infrastructure.PersistenceProgress;
using Infrastructure.StaticData;
using UnityEngine;

namespace Gameplay.Boosters
{
    public class BoostersManager : IBoostersManager
    {
        private const int PRICE_BOOSTER = 5;

        public event Action<EBoosterType> Activate;

        private readonly IProgressService _progressService;
        private IGameplayModel _gameplayModel;
        private ICurrenciesModel _currenciesModel;

        private BoostersProgress _boostersProgress;
        private BoostersConfig _boostersConfig;

        private Dictionary<EBoosterType, bool> _boosters = new Dictionary<EBoosterType, bool>();

        public BoostersManager(IProgressService progressService, GameStaticData gameStaticData,
            ICurrenciesModel currenciesModel)
        {
            _progressService = progressService;
            _boostersConfig = gameStaticData.BoostersConfig;
            _currenciesModel = currenciesModel;

            progressService.OnLoaded += Loaded;
        }

        public void Initialize(IGameplayModel gameplayModel)
        {
            _gameplayModel = gameplayModel;

            _gameplayModel.OnStartWave += ResetBoosters;
        }

        private void Loaded()
        {
            _progressService.OnLoaded -= Loaded;

            _boostersProgress = _progressService.PlayerProgress.BoostersProgress;

            foreach (var boosterProgressData in _boostersConfig.Boosters)
            {
                _boosters.Add(boosterProgressData.BoosterType, false);
            }

            _boosters.Add(EBoosterType.Relocation, false);
        }

        public void ActivateBooster(EBoosterType boosterType)
        {
            if (_boosters[boosterType] && boosterType != EBoosterType.Relocation) return;

            var progress = _boostersProgress.GetOrCreate(boosterType);
            if (progress.Value <= 0)
            {
                // 没有库存时先给一次「看广告免费拿」的机会，广告真的失败才退回扣钻石。
                AdRewardService.ShowVideo("免费获得道具", () => { GrantBooster(boosterType); }, error =>
                {
                    if (error == AdRewardService.CancelReason)
                    {
                        Debug.Log($"[激励广告] 玩家取消观看 {boosterType} 广告");
                        return;
                    }

                    Debug.Log($"[激励广告] 道具广告未完成（{error}），尝试消耗 {PRICE_BOOSTER} 钻石");
                    BuyBoosterByHardCurrency(boosterType);
                });

                return;
            }

            _boosters[boosterType] = true;

            Activate?.Invoke(boosterType);
        }

        /// <summary>发放一个道具并立即激活（广告看完后调用）。</summary>
        private void GrantBooster(EBoosterType boosterType)
        {
            AddBooster(boosterType, 1);
            _boosters[boosterType] = true;
            Activate?.Invoke(boosterType);
        }

        /// <summary>广告不可用时的备用路径：花钻石买一个道具。</summary>
        private void BuyBoosterByHardCurrency(EBoosterType boosterType)
        {
            var hardCurrencyProgress =
                _currenciesModel.CurrenciesProgress.GetOrCreate(ECurrencyType.HardCurrency);

            if (hardCurrencyProgress.Value < PRICE_BOOSTER)
            {
                Debug.Log($"{boosterType} not available（钻石不足 {hardCurrencyProgress.Value}/{PRICE_BOOSTER}）");
                return;
            }

            _currenciesModel.Consume(ECurrencyType.HardCurrency, PRICE_BOOSTER);
            GrantBooster(boosterType);
        }

        public void ConsumeBooster(EBoosterType boosterType, int value)
        {
            var progress = _boostersProgress.GetOrCreate(boosterType);
            progress.Value -= value;
        }

        public void AddBooster(EBoosterType boosterType, int value)
        {
            var progress = _boostersProgress.GetOrCreate(boosterType);
            progress.Value += value;
        }

        public BoosterProgressData GetBoosterProgressData(EBoosterType eBoosterType)
        {
            return _boostersProgress.GetOrCreate(eBoosterType);
        }

        private void ResetBoosters(ERegionType regionType ,int waveIndex)
        {
            for (int i = 0; i < _boosters.Count; i++)
            {
                var boosterType = _boosters.ToList()[i].Key;
                _boosters[boosterType] = false;
            }
        }
    }
}