using System;
using System.Collections;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.Features.Towers;
using Assets._Project.Develop.Runtime.UI.Core.Popups;
using Assets._Project.Develop.Runtime.UI.Gameplay;
using Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.Utilities.CoroutinesManagment;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Tutorial.Steps
{
    // Указатель на построенную башню — без принуждения: щелчок по затемнению закрывает его. Башня только что построена,
    // поэтому пауза встаёт не сразу, а когда башня доиграет появление. Щелчок по башне в вырезе открывает её окно как
    // обычно; тогда затемнение гаснет, пауза снимается, и шаг ждёт, пока окно башни закроют. Щелчок по затемнению до
    // конца появления сразу убирает указатель, а паузу и следующий шаг откладывает до конца появления: иначе башня
    // застыла бы посреди анимации.
    public class TutorialTowerPointerStep : ITutorialStep
    {
        private readonly TutorialPointersInfo _info;
        private readonly BuiltTowersService _builtTowersService;
        private readonly GameplayPopupService _popupService;
        private readonly ICoroutinesPerformer _coroutinesPerformer;

        private TutorialPopupPresenter _popup;
        private Action _completed;
        private Coroutine _pauseDelay;
        private bool _isWaitingForTowerPopup;
        private bool _isFinishing;

        public TutorialTowerPointerStep(
            TutorialPointersInfo info,
            BuiltTowersService builtTowersService,
            GameplayPopupService popupService,
            ICoroutinesPerformer coroutinesPerformer)
        {
            _info = info;
            _builtTowersService = builtTowersService;
            _popupService = popupService;
            _coroutinesPerformer = coroutinesPerformer;
        }

        public string Id => _info.Tower.Id;

        public bool IsNeeded() => _builtTowersService.Towers.Count > 0;

        public void Start(TutorialPopupPresenter popup, Action completed)
        {
            _popup = popup;
            _completed = completed;
            _isWaitingForTowerPopup = false;
            _isFinishing = false;

            _popup.DimmerClicked += OnDimmerClicked;
            _popupService.TowerPopupOpened += OnTowerPopupOpened;
            _popupService.TowerPopupClosed += OnTowerPopupClosed;

            Entity tower = _builtTowersService.Towers[0];
            WorldTutorialPointerTarget target = new(tower.BoxCollider, _info.HolePadding);

            _popup.ShowPointer(target, _info.Tower.Side, _info.Tower.Text, _info.ContinueText);

            _popup.SetPaused(false);
            _pauseDelay = _coroutinesPerformer.StartPerform(PauseAfterTowerAppeared());
        }

        public void Stop()
        {
            if (_popup != null)
                _popup.DimmerClicked -= OnDimmerClicked;

            _popupService.TowerPopupOpened -= OnTowerPopupOpened;
            _popupService.TowerPopupClosed -= OnTowerPopupClosed;

            StopPauseDelay();

            _popup = null;
            _completed = null;
        }

        private IEnumerator PauseAfterTowerAppeared()
        {
            // Игровое время, а не реальное: пружина появления идёт по нему же. Долгие первые кадры после постройки
            // (Time.maximumDeltaTime) двигают его медленнее реального, и реальная задержка кончалась посреди пружины.
            yield return new WaitForSeconds(_info.TowerPauseDelay);

            _pauseDelay = null;
            _popup.SetPaused(true);

            if (_isFinishing)
                _completed?.Invoke();
        }

        private void OnDimmerClicked()
        {
            if (_isFinishing == false)
                Finish();
        }

        private void OnTowerPopupOpened(PopupPresenterBase towerPopup, Entity source)
        {
            if (_isWaitingForTowerPopup || _isFinishing)
                return;

            _isWaitingForTowerPopup = true;

            StopPauseDelay();

            _popup.HidePointer();
            _popup.SetDimmerVisible(false);
            _popup.SetPaused(false);
        }

        // Закрытие окна постройки, из которого шаг начался, сюда тоже приходит — оно не в счёт
        private void OnTowerPopupClosed()
        {
            if (_isWaitingForTowerPopup == false)
                return;

            _popup.SetDimmerVisible(true);
            _popup.SetPaused(true);

            _completed?.Invoke();
        }

        // Следующий указатель снова держит бой на паузе. Пока башня доигрывает появление, пауза и следующий шаг ждут
        // конца задержки — их выставит PauseAfterTowerAppeared.
        private void Finish()
        {
            _isFinishing = true;
            _popup.HidePointer();

            if (_pauseDelay != null)
                return;

            _popup.SetPaused(true);
            _completed?.Invoke();
        }

        private void StopPauseDelay()
        {
            if (_pauseDelay == null)
                return;

            _coroutinesPerformer.StopPerform(_pauseDelay);
            _pauseDelay = null;
        }
    }
}
