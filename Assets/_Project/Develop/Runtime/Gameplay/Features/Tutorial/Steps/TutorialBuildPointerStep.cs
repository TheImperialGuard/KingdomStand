using System;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.Features.Level;
using Assets._Project.Develop.Runtime.Gameplay.Features.Towers;
using Assets._Project.Develop.Runtime.UI.Core.Popups;
using Assets._Project.Develop.Runtime.UI.Gameplay;
using Assets._Project.Develop.Runtime.UI.Gameplay.BuildTowerPopup;
using Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Tutorial.Steps
{
    // Указатель на место под башню — с принуждением: щёлкнуть можно только в вырез. Место — ближайшее к центру экрана.
    // Щелчок открывает окно постройки, и вырез переходит на его кольцо: выбрать можно любую башню. Шаг кончается, когда
    // башня построена.
    public class TutorialBuildPointerStep : ITutorialStep
    {
        private readonly TutorialPointersInfo _info;
        private readonly TowersPlaceholdersService _placeholdersService;
        private readonly BuiltTowersService _builtTowersService;
        private readonly GameplayPopupService _popupService;

        private TutorialPopupPresenter _popup;
        private Action _completed;
        private Entity _placeholder;

        public TutorialBuildPointerStep(
            TutorialPointersInfo info,
            TowersPlaceholdersService placeholdersService,
            BuiltTowersService builtTowersService,
            GameplayPopupService popupService)
        {
            _info = info;
            _placeholdersService = placeholdersService;
            _builtTowersService = builtTowersService;
            _popupService = popupService;
        }

        public string Id => _info.Build.Id;

        public bool IsNeeded() => _builtTowersService.Towers.Count == 0 && _placeholdersService.Placeholders.Count > 0;

        public void Start(TutorialPopupPresenter popup, Action completed)
        {
            _popup = popup;
            _completed = completed;
            _placeholder = FindPlaceholderNearestToScreenCenter();

            _popupService.TowerPopupOpened += OnTowerPopupOpened;
            _popupService.TowerPopupClosed += OnTowerPopupClosed;
            _builtTowersService.Added += OnTowerAdded;

            ShowPlaceholderPointer();
        }

        public void Stop()
        {
            _popupService.TowerPopupOpened -= OnTowerPopupOpened;
            _popupService.TowerPopupClosed -= OnTowerPopupClosed;
            _builtTowersService.Added -= OnTowerAdded;

            _popup = null;
            _completed = null;
            _placeholder = null;
        }

        private void ShowPlaceholderPointer()
        {
            WorldTutorialPointerTarget target = new(_placeholder.BoxCollider, _info.HolePadding);

            _popup.ShowPointer(target, _info.Build.Side, _info.Build.Text, null);
        }

        private void OnTowerPopupOpened(PopupPresenterBase towerPopup, Entity source)
        {
            if (towerPopup is not BuildTowerPopupPresenter buildPopup)
                return;

            RectTutorialPointerTarget target = new(buildPopup.RingArea, _info.HolePadding);

            _popup.ShowPointer(target, _info.Build.Side, _info.Build.Text, null);
        }

        // Окно постройки закрылось без башни — указатель возвращается на место
        private void OnTowerPopupClosed() => ShowPlaceholderPointer();

        private void OnTowerAdded(Entity tower)
        {
            _popup.HidePointer();
            _completed?.Invoke();
        }

        private Entity FindPlaceholderNearestToScreenCenter()
        {
            Camera camera = Camera.main;
            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) / 2f;

            Entity nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Entity placeholder in _placeholdersService.Placeholders)
            {
                Vector2 screenPoint = camera.WorldToScreenPoint(placeholder.Transform.position);
                float distance = (screenPoint - screenCenter).sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearest = placeholder;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
    }
}
