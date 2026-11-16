using System;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.Features.Towers;
using Assets._Project.Develop.Runtime.UI.Core.Popups;
using Assets._Project.Develop.Runtime.UI.Gameplay;
using Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.UI.Gameplay.UpgradeTowerPopup;
using Assets._Project.Develop.Runtime.Utilities.Reactive;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Tutorial.Steps
{
    // Указатель на улучшение — без принуждения, в два шага: сначала на башню, улучшение которой по карману, затем, когда
    // по ней щёлкнули, на кнопку улучшения в её окне. Щелчок по затемнению или по кнопке закрывает указатель. Если игрок
    // уже улучшал башню сам, шаг не нужен.
    public class TutorialUpgradePointerStep : ITutorialStep
    {
        private const int FirstTowerLevel = 1;
        private const int AbilitiesTowerLevel = 4;

        private readonly TutorialPointersInfo _info;
        private readonly BuiltTowersService _builtTowersService;
        private readonly TowersPurchaseService _towersPurchaseService;
        private readonly GameplayPopupService _popupService;

        private TutorialPopupPresenter _popup;
        private Action _completed;
        private UpgradeTowerPopupPresenter _upgradePopup;

        public TutorialUpgradePointerStep(
            TutorialPointersInfo info,
            BuiltTowersService builtTowersService,
            TowersPurchaseService towersPurchaseService,
            GameplayPopupService popupService)
        {
            _info = info;
            _builtTowersService = builtTowersService;
            _towersPurchaseService = towersPurchaseService;
            _popupService = popupService;
        }

        public string Id => _info.Upgrade.Id;

        public bool IsNeeded()
        {
            foreach (Entity tower in _builtTowersService.Towers)
            {
                if (tower.TowerLevel.Value > FirstTowerLevel)
                    return false;
            }

            return true;
        }

        public bool HasTarget() => FindTarget() != null;

        public void Start(TutorialPopupPresenter popup, Action completed)
        {
            _popup = popup;
            _completed = completed;

            Entity tower = FindTarget();

            if (tower == null)
            {
                _completed?.Invoke();
                return;
            }

            _popup.DimmerClicked += Finish;
            _popupService.TowerPopupOpened += OnTowerPopupOpened;

            WorldTutorialPointerTarget target = new(tower.BoxCollider, _info.HolePadding);

            _popup.ShowPointer(target, _info.Upgrade.Side, _info.Upgrade.Text, _info.ContinueText);
        }

        public void Stop()
        {
            if (_popup != null)
                _popup.DimmerClicked -= Finish;

            _popupService.TowerPopupOpened -= OnTowerPopupOpened;

            if (_upgradePopup != null)
                _upgradePopup.UpgradeClicked -= Finish;

            _popup = null;
            _completed = null;
            _upgradePopup = null;
        }

        private void OnTowerPopupOpened(PopupPresenterBase towerPopup, Entity source)
        {
            if (towerPopup is not UpgradeTowerPopupPresenter upgradePopup || _upgradePopup != null)
                return;

            _upgradePopup = upgradePopup;
            _upgradePopup.UpgradeClicked += Finish;

            RectTutorialPointerTarget target = new(upgradePopup.UpgradeButtonRect, _info.HolePadding);

            _popup.ShowPointer(target, _info.Upgrade.SecondSide, _info.Upgrade.SecondText, _info.ContinueText);
        }

        private void Finish()
        {
            _popup.HidePointer();
            _completed?.Invoke();
        }

        // Башня, которую можно улучшить сейчас: не заморожена, не на уровне способностей и золота хватает
        private Entity FindTarget()
        {
            foreach (Entity tower in _builtTowersService.Towers)
            {
                if (IsUpgradable(tower))
                    return tower;
            }

            return null;
        }

        private bool IsUpgradable(Entity tower)
        {
            if (tower.SelfReleaseRequested.Value)
                return false;

            if (tower.TryGetIsFrozen(out ReactiveVariable<bool> isFrozen) && isFrozen.Value)
                return false;

            int level = tower.TowerLevel.Value;

            return level < AbilitiesTowerLevel && _towersPurchaseService.EnoughGoldFor(tower.TowerType.Value, level + 1);
        }
    }
}
