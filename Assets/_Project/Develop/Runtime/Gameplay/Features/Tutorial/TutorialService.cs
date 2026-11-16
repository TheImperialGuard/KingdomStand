using System;
using Assets._Project.Develop.Runtime.Configs.Gameplay.EnemyHints;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.Gameplay.Features.EnemyHints;
using Assets._Project.Develop.Runtime.Infrastructure.DI;
using Assets._Project.Develop.Runtime.Meta.Features.Tutorial;
using Assets._Project.Develop.Runtime.UI.Gameplay;
using Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagment;
using Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Tutorial
{
    // Когда в уровне кампании всплывают слайдеры заметок обучения: на старте боя и вместе с медальоном подсказки о
    // враге. Каждый слайдер показывается один раз — отметка ложится в сохранения при его закрытии. Пока окно обучения
    // открыто, бой стоит, поэтому второй слайдер в это время всплыть не может. В бесконечном режиме и в плейтесте из
    // редактора уровней обучения нет
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

        private TutorialPopupPresenter _popup;

        public TutorialService(
            TutorialConfig config,
            int levelNumber,
            bool isEnabled,
            TutorialProgressService progressService,
            PlayerDataProvider playerDataProvider,
            ICoroutinesPerformer coroutinesPerformer,
            GameplayPopupService popupService,
            EnemyHintsService enemyHintsService)
        {
            _config = config;
            _levelNumber = levelNumber;
            _isEnabled = isEnabled;
            _progressService = progressService;
            _playerDataProvider = playerDataProvider;
            _coroutinesPerformer = coroutinesPerformer;
            _popupService = popupService;
            _enemyHintsService = enemyHintsService;
        }

        public void Initialize() => _enemyHintsService.HintShown += OnEnemyHintShown;

        public void Dispose() => _enemyHintsService.HintShown -= OnEnemyHintShown;

        // Бой запущен, окно старта волны уже открыто — слайдер встаёт поверх него
        public void ProcessLevelStart() => TryShowSlider(TutorialTriggers.LevelStart, null);

        private void OnEnemyHintShown(EnemyHintInfo hint) => TryShowSlider(TutorialTriggers.EnemyHint, hint);

        private void TryShowSlider(TutorialTriggers trigger, EnemyHintInfo hint)
        {
            if (_isEnabled == false || _popup != null)
                return;

            foreach (TutorialSliderInfo slider in _config.Sliders)
            {
                if (IsMatch(slider, trigger, hint) && _progressService.IsShown(slider.Id) == false)
                {
                    ShowSlider(slider);
                    return;
                }
            }
        }

        private bool IsMatch(TutorialSliderInfo slider, TutorialTriggers trigger, EnemyHintInfo hint)
        {
            if (slider.LevelNumber != _levelNumber || slider.Trigger != trigger)
                return false;

            return trigger != TutorialTriggers.EnemyHint || slider.Enemy == hint.Enemy;
        }

        private void ShowSlider(TutorialSliderInfo slider)
        {
            _popup = _popupService.OpenTutorialPopup();
            _popup.ShowSlider(slider, () => OnSliderClosed(slider));
        }

        private void OnSliderClosed(TutorialSliderInfo slider)
        {
            _progressService.MarkShown(slider.Id);
            _coroutinesPerformer.StartPerform(_playerDataProvider.SaveAsync());

            _popupService.ClosePopup(_popup);
            _popup = null;
        }
    }
}
