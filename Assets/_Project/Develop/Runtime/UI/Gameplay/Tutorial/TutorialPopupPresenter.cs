using System;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.Gameplay.Features.PauseFeature;
using Assets._Project.Develop.Runtime.Gameplay.Features.Raycast;
using Assets._Project.Develop.Runtime.UI.Core.Popups;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagment;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // Окно обучения держит бой на паузе, пока открыто (шаг может отпустить паузу на время, SetPaused). Части обучения —
    // слайдер, «Королевский взор», указатель — показываются в нём по очереди, а закрывает окно тот, кто его открыл, —
    // когда цепочка подсказок кончилась. Щелчок по затемнению приходит событием DimmerClicked: что он значит, решает
    // текущий шаг.
    public class TutorialPopupPresenter : PopupPresenterBase
    {
        public event Action DimmerClicked;

        private readonly TutorialPopupView _view;
        private readonly TimeScalePauseService _timeScalePauseService;
        private readonly GameplayPresentersFactory _presentersFactory;

        private TutorialSliderPresenter _sliderPresenter;
        private Action _sliderClosedCallback;

        private bool _isPausing;
        private bool _isHiding;

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
            _view.CameraHintView.gameObject.SetActive(false);
            _view.PointerView.gameObject.SetActive(false);
            _view.ContinueHintView.gameObject.SetActive(false);
            _view.CameraInputPassThrough.enabled = false;

            _view.Dimmer.Clicked += OnDimmerClicked;

            SetPaused(true);
        }

        public override void Dispose()
        {
            base.Dispose();

            _view.Dimmer.Clicked -= OnDimmerClicked;

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

        // Пауза вложенная: окно держит её не больше одной, а закрывающееся окно паузу уже не берёт
        public void SetPaused(bool isPaused)
        {
            if (isPaused == false)
            {
                ReleasePause();
                return;
            }

            if (_isPausing || _isHiding)
                return;

            _isPausing = true;
            _timeScalePauseService.Pause();
        }

        public void ShowCameraHint(TutorialCameraHintInfo info, string continueText, bool isMobile)
        {
            _view.CameraHintView.Show(info.Text, info.MoveCaption, info.ZoomCaption, isMobile);
            _view.ContinueHintView.Show(continueText);
        }

        public void HideCameraHint()
        {
            _view.CameraHintView.Hide();
            _view.ContinueHintView.Hide();
        }

        public void SetCameraInputPassThrough(bool isPassing) => _view.CameraInputPassThrough.enabled = isPassing;

        // Надпись «Нажмите в любом месте» — только у необязательного указателя: continueText не пустой
        public void ShowPointer(ITutorialPointerTarget target, TutorialPointerSides side, string text, string continueText)
        {
            _view.BringToFront();
            _view.PointerView.Show(target, side, text);

            if (string.IsNullOrEmpty(continueText))
                _view.ContinueHintView.Hide();
            else
                _view.ContinueHintView.Show(continueText);
        }

        public void SetPointerText(string text) => _view.PointerView.SetText(text);

        public void HidePointer()
        {
            _view.PointerView.Hide();
            _view.ContinueHintView.Hide();
        }

        public void SetDimmerVisible(bool isVisible) => _view.Dimmer.SetVisible(isVisible);

        public void BringToFront() => _view.BringToFront();

        protected override void OnPreHide()
        {
            base.OnPreHide();

            _isHiding = true;
            ReleasePause();
        }

        private void OnDimmerClicked() => DimmerClicked?.Invoke();

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

        // Снимается один раз, при скрытии или при уничтожении окна — что наступит раньше
        private void ReleasePause()
        {
            if (_isPausing == false)
                return;

            _isPausing = false;
            _timeScalePauseService.Unpause();
        }
    }
}
