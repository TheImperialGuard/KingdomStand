using System;
using System.Collections;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.Utilities.CameraManagment;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagment;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Tutorial.Steps
{
    // «Королевский взор»: камера под подсказкой работает — боя ещё нет, пауза на это время снята, а затемнение
    // пропускает колёсико и перетаскивание. Подсказка гаснет на первом движении камеры или щелчке по затемнению, а шаг
    // кончается, когда камера постоит StillDelay: игрок успевает её подвигать.
    public class TutorialCameraHintStep : ITutorialStep
    {
        private const float StillDelay = 0.6f;

        private readonly TutorialPointersInfo _info;
        private readonly BoundedOrthoCamera _camera;
        private readonly ICoroutinesPerformer _coroutinesPerformer;
        private readonly bool _isMobile;

        private TutorialPopupPresenter _popup;
        private Action _completed;
        private IDisposable _cameraMovedDisposable;
        private Coroutine _stillWaiting;
        private bool _isDismissed;

        public TutorialCameraHintStep(
            TutorialPointersInfo info,
            BoundedOrthoCamera camera,
            ICoroutinesPerformer coroutinesPerformer,
            bool isMobile)
        {
            _info = info;
            _camera = camera;
            _coroutinesPerformer = coroutinesPerformer;
            _isMobile = isMobile;
        }

        public string Id => _info.CameraHint.Id;

        public bool IsNeeded() => true;

        public void Start(TutorialPopupPresenter popup, Action completed)
        {
            _popup = popup;
            _completed = completed;
            _isDismissed = false;

            _popup.SetPaused(false);
            _popup.SetCameraInputPassThrough(true);
            _popup.ShowCameraHint(_info.CameraHint, _info.ContinueText, _isMobile);

            _popup.DimmerClicked += OnActionPerformed;
            _cameraMovedDisposable = _camera.Moved.Subscribe(OnActionPerformed);
        }

        public void Stop()
        {
            if (_popup != null)
                _popup.DimmerClicked -= OnActionPerformed;

            _cameraMovedDisposable?.Dispose();
            _cameraMovedDisposable = null;

            StopWaiting();

            _popup = null;
            _completed = null;
        }

        private void OnActionPerformed()
        {
            if (_isDismissed == false)
            {
                _isDismissed = true;
                _popup.HideCameraHint();
            }

            StopWaiting();
            _stillWaiting = _coroutinesPerformer.StartPerform(WaitForStillCamera());
        }

        private IEnumerator WaitForStillCamera()
        {
            yield return new WaitForSecondsRealtime(StillDelay);

            _stillWaiting = null;

            _popup.SetCameraInputPassThrough(false);
            _popup.SetPaused(true);

            _completed?.Invoke();
        }

        private void StopWaiting()
        {
            if (_stillWaiting == null)
                return;

            _coroutinesPerformer.StopPerform(_stillWaiting);
            _stillWaiting = null;
        }
    }
}
