using System;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.UI.Core.Popups;
using Assets._Project.Develop.Runtime.UI.Gameplay;
using Assets._Project.Develop.Runtime.UI.Gameplay.SkipStagePopup;
using Assets._Project.Develop.Runtime.UI.Gameplay.StartStagesPopup;
using Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Tutorial.Steps
{
    // Указатель на медальон вызова волны — старта боя или досрочного вызова. До первого нажатия — принуждение: щёлкнуть
    // можно только медальон. Первое нажатие раскрывает подсказку окна, и текст указателя меняется; дальше второе нажатие
    // по медальону вызывает волну, а щелчок по затемнению отменяет нажатие, как щелчок мимо, — указатель закрывается.
    public class TutorialStageCallPointerStep : ITutorialStep
    {
        private readonly TutorialPointerInfo _info;
        private readonly float _holePadding;
        private readonly GameplayPopupService _popupService;
        private readonly bool _isSkipPopup;

        private TutorialPopupPresenter _popup;
        private Action _completed;
        private IStageCallPopup _stageCallPopup;
        private bool _isArmed;

        public TutorialStageCallPointerStep(
            TutorialPointerInfo info,
            float holePadding,
            GameplayPopupService popupService,
            bool isSkipPopup)
        {
            _info = info;
            _holePadding = holePadding;
            _popupService = popupService;
            _isSkipPopup = isSkipPopup;
        }

        public string Id => _info.Id;

        // Окна нет — волну уже вызвали сами
        public bool IsNeeded()
        {
            IStageCallPopup stageCallPopup = _popupService.StageCallPopup;

            return stageCallPopup != null && (stageCallPopup is SkipStagePopupPresenter) == _isSkipPopup;
        }

        public void Start(TutorialPopupPresenter popup, Action completed)
        {
            _popup = popup;
            _completed = completed;
            _stageCallPopup = _popupService.StageCallPopup;
            _isArmed = false;

            _popup.DimmerClicked += OnDimmerClicked;
            _stageCallPopup.InfoShown += OnInfoShown;
            _stageCallPopup.CloseRequest += OnStageCallPopupCloseRequest;

            RectTutorialPointerTarget target = new(_stageCallPopup.ButtonRect, _holePadding);

            _popup.ShowPointer(target, _info.Side, _info.Text, null);
        }

        public void Stop()
        {
            if (_popup != null)
                _popup.DimmerClicked -= OnDimmerClicked;

            if (_stageCallPopup != null)
            {
                _stageCallPopup.InfoShown -= OnInfoShown;
                _stageCallPopup.CloseRequest -= OnStageCallPopupCloseRequest;
            }

            _popup = null;
            _completed = null;
            _stageCallPopup = null;
        }

        private void OnInfoShown()
        {
            _isArmed = true;
            _popup.SetPointerText(_info.SecondText);
        }

        private void OnDimmerClicked()
        {
            if (_isArmed == false)
                return;

            _stageCallPopup.CancelInfo();
            Finish();
        }

        private void OnStageCallPopupCloseRequest(PopupPresenterBase stageCallPopup, bool withCallBack) => Finish();

        private void Finish()
        {
            _popup.HidePointer();
            _completed?.Invoke();
        }
    }
}
