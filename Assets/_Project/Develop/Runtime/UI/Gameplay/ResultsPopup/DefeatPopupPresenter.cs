using Assets._Project.Develop.Runtime.Gameplay.Features.LevelStages;
using Assets._Project.Develop.Runtime.Gameplay.Features.Raycast;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Meta.Infrastructure;
using Assets._Project.Develop.Runtime.UI.Core.Popups;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagment;
using Assets._Project.Develop.Runtime.Utilities.Localization;
using Assets._Project.Develop.Runtime.Utilities.SceneManagment;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.ResultsPopup
{
    public class DefeatPopupPresenter : PopupPresenterBase
    {
        private const string TitleName = "Поражение";
        private const string DecreeText = "Оборона прорвана. Король требует реванша";
        private const string WaveTemplate = "Волна {0}/{1}";

        private readonly DefeatPopupView _popupView;
        private readonly SceneSwitcherService _sceneSwitcherService;
        private readonly ICoroutinesPerformer _coroutinesPerformer;
        private readonly GameplayInputArgs _gameplayInputArgs;
        private readonly StageProviderService _stageProviderService;
        private readonly LocalizationService _localizationService;

        public DefeatPopupPresenter(
            ICoroutinesPerformer coroutinesPerformer,
            RayShooterService rayShooterService,
            DefeatPopupView popupView,
            SceneSwitcherService sceneSwitcherService,
            GameplayInputArgs gameplayInputArgs,
            StageProviderService stageProviderService,
            LocalizationService localizationService)
            : base(coroutinesPerformer, rayShooterService)
        {
            _popupView = popupView;
            _sceneSwitcherService = sceneSwitcherService;
            _coroutinesPerformer = coroutinesPerformer;
            _gameplayInputArgs = gameplayInputArgs;
            _stageProviderService = stageProviderService;
            _localizationService = localizationService;
        }

        protected override PopupViewBase PopupView => _popupView;

        public override void Initialize()
        {
            base.Initialize();

            _popupView.SetTitle(_localizationService.Translate(TitleName));
            _popupView.SetDecree(_localizationService.Translate(DecreeText));
            _popupView.SetWave(_localizationService.FormatTemplate(
                WaveTemplate,
                _stageProviderService.CurrentStageNumber.Value,
                _stageProviderService.StagesCount));

            _popupView.ContinueClicked += OnContinueClicked;
            _popupView.RestartClicked += OnRestartClicked;
        }

        public override void Dispose()
        {
            base.Dispose();

            _popupView.ContinueClicked -= OnContinueClicked;
            _popupView.RestartClicked -= OnRestartClicked;
        }

        protected override void OnPreHide()
        {
            base.OnPreHide();

            _popupView.ContinueClicked -= OnContinueClicked;
            _popupView.RestartClicked -= OnRestartClicked;
        }

        private void OnContinueClicked()
        {
            _coroutinesPerformer.StartPerform(_sceneSwitcherService.ProcessSwitchTo(Scenes.MainMenu, new MainMenuInputArgs(Scenes.Gameplay)));
            OnCloseRequest();
        }

        private void OnRestartClicked()
        {
            _coroutinesPerformer.StartPerform(_sceneSwitcherService.ProcessSwitchTo(Scenes.Gameplay, _gameplayInputArgs.AsRestart()));
            OnCloseRequest();
        }
    }
}
