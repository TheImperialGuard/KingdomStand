using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.EnemyHints;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.Gameplay.Features.EnemyHints;
using Assets._Project.Develop.Runtime.Gameplay.Features.Tutorial.Steps;
using Assets._Project.Develop.Runtime.Infrastructure.DI;
using Assets._Project.Develop.Runtime.Meta.Features.Tutorial;
using Assets._Project.Develop.Runtime.UI.Gameplay;
using Assets._Project.Develop.Runtime.UI.Gameplay.SkipStagePopup;
using Assets._Project.Develop.Runtime.UI.Gameplay.StartStagesPopup;
using Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;
using Assets._Project.Develop.Runtime.Utilities.Wallet;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Tutorial
{
    // Когда в уровне кампании идут шаги обучения. На старте боя — слайдер заметок, а в уровне указателей ещё
    // «Королевский взор», место под башню, башня и старт волны — всё в одном окне, чтобы затемнение не мигало. Посреди
    // боя — слайдер вместе с медальоном подсказки о враге, указатель пропуска при первом окне досрочного вызова и
    // указатель улучшения: после пропуска, при первом поступлении золота, которого хватает на улучшение. Шаги идут
    // очередью, окно обучения открыто, пока очередь не кончилась, и бой на это время стоит. Каждый шаг показывается
    // один раз — отметка ложится в сохранения, когда шаг кончился или оказался не нужен. В бесконечном режиме и в
    // плейтесте из редактора уровней обучения нет.
    public class TutorialService : IInitializable, IDisposable
    {
        private readonly TutorialConfig _config;
        private readonly int _levelNumber;
        private readonly bool _isEnabled;
        private readonly TutorialProgressService _progressService;
        private readonly PlayerDataProvider _playerDataProvider;
        private readonly ICoroutinesPerformer _coroutinesPerformer;
        private readonly GameplayPopupService _popupService;
        private readonly EnemyHintsService _enemyHintsService;
        private readonly WalletService _walletService;
        private readonly TutorialStepsFactory _stepsFactory;

        private readonly Queue<ITutorialStep> _queue = new();

        private ITutorialStep _currentStep;
        private TutorialPopupPresenter _popup;
        private IDisposable _goldDisposable;

        public TutorialService(
            TutorialConfig config,
            int levelNumber,
            bool isEnabled,
            TutorialProgressService progressService,
            PlayerDataProvider playerDataProvider,
            ICoroutinesPerformer coroutinesPerformer,
            GameplayPopupService popupService,
            EnemyHintsService enemyHintsService,
            WalletService walletService,
            TutorialStepsFactory stepsFactory)
        {
            _config = config;
            _levelNumber = levelNumber;
            _isEnabled = isEnabled;
            _progressService = progressService;
            _playerDataProvider = playerDataProvider;
            _coroutinesPerformer = coroutinesPerformer;
            _popupService = popupService;
            _enemyHintsService = enemyHintsService;
            _walletService = walletService;
            _stepsFactory = stepsFactory;
        }

        private TutorialPointersInfo Pointers => _config.Pointers;

        private bool HasPointers => _isEnabled && Pointers.LevelNumber == _levelNumber;

        private bool IsBusy => _currentStep != null || _queue.Count > 0;

        public void Initialize()
        {
            _enemyHintsService.HintShown += OnEnemyHintShown;
            _popupService.StageCallPopupOpened += OnStageCallPopupOpened;
            _goldDisposable = _walletService.GetCurrency(CurrencyTypes.Gold).Subscribe(OnGoldChanged);
        }

        public void Dispose()
        {
            _enemyHintsService.HintShown -= OnEnemyHintShown;
            _popupService.StageCallPopupOpened -= OnStageCallPopupOpened;
            _goldDisposable?.Dispose();

            _currentStep?.Stop();
            _currentStep = null;
            _queue.Clear();
        }

        // Бой запущен, окно старта волны уже открыто — обучение встаёт поверх него
        public void ProcessLevelStart()
        {
            if (_isEnabled == false)
                return;

            EnqueueSliders(TutorialTriggers.LevelStart, null);

            if (HasPointers)
            {
                _queue.Enqueue(_stepsFactory.CreateCameraHintStep());
                _queue.Enqueue(_stepsFactory.CreateBuildPointerStep());
                _queue.Enqueue(_stepsFactory.CreateTowerPointerStep());
                _queue.Enqueue(_stepsFactory.CreateStartWavePointerStep());
            }

            RunNext();
        }

        private void OnEnemyHintShown(EnemyHintInfo hint)
        {
            if (_isEnabled == false)
                return;

            EnqueueSliders(TutorialTriggers.EnemyHint, hint);
            RunNext();
        }

        private void OnStageCallPopupOpened(IStageCallPopup stageCallPopup)
        {
            if (HasPointers == false || stageCallPopup is not SkipStagePopupPresenter)
                return;

            _queue.Enqueue(_stepsFactory.CreateSkipWavePointerStep());
            RunNext();
        }

        // Золото за досрочный вызов приходит, пока окно обучения ещё открыто, — так указатель улучшения не встаёт сразу
        // за указателем пропуска
        private void OnGoldChanged(int oldValue, int newValue)
        {
            if (HasPointers == false || newValue <= oldValue || IsBusy)
                return;

            if (_progressService.IsShown(Pointers.SkipWave.Id) == false || _progressService.IsShown(Pointers.Upgrade.Id))
                return;

            TutorialUpgradePointerStep step = _stepsFactory.CreateUpgradePointerStep();

            if (step.IsNeeded() && step.HasTarget() == false)
                return;

            _queue.Enqueue(step);
            RunNext();
        }

        private void EnqueueSliders(TutorialTriggers trigger, EnemyHintInfo hint)
        {
            foreach (TutorialSliderInfo slider in _config.Sliders)
            {
                if (IsMatch(slider, trigger, hint))
                    _queue.Enqueue(_stepsFactory.CreateSliderStep(slider));
            }
        }

        private bool IsMatch(TutorialSliderInfo slider, TutorialTriggers trigger, EnemyHintInfo hint)
        {
            if (slider.LevelNumber != _levelNumber || slider.Trigger != trigger)
                return false;

            return trigger != TutorialTriggers.EnemyHint || slider.Enemy == hint.Enemy;
        }

        private void RunNext()
        {
            if (_currentStep != null)
                return;

            while (_queue.Count > 0)
            {
                ITutorialStep step = _queue.Dequeue();

                if (_progressService.IsShown(step.Id))
                    continue;

                if (step.IsNeeded() == false)
                {
                    MarkShown(step.Id);
                    continue;
                }

                _popup ??= _popupService.OpenTutorialPopup();
                _currentStep = step;

                step.Start(_popup, () => OnStepCompleted(step));
                return;
            }

            ClosePopup();
        }

        private void OnStepCompleted(ITutorialStep step)
        {
            if (step != _currentStep)
                return;

            step.Stop();
            _currentStep = null;

            MarkShown(step.Id);
            RunNext();
        }

        private void MarkShown(string id)
        {
            _progressService.MarkShown(id);
            _coroutinesPerformer.StartPerform(_playerDataProvider.SaveAsync());
        }

        private void ClosePopup()
        {
            if (_popup == null)
                return;

            _popupService.ClosePopup(_popup);
            _popup = null;
        }
    }
}
