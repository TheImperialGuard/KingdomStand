using Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode;
using Assets._Project.Develop.Runtime.Gameplay.Features.Raycast;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Meta.Infrastructure;
using Assets._Project.Develop.Runtime.UI.Core.Popups;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagment;
using Assets._Project.Develop.Runtime.Utilities.Localization;
using Assets._Project.Develop.Runtime.Utilities.SceneManagment;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.EndlessResultPopup
{
    // Итог забега бесконечного режима вместо окна поражения: достигнутая волна и рекорд или штамп «Новый рекорд!». Забег
    // к этому времени уже засчитан.
    public class EndlessResultPopupPresenter : PopupPresenterBase
    {
        private const string TitleName = "Раунд окончен";
        private const string DecreeText = "Вы внесли своё имя в летопись";
        private const string WaveTemplate = "Волна {0}";
        private const string RecordTemplate = "Рекорд: волна {0}";
        private const string NewRecordText = "Новый рекорд!";

        private readonly EndlessResultPopupView _popupView;
        private readonly SceneSwitcherService _sceneSwitcherService;
        private readonly ICoroutinesPerformer _coroutinesPerformer;
        private readonly GameplayInputArgs _gameplayInputArgs;
        private readonly EndlessRunResult _runResult;
        private readonly LocalizationService _localizationService;

        public EndlessResultPopupPresenter(
            ICoroutinesPerformer coroutinesPerformer,
            RayShooterService rayShooterService,
            EndlessResultPopupView popupView,
            SceneSwitcherService sceneSwitcherService,
            GameplayInputArgs gameplayInputArgs,
            EndlessRunResult runResult,
            LocalizationService localizationService)
            : base(coroutinesPerformer, rayShooterService)
        {
            _popupView = popupView;
            _sceneSwitcherService = sceneSwitcherService;
            _coroutinesPerformer = coroutinesPerformer;
            _gameplayInputArgs = gameplayInputArgs;
            _runResult = runResult;
            _localizationService = localizationService;
        }

        protected override PopupViewBase PopupView => _popupView;

        public override void Initialize()
        {
            base.Initialize();

            _popupView.SetTitle(_localizationService.Translate(TitleName));
            _popupView.SetDecree(_localizationService.Translate(DecreeText));
            _popupView.SetWave(_localizationService.FormatTemplate(WaveTemplate, _runResult.ReachedWave));

            if (_runResult.IsNewRecord)
                _popupView.SetNewRecord(_localizationService.Translate(NewRecordText));
            else
                _popupView.SetRecord(_localizationService.FormatTemplate(RecordTemplate, _runResult.BestWave));

            _popupView.MenuClicked += OnMenuClicked;
            _popupView.RestartClicked += OnRestartClicked;
        }

        public override void Dispose()
        {
            base.Dispose();

            UnsubscribeFromView();
        }

        protected override void OnPreHide()
        {
            base.OnPreHide();

            UnsubscribeFromView();
        }

        private void UnsubscribeFromView()
        {
            _popupView.MenuClicked -= OnMenuClicked;
            _popupView.RestartClicked -= OnRestartClicked;
        }

        private void OnMenuClicked()
        {
            _coroutinesPerformer.StartPerform(
                _sceneSwitcherService.ProcessSwitchTo(Scenes.MainMenu, MainMenuInputArgs.FromGameplay(_gameplayInputArgs)));

            OnCloseRequest();
        }

        private void OnRestartClicked()
        {
            _coroutinesPerformer.StartPerform(_sceneSwitcherService.ProcessSwitchTo(Scenes.Gameplay, _gameplayInputArgs.AsRestart()));

            OnCloseRequest();
        }
    }
}
