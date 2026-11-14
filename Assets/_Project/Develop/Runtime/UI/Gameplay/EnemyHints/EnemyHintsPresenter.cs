using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.EnemyHints;
using Assets._Project.Develop.Runtime.Gameplay.Features.EnemyHints;
using Assets._Project.Develop.Runtime.UI.Core.Presenters;
using Assets._Project.Develop.Runtime.UI.Core.Views;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.EnemyHints
{
    // Медальоны подсказок о новых врагах: всплывают по сервису, нажатый открывает книгу на своём враге и исчезает,
    // в конце боя исчезают все
    public class EnemyHintsPresenter : IPresenter
    {
        private readonly EnemyHintsView _view;
        private readonly EnemyHintsService _enemyHintsService;
        private readonly ViewsFactory _viewsFactory;
        private readonly GameplayPopupService _gameplayPopupService;

        private readonly Dictionary<EnemyHintView, EnemyHintInfo> _hints = new();

        public EnemyHintsPresenter(
            EnemyHintsView view,
            EnemyHintsService enemyHintsService,
            ViewsFactory viewsFactory,
            GameplayPopupService gameplayPopupService)
        {
            _view = view;
            _enemyHintsService = enemyHintsService;
            _viewsFactory = viewsFactory;
            _gameplayPopupService = gameplayPopupService;
        }

        public void Initialize()
        {
            _enemyHintsService.HintShown += OnHintShown;
            _enemyHintsService.Stopped += OnHintsStopped;
        }

        public void Dispose()
        {
            _enemyHintsService.HintShown -= OnHintShown;
            _enemyHintsService.Stopped -= OnHintsStopped;

            foreach (EnemyHintView hintView in _hints.Keys)
            {
                hintView.Clicked -= OnHintClicked;
                _viewsFactory.Release(hintView);
            }

            _hints.Clear();
        }

        private void OnHintShown(EnemyHintInfo hint)
        {
            EnemyHintView hintView = _viewsFactory.Create<EnemyHintView>(ViewIDs.EnemyHint, _view.Container);

            hintView.SetPortrait(hint.Portrait);
            hintView.Clicked += OnHintClicked;

            _hints.Add(hintView, hint);
            _view.Add(hintView);
        }

        private void OnHintClicked(EnemyHintView hintView)
        {
            if (_hints.TryGetValue(hintView, out EnemyHintInfo hint) == false)
                return;

            Hide(hintView);

            _gameplayPopupService.OpenEncyclopediaPopup(hint.Enemy);
        }

        private void OnHintsStopped()
        {
            List<EnemyHintView> hintViews = new(_hints.Keys);

            foreach (EnemyHintView hintView in hintViews)
                Hide(hintView);
        }

        private void Hide(EnemyHintView hintView)
        {
            hintView.Clicked -= OnHintClicked;

            _hints.Remove(hintView);
            _view.Remove(hintView);

            hintView.PlayHide(() => _viewsFactory.Release(hintView));
        }
    }
}
