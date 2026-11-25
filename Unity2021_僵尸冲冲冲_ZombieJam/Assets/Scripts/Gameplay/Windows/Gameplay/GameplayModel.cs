using System;
using Gameplay.Ad;
using Gameplay.Configs.Region;
using Gameplay.Curencies;
using Gameplay.Enums;
using Gameplay.Level;
using Gameplay.Parking;
using Gameplay.Tutorial;
using Gameplay.Units;
using Infrastructure.Timer;
using Infrastructure.Windows;
using UnityEngine;
using Utils;

namespace Gameplay.Windows.Gameplay
{
    public class GameplayModel : IGameplayModel
    {
        private const int PRICE_REVIVE = 5;

        public event Action OnRevive;
        public event Action OnUnitFirstDoDamage;
        public event Action<ERegionType, int> OnToTheNextWave;
        public event Action<ERegionType, int> OnWaveCompleted;

        public event Action<ERegionType, int> OnWaveLoose;
        public event Action<ERegionType, int> OnStartWave;
        public event Action<int> OnEnemyDied;
        public event Action CreatedTimer;

        private readonly IRegionManager _regionManager;
        private readonly ILevelModel _levelModel;
        private readonly IAdsService _adsService;
        private readonly TimerService _timerService;
        private readonly ITutorialService _tutorialService;
        private readonly IWindowService _windowService;
        private readonly ICurrenciesModel _currenciesModel;

        private ZombieSpawner _zombieSpawner;

        public GameplayModel(IRegionManager regionManager, ILevelModel levelModel,
            IAdsService adsService, ITutorialService tutorialService, TimerService timerService,
            IWindowService windowService, ICurrenciesModel currenciesModel)
        {
            _regionManager = regionManager;
            _levelModel = levelModel;
            _adsService = adsService;
            _tutorialService = tutorialService;
            _timerService = timerService;
            _windowService = windowService;
            _currenciesModel = currenciesModel;
        }

        public bool IsAvailableRevive { get; set; } = true;
        public bool IsStartWave { get; set; }
        public bool IsWasFirstDamage { get; set; }
        public bool IsWaveCompleted { get; set; }
        public int TargetsCount { get; set; }
        public TimeModel Timer { get; set; }
        public int GameplayTimeDuration { get; set; }
        public EWaveType WaveType { get; set; }
        public ETutorialState TutorialState => _tutorialService.CurrentState;

        private DateTime _startGameplayDateTime;

        public void InitializeZombieSpawner(ZombieSpawner zombieSpawner)
        {
            _zombieSpawner = zombieSpawner;
        }

        public void NextWave()
        {
            IsWaveCompleted = false;

            var progress = GetCurrentRegionProgress();
            _regionManager.WaveCompleted();
            _levelModel.AddExperience(true);
            IsAvailableRevive = true;
            StopWave();
            OnToTheNextWave?.Invoke(progress.CurrentRegionType, progress.GetCurrentRegion().CurrentWaweIndex);
        }

        // 原 async void + await Task.Delay(3000)：抖音 / WebGL 下续体永不恢复，
        // 波次打完后胜利结算窗口永远不弹，玩家卡在空场景里。
        public void WaveCompleted()
        {
            IsWaveCompleted = true;

            if (Timer != null)
            {
                Timer.IsWork = false;
            }

            GameplayTimeDuration = (DateTime.Now - _startGameplayDateTime).Seconds;
            
            var progress = GetCurrentRegionProgress();
            OnWaveCompleted?.Invoke(progress.CurrentRegionType, progress.GetCurrentRegion().CurrentWaweIndex);

            DelayedCall.After(3000, () =>
            {
                if (IsWaveCompleted)
                {
                    _windowService.Open(WindowType.Victory);
                }
            });
        }

        public void StartWave()
        {
            _windowService.Open(WindowType.Gameplay);
            _regionManager.Progress.LevelStartCount++;
            _startGameplayDateTime = DateTime.Now;
            IsWaveCompleted = false;
            IsStartWave = true;
            IsWasFirstDamage = false;
            
            ActivateVignette(_regionManager.ProgressData);
            InitializeTimer();
            OnStartWave?.Invoke(_regionManager.ProgressData.ERegionType, _regionManager.ProgressData.CurrentWaweIndex);
        }

        public void LooseWave()
        {
            var progress = GetCurrentRegionProgress();
            OnWaveLoose?.Invoke(progress.CurrentRegionType, progress.GetCurrentRegion().CurrentWaweIndex);
        }

