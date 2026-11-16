using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.InputFeature
{
    // Что из интерфейса лежит под точкой экрана. Свой RaycastAll вместо IsPointerOverGameObject: порядок Update между
    // EventSystem и бутстрапом не задан, и на кадре касания указатель модуля может быть ещё не заведён или уже
    // освобождён. Верхний объект с включённой меткой CameraInputPassThrough управлять камерой не мешает.
    public class UIPointerProbe
    {
        private readonly List<RaycastResult> _results = new();

        private PointerEventData _eventData;

        public bool IsOverUI { get; private set; }

        public bool IsBlockingCamera { get; private set; }

        public void Probe(Vector2 screenPosition)
        {
            IsOverUI = false;
            IsBlockingCamera = false;

            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null)
                return;

            if (_eventData == null)
                _eventData = new PointerEventData(eventSystem);

            _eventData.position = screenPosition;

            _results.Clear();
            eventSystem.RaycastAll(_eventData, _results);

            IsOverUI = _results.Count > 0;
            IsBlockingCamera = IsOverUI && IsPassThrough(_results[0].gameObject) == false;
        }

        private bool IsPassThrough(GameObject target)
            => target.TryGetComponent(out CameraInputPassThrough passThrough) && passThrough.enabled;
    }
}
