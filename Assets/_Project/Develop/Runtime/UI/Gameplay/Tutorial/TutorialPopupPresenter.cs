using System;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.Gameplay.Features.PauseFeature;
using Assets._Project.Develop.Runtime.Gameplay.Features.Raycast;
using Assets._Project.Develop.Runtime.UI.Core.Popups;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagment;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // Окно обучения держит бой на паузе, пока открыто. Части обучения показываются в нём по очереди, а закрывает окно
    // тот, кто его открыл, — когда цепочка подсказок кончилась.
    public class TutorialPopupPresenter : PopupPresenterBase
    {
        private readonly TutorialPopupView _view;
        private readonly TimeScalePauseService _timeScalePauseService;
        private readonly GameplayPresentersFactory _presentersFactory;

        private TutorialSliderPresenter _sliderPresenter;
        private Action _sliderClosedCallback;

        private bool _isPausing;

        public TutorialPopupPresenter(
            ICoroutinesPerformer coroutinesPerformer,
            RayShooterService rayShooterService,
            TutorialPopupView view,
            TimeScalePauseService timeScalePauseService,
            GameplayPresentersFactory presentersFactory)
            : base(coroutinesPerformer, rayShooterService)
        {
            _view = view;
            _timeScalePauseService = timeScalePauseService;
            _presentersFactory = presentersFactory;
        }

        protected override PopupViewBase PopupView => _view;

        public override void Initialize()
        {
            base.Initialize();

            _view.SliderView.SetVisible(false);

            _timeScalePauseService.Pause();
            _isPausing = true;
        }

        public override void Dispose()
        {
            base.Dispose();

            DisposeSlider();
            ReleasePause();
        }

        public void ShowSlider(TutorialSliderInfo info, Action closedCallback)
        {
            DisposeSlider();

            _sliderClosedCallback = closedCallback;

            _sliderPresenter = _presentersFactory.CreateTutorialSliderPresenter(_view.SliderView, info);
            _sliderPresenter.Closed += OnSliderClosed;
            _sliderPresenter.Initialize();
        }

        protected override void OnPreHide()
        {
            base.OnPreHide();

            ReleasePause();
        }

        private void OnSliderClosed()
        {
            Action callback = _sliderClosedCallback;

            DisposeSlider();
            _view.SliderView.SetVisible(false);

            callback?.Invoke();
        }

        private void DisposeSlider()
        {
            if (_sliderPresenter == null)
                return;

            _sliderPresenter.Closed -= OnSliderClosed;
            _sliderPresenter.Dispose();
            _sliderPresenter = null;
            _sliderClosedCallback = null;
        }

        // Пауза вложенная: снимается один раз, при скрытии или при уничтожении окна — что наступит раньше
        private void ReleasePause()
        {
            if (_isPausing == false)
                return;

            _isPausing = false;
            _timeScalePauseService.Unpause();
        }
    }
}
