using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Assets._Project.Develop.Runtime.Gameplay.Features.Raycast;
using Assets._Project.Develop.Runtime.UI.Core.Popups;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagment;
using Assets._Project.Develop.Runtime.Utilities.Localization;
using Assets._Project.Develop.Runtime.Utilities.Text;
using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.BottomInfoPopup
{
    public class TowerInfoPopupPresenter : PopupPresenterBase
    {
        private const string PhysicalDamageCaption = "физ. урон";
        private const string MagicDamageCaption = "маг. урон";

        private readonly BottomInfoPopupView _view;
        private readonly Entity _sourceTower;

        private readonly EntitiesLifeContext _entitiesLifeContext;
        private readonly LocalizationService _localizationService;

        private IDisposable _damageChangedDisposable;
        private IDisposable _frozenDisposable;

        public TowerInfoPopupPresenter(
            ICoroutinesPerformer coroutinesPerformer,
            RayShooterService rayShooterService,
            BottomInfoPopupView view,
            Entity sourceTower,
            EntitiesLifeContext entitiesLifeContext,
            LocalizationService localizationService)
            : base(coroutinesPerformer, rayShooterService)
        {
            _view = view;
            _sourceTower = sourceTower;
            _entitiesLifeContext = entitiesLifeContext;
            _localizationService = localizationService;
        }

        protected override PopupViewBase PopupView => _view;

        public override void Initialize()
        {
            base.Initialize();

            _damageChangedDisposable = _sourceTower.InstantAttackDamage.Subscribe(OnDamageChanged);
            _frozenDisposable = _sourceTower.IsFrozen.Subscribe(OnFrozenChanged);
            _entitiesLifeContext.Released += OnSomeEntityReleased;

            SetupInfo();
        }

        private void SetupInfo()
        {
            DamageTypes damageType = _sourceTower.InstantAttackDamageType.Value;
            string damageCaption = damageType == DamageTypes.Magic ? MagicDamageCaption : PhysicalDamageCaption;

            _view.SetTowerInfo(
                NonBreakingSpace.BeforeTrailingNumber(_localizationService.Translate(_sourceTower.TowerName.Value)),
                damageType,
                _localizationService.Translate(damageCaption),
                _localizationService.FormatDecimal(_sourceTower.AttacksPerSecond.Value),
                _localizationService.FormatDecimal(_sourceTower.InstantShootRange.Value));

            UpdateDamage();
        }

        private void UpdateDamage()
            => _view.SetTowerDamage(_localizationService.FormatDecimal(_sourceTower.InstantAttackDamage.Value));

        private void OnDamageChanged(float arg1, float damage) => UpdateDamage();

        public override void Dispose()
        {
            base.Dispose();

            _damageChangedDisposable?.Dispose();
            _frozenDisposable?.Dispose();
            _entitiesLifeContext.Released -= OnSomeEntityReleased;
        }

        protected override void OnClickedOutside(RaycastHit oldHit, RaycastHit newHit)
        {
            base.OnClickedOutside(oldHit, newHit);

            // Щелчок по своей башне ничего не делает, как и в окне башни (Р-38).
            if (newHit.IsOn(_sourceTower))
                return;

            OnCloseRequest();
        }

        private void OnSomeEntityReleased(Entity entity)
        {
            if (_sourceTower == entity)
                OnCloseRequest();
        }

        // Закрывается вместе с окном замороженной башни.
        private void OnFrozenChanged(bool oldValue, bool isFrozen)
        {
            if (isFrozen)
                OnCloseRequest();
        }
    }
}
