using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Encyclopedia;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.UI.Core.Hotkeys;
using Assets._Project.Develop.Runtime.UI.Core.Presenters;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // Листание слайдера заметок. Несколько страниц: «Назад» на первой приглушена, на последней вместо «Далее» —
    // завершение, «Пропустить» закрывает сразу. Одна страница — только завершение. Клавиши как в книге: ← и → листают,
    // Esc закрывает. Closed приходит один раз, чем бы слайдер ни закрыли.
    public class TutorialSliderPresenter : IPresenter
    {
        public event Action Closed;

        private readonly TutorialSliderView _view;
        private readonly TutorialSliderInfo _info;
        private readonly HotkeysService _hotkeysService;

        private readonly List<IDisposable> _hotkeysDisposables = new();

        private int _pageIndex;
        private bool _isClosed;

        public TutorialSliderPresenter(TutorialSliderView view, TutorialSliderInfo info, HotkeysService hotkeysService)
        {
            _view = view;
            _info = info;
            _hotkeysService = hotkeysService;
        }

        private IReadOnlyList<EncyclopediaNoteConfig> Pages => _info.Pages;

        private bool HasSeveralPages => Pages.Count > 1;

        private bool IsLastPage => _pageIndex == Pages.Count - 1;

        public void Initialize()
        {
            _view.BackClicked += OnBackClicked;
            _view.NextClicked += OnNextClicked;
            _view.SkipClicked += OnCloseRequested;

            _hotkeysDisposables.Add(_hotkeysService.PreviousRequested.Subscribe(OnPreviousRequested));
            _hotkeysDisposables.Add(_hotkeysService.NextRequested.Subscribe(OnNextRequested));
            _hotkeysDisposables.Add(_hotkeysService.CloseRequested.Subscribe(OnCloseRequested));

            _view.SetVisible(true);
            _view.SetSkipButtonVisible(HasSeveralPages);

            ShowPage(0);
        }

        public void Dispose()
        {
            _view.BackClicked -= OnBackClicked;
            _view.NextClicked -= OnNextClicked;
            _view.SkipClicked -= OnCloseRequested;

            foreach (IDisposable disposable in _hotkeysDisposables)
                disposable.Dispose();

            _hotkeysDisposables.Clear();
        }

        private void ShowPage(int index)
        {
            _pageIndex = index;

            EncyclopediaNoteConfig note = Pages[_pageIndex];

            _view.SetNote(note.Picture, note.Title, note.Text);
            _view.SetDots(Pages.Count, _pageIndex);
            _view.SetBackButton(HasSeveralPages, _pageIndex > 0);

            if (IsLastPage)
                _view.ShowFinishButton(_info.FinishText);
            else
                _view.ShowNextButton();

            _view.UpdateLayout();
        }

        private void OnBackClicked()
        {
            if (_pageIndex > 0)
                ShowPage(_pageIndex - 1);
        }

        private void OnNextClicked()
        {
            if (IsLastPage)
                Close();
            else
                ShowPage(_pageIndex + 1);
        }

        // Клавиши на краях слайдера ничего не делают: стрелка не закрывает его на последней странице
        private void OnPreviousRequested() => OnBackClicked();

        private void OnNextRequested()
        {
            if (IsLastPage == false)
                ShowPage(_pageIndex + 1);
        }

        private void OnCloseRequested() => Close();

        private void Close()
        {
            if (_isClosed)
                return;

            _isClosed = true;

            Closed?.Invoke();
        }
    }
}