        private void InitializeTimer()
        {
            RemoveTimer();

            if (_tutorialService.CurrentState == ETutorialState.Swipe) return;

            var config = _regionManager.RegionConfig.Waves[_regionManager.ProgressData.CurrentWaweIndex];
            if (config.WaveTimerType == EWaveTimerType.Beginning)
            {
                CreateTimer(config);
            }
            else if (config.WaveTimerType == EWaveTimerType.FirstDamage)
            {
                OnUnitFirstDoDamage += CreateTimerOnFirstDamage;
            }
        }

        private void CreateTimerOnFirstDamage()
        {
            OnUnitFirstDoDamage -= CreateTimerOnFirstDamage;

            if (!IsWasFirstDamage) return;

            var config = _regionManager.RegionConfig.Waves[_regionManager.ProgressData.CurrentWaweIndex];
            CreateTimer(config);
        }

        private void CreateTimer(WaveConfigData config)
        {
            Timer = _timerService.CreateTimer(config.WaveTimerType.ToString(), config.TimerDuration);
            Timer.Stopped += OnStopTimer;
            CreatedTimer?.Invoke();
        }

        public int GetExperience(bool isWin)
        {
            return _levelModel.GetExperience(isWin);
        }

        public RegionProgress GetCurrentRegionProgress()
        {
            return _regionManager.Progress;
        }

        public RegionConfigData GetRegionConfig()
        {
            return _regionManager.RegionConfig;
        }

        public void StartReviveForAds()
        {
            // 优先走激励视频：看完广告免费复活；广告不可用或被关闭时才退回扣钻石。
            AdRewardService.ShowVideo("免费复活", () =>
            {
                Debug.Log("[激励广告] 复活广告观看完成，免费复活");
                ReviveUnits();
            }, error =>
            {
                if (error == AdRewardService.CancelReason)
                {
                    // 玩家自己关掉了弹窗，不算广告失败，也不应该偷偷扣钻石。
                    Debug.Log("[激励广告] 玩家取消观看复活广告");
                    return;
                }

                Debug.Log($"[激励广告] 复活广告未完成（{error}），尝试消耗 {PRICE_REVIVE} 钻石复活");
                ReviveByHardCurrency();
            });
        }

        /// <summary>钻石复活（广告不可用时的备用路径）。</summary>
        private void ReviveByHardCurrency()
        {
            if (!IsAvailableRevive)
            {
                Debug.Log("[复活] 本局已使用过复活次数");
                return;
            }

            var hardCurrencyProgress =
                _currenciesModel.CurrenciesProgress.GetOrCreate(ECurrencyType.HardCurrency);

            if (hardCurrencyProgress.Value >= PRICE_REVIVE)
            {
                _currenciesModel.Consume(ECurrencyType.HardCurrency, PRICE_REVIVE);
                ReviveUnits();
            }
            else
            {
                Debug.LogWarning($"[复活] 钻石不足（{hardCurrencyProgress.Value}/{PRICE_REVIVE}），且广告不可用，无法复活");
            }
        }

        private void ReviveUnits()
        {
            IsAvailableRevive = false;
            OnRevive?.Invoke();
        }

        public void StopWave()
        {
            IsStartWave = false;
        }

        public void GetRewardForWave(bool isShowedAds)
        {
            _regionManager.GetRewardForWave(isShowedAds);
        }

        public void FirstDamage()
        {
            IsWasFirstDamage = true;
            OnUnitFirstDoDamage?.Invoke();
        }

        private void RemoveTimer()
        {
            if (Timer != null)
            {
                _timerService.RemoveTimer(Timer);
                Timer = null;
            }
        }

        public void EnemyDied(int index, EEnemyType eEnemyType)
        {
            OnEnemyDied?.Invoke(index);
        }

        public void SetWaveType(EWaveType waveType)
        {
            WaveType = waveType;
        }

        public Unit IsHaveTank()
        {
            return _zombieSpawner.Zombies.Find(x => x.Config.Type == EUnitClass.Tank);
        }

        private void OnStopTimer(TimeModel timeModel)
        {
            _windowService.Open(WindowType.Died);
        }
        
        private void ActivateVignette(RegionProgressData regionProgressData)
        {
            if (regionProgressData.CurrentWaweIndex > 3)
            {
                if (!_windowService.IsOpen(WindowType.Vignette))
                {
                    _windowService.Open(WindowType.Vignette);
                }
            }
            else
            {
                if (_windowService.IsOpen(WindowType.Vignette))
                {
                    _windowService.Close(WindowType.Vignette);
                }
            }
        }
    }
}