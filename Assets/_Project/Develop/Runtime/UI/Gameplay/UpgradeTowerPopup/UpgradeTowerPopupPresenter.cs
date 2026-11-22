using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Towers;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.Features.Level;
using Assets._Project.Develop.Runtime.Gameplay.Features.Raycast;
using Assets._Project.Develop.Runtime.Gameplay.Features.Towers;
using Assets._Project.Develop.Runtime.UI.Core.Sounds;
using Assets._Project.Develop.Runtime.UI.Gameplay.TowerPopups;
using Assets._Project.Develop.Runtime.Utilities.CameraManagment;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagment;
using Assets._Project.Develop.Runtime.Utilities.Localization;
using Assets._Project.Develop.Runtime.Utilities.Wallet;
using System;
using UnityEngine;
#if UNITY_EDITOR
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
#endif

namespace Assets._Project.Develop.Runtime.UI.Gameplay.UpgradeTowerPopup
{
    // Улучшение башни I–III уровня и продажа. Заряд улучшения показывает на круге дальности башни прирост до дальности
    // следующего уровня.
    public class UpgradeTowerPopupPresenter : TowerPopupPresenterBase<UpgradeTowerPopupButtons>
    {
        // Нажатие на кнопку улучшения — любое, в том числе заряжающее; нужно указателю обучения
        public event Action UpgradeClicked;

        private readonly UpgradeTowerPopupView _view;

        private readonly TowersFactory _towersFactory;
        private readonly TowersListConfig _towersListConfig;

        public UpgradeTowerPopupPresenter(
            ICoroutinesPerformer coroutinesPerformer,
            RayShooterService rayShooterService,
            UpgradeTowerPopupView view,
            Entity sourceTower,
            TowersFactory towersFactory,
            TowersListConfig towersListConfig,
            TowersPurchaseService towersPurchaseService,
            WalletService walletService,
            TowersPlaceholdersService towersPlaceholdersService,
            BoundedOrthoCamera camera,
            UISoundPlayer uiSoundPlayer,
            LocalizationService localizationService)
            : base(
                coroutinesPerformer,
                rayShooterService,
                view,
                sourceTower,
                towersPurchaseService,
                towersPlaceholdersService,
                walletService,
                camera,
                uiSoundPlayer,
                localizationService)
        {
            _view = view;
            _towersFactory = towersFactory;
            _towersListConfig = towersListConfig;
        }

        public TowerTypes TowerType => Source.TowerType.Value;

        public RectTransform UpgradeButtonRect => _view.UpgradeButtonRect;

        private int NextTowerLevel => Source.TowerLevel.Value + 1;

        private ShootingTowerConfig NextLevelConfig => _towersListConfig.GetBy(TowerType, NextTowerLevel);

        public override void Initialize()
        {
            base.Initialize();

            _view.ButtonClicked += OnButtonClicked;
            _view.ButtonClicked += OnAnyButtonClicked;
        }

        public override void Dispose()
        {
            base.Dispose();

            _view.ButtonClicked -= OnButtonClicked;
            _view.ButtonClicked -= OnAnyButtonClicked;
        }

        protected override void OnPreHide()
        {
            base.OnPreHide();

            _view.ButtonClicked -= OnButtonClicked;
            _view.ButtonClicked -= OnAnyButtonClicked;
        }

        protected override void RefreshButtons()
        {
            int upgradeCost = TowersPurchaseService.GetPriceFor(TowerType, NextTowerLevel);

            _view.SetUpgradeButton(GetStateFor(UpgradeTowerPopupButtons.Upgrade), upgradeCost);
            _view.SetSellButton(GetStateFor(UpgradeTowerPopupButtons.Sell));
        }

        protected override bool IsAvailable(UpgradeTowerPopupButtons button)
        {
            switch (button)
            {
                case UpgradeTowerPopupButtons.Upgrade:
                    return TowersPurchaseService.EnoughGoldFor(TowerType, NextTowerLevel);

                case UpgradeTowerPopupButtons.Sell:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(button));
            }
        }

        protected override TowerTipContent GetTip(UpgradeTowerPopupButtons button)
        {
            switch (button)
            {
                case UpgradeTowerPopupButtons.Upgrade:
                    return GetTowerTip(NextLevelConfig);

                case UpgradeTowerPopupButtons.Sell:
                    return GetSellTip();

                default:
                    throw new ArgumentOutOfRangeException(nameof(button));
            }
        }

        protected override void OnArmed(UpgradeTowerPopupButtons button)
        {
            if (button == UpgradeTowerPopupButtons.Upgrade)
                Source.ShootingRangeZone.ShowGrowth(NextLevelConfig.AttackRange);
        }

        protected override void OnDisarmed(UpgradeTowerPopupButtons button) => Source.ShootingRangeZone.HideGrowth();

        protected override void Activate(UpgradeTowerPopupButtons button)
        {
            switch (button)
            {
                case UpgradeTowerPopupButtons.Upgrade:
                    UpgradeTower();
                    break;

                case UpgradeTowerPopupButtons.Sell:
                    SellSource();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(button));
            }
        }

        private void OnAnyButtonClicked(UpgradeTowerPopupButtons button)
        {
            if (button == UpgradeTowerPopupButtons.Upgrade)
                UpgradeClicked?.Invoke();
        }

        private void UpgradeTower()
        {
            if (TowersPurchaseService.TryBuyTower(TowerType, NextTowerLevel) == false)
                return;

            #if UNITY_EDITOR
                // Покупка уровня с местом башни — в лог плейтеста из редактора уровней
                PlaytestHooks.NotifyTowerBought(TowerType, NextTowerLevel, NextLevelConfig.Cost, Source.Transform.position);
            #endif

            _towersFactory.Create(Source.Transform.position, NextLevelConfig, NextTowerLevel);

            Source.SelfReleaseRequested.Value = true;
            OnCloseRequest(false);
        }
    }
}
