using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.EnemyHints;
using Assets._Project.Develop.Runtime.Gameplay.Features.LevelStages;
using Assets._Project.Develop.Runtime.Infrastructure.DI;
using Assets._Project.Develop.Runtime.Utilities.Timer;
#if UNITY_EDITOR
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
#endif

namespace Assets._Project.Develop.Runtime.Gameplay.Features.EnemyHints
{
    // Когда всплывают подсказки о новых врагах уровня. Отсчёт идёт от фактического начала стейджа таймерами стейджа и
    // спавна, поэтому пауза его останавливает, а досрочный вызов волны начинает от нового стейджа. Смена стейджа
    // запущенные отсчёты не гасит: враг выпущен и всё равно дойдёт до поля. Гасит их конец боя
    public class EnemyHintsService : IInitializable, IDisposable
    {
        public event Action<EnemyHintInfo> HintShown;
        public event Action Stopped;

        private readonly EnemyHintsConfig _config;
        private readonly int _levelNumber;
        private readonly StageProviderService _stageProviderService;
        private readonly StagesCycle _stagesCycle;
        private readonly TimerServiceFactory _timerServiceFactory;

        private readonly Dictionary<TimerService, EnemyHintInfo> _pendingHints = new();
        private readonly List<IDisposable> _disposables = new();

        public EnemyHintsService(
            EnemyHintsConfig config,
            int levelNumber,
            StageProviderService stageProviderService,
            StagesCycle stagesCycle,
            TimerServiceFactory timerServiceFactory)
        {
            _config = config;
            _levelNumber = levelNumber;
            _stageProviderService = stageProviderService;
            _stagesCycle = stagesCycle;
            _timerServiceFactory = timerServiceFactory;
        }

        public void Initialize()
        {
            _disposables.Add(_stageProviderService.CurrentStageNumber.Subscribe(OnStageNumberChanged));

            _stagesCycle.Stopped += OnStagesStopped;
        }

        public void Dispose()
        {
            _stagesCycle.Stopped -= OnStagesStopped;

            foreach (IDisposable disposable in _disposables)
                disposable.Dispose();

            _disposables.Clear();

            StopPendingHints();
        }

        private void OnStageNumberChanged(int oldNumber, int newNumber)
        {
            int campaignStageNumber = ToCampaignStageNumber(newNumber);

            foreach (EnemyHintInfo hint in _config.Hints)
            {
                if (hint.LevelNumber == _levelNumber && hint.StageNumber == campaignStageNumber)
                    StartHintTimer(hint);
            }
        }

        private void OnStagesStopped()
        {
            StopPendingHints();

            Stopped?.Invoke();
        }

        private void OnHintTimerEnded(TimerService timer)
        {
            if (_pendingHints.Remove(timer, out EnemyHintInfo hint) == false)
                return;

            HintShown?.Invoke(hint);
        }

        private void StartHintTimer(EnemyHintInfo hint)
        {
            TimerService timer = _timerServiceFactory.Create(hint.ShowTime);

            _pendingHints.Add(timer, hint);
            _disposables.Add(timer.CooldownEndedWithTimer.Subscribe(OnHintTimerEnded));

            timer.Restart();
        }

        private void StopPendingHints()
        {
            foreach (TimerService timer in _pendingHints.Keys)
                timer.Dispose();

            _pendingHints.Clear();
        }

        // Плейтест с середины уровня играет копию без стейджей до стартового, и номера в бою сдвинуты на их число
        private int ToCampaignStageNumber(int stageNumber)
        {
            #if UNITY_EDITOR
                return stageNumber + PlaytestHooks.RemovedStagesCount;
            #else
                return stageNumber;
            #endif
        }
    }
}
