using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.Gameplay.Features.InputFeature;
using Assets._Project.Develop.Runtime.Gameplay.Features.Level;
using Assets._Project.Develop.Runtime.Gameplay.Features.Towers;
using Assets._Project.Develop.Runtime.UI.Gameplay;
using Assets._Project.Develop.Runtime.Utilities.CameraManagment;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagment;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Tutorial.Steps
{
    // Собирает шаги обучения с их зависимостями: сервису обучения остаётся решать, когда какой шаг показать.
    public class TutorialStepsFactory
    {
        private readonly TutorialPointersInfo _pointers;
        private readonly GameplayPopupService _popupService;
        private readonly BoundedOrthoCamera _camera;
        private readonly IInputService _inputService;
        private readonly ICoroutinesPerformer _coroutinesPerformer;
        private readonly TowersPlaceholdersService _placeholdersService;
        private readonly BuiltTowersService _builtTowersService;
        private readonly TowersPurchaseService _towersPurchaseService;

        public TutorialStepsFactory(
            TutorialConfig config,
            GameplayPopupService popupService,
            BoundedOrthoCamera camera,
            IInputService inputService,
            ICoroutinesPerformer coroutinesPerformer,
            TowersPlaceholdersService placeholdersService,
            BuiltTowersService builtTowersService,
            TowersPurchaseService towersPurchaseService)
        {
            _pointers = config.Pointers;
            _popupService = popupService;
            _camera = camera;
            _inputService = inputService;
            _coroutinesPerformer = coroutinesPerformer;
            _placeholdersService = placeholdersService;
            _builtTowersService = builtTowersService;
            _towersPurchaseService = towersPurchaseService;
        }

        public TutorialSliderStep CreateSliderStep(TutorialSliderInfo info) => new(info);

        // Иконки взора — по устройству ввода: жесты на телефоне и планшете, клавиши и мышь на ПК
        public TutorialCameraHintStep CreateCameraHintStep()
            => new(_pointers, _camera, _coroutinesPerformer, _inputService is MobileInput);

        public TutorialBuildPointerStep CreateBuildPointerStep()
            => new(_pointers, _placeholdersService, _builtTowersService, _popupService);

        public TutorialTowerPointerStep CreateTowerPointerStep()
            => new(_pointers, _builtTowersService, _popupService, _coroutinesPerformer);

        public TutorialStageCallPointerStep CreateStartWavePointerStep()
            => new(_pointers.StartWave, _pointers.HolePadding, _popupService, isSkipPopup: false);

        public TutorialStageCallPointerStep CreateSkipWavePointerStep()
            => new(_pointers.SkipWave, _pointers.HolePadding, _popupService, isSkipPopup: true);

        public TutorialUpgradePointerStep CreateUpgradePointerStep()
            => new(_pointers, _builtTowersService, _towersPurchaseService, _popupService);
    }
}
