using Assets._Project.Develop.Runtime.Gameplay.Features.LevelStages;
using Assets._Project.Develop.Runtime.UI.CommonViews;
using Assets._Project.Develop.Runtime.UI.Core.Presenters;
using Assets._Project.Develop.Runtime.Utilities.Localization;
using System;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.StagesStatus
{
    // «Волна  N/M» в HUD; у бесконечного режима числа волн нет — «Волна  N».
    public class StagesStatusPresenter : IPresenter
    {
        private const string FiniteStatusTemplate = "Волна  {0}/{1}";
        private const string EndlessStatusTemplate = "Волна  {0}";

        private readonly IconTextView _view;
        private readonly StageProviderService _stagesProvider;
        private readonly LocalizationService _localizationService;

        private IDisposable _currentStageChangedDisposable;

        public StagesStatusPresenter(
            IconTextView view,
            StageProviderService stagesProviderService,
            LocalizationService localizationService)
        {
            _view = view;
            _stagesProvider = stagesProviderService;
            _localizationService = localizationService;
        }

        public void Initialize()
        {
            _currentStageChangedDisposable = _stagesProvider.CurrentStageNumber.Subscribe(OnCurrentStageChanged);

            UpdateView();
        }

        public void Dispose()
        {
            _currentStageChangedDisposable.Dispose();
        }

        private void OnCurrentStageChanged(int arg1, int arg2) => UpdateView();

        private void UpdateView()
        {
            int currentStage = _stagesProvider.CurrentStageNumber.Value;

            string stagesStatus = _stagesProvider.HasFiniteStages
                ? _localizationService.FormatTemplate(FiniteStatusTemplate, currentStage, _stagesProvider.StagesCount)
                : _localizationService.FormatTemplate(EndlessStatusTemplate, currentStage);

            _view.SetText(stagesStatus);
        }
    }
}
