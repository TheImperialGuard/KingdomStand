using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // Цель указателя — объект мира (место под башню, башня): вырез — прямоугольник вокруг проекции его коллайдера с
    // отступом. Коллайдер — та область, куда приходится щелчок, и у каждого вида башни он свой.
    public class WorldTutorialPointerTarget : ITutorialPointerTarget
    {
        private const int BoxCornersCount = 8;

        private readonly Collider _target;
        private readonly float _padding;

        public WorldTutorialPointerTarget(Collider target, float padding)
        {
            _target = target;
            _padding = padding;
        }

        public bool TryGetHole(RectTransform space, out TutorialHole hole)
        {
            hole = default;

            Camera camera = Camera.main;

            // Выключенный коллайдер — у освобождённой сущности: его границы пусты
            if (_target == null || _target.enabled == false || camera == null)
                return false;

            Bounds bounds = _target.bounds;
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;

            for (int i = 0; i < BoxCornersCount; i++)
            {
                Vector3 screenPoint = camera.WorldToScreenPoint(GetCorner(bounds, i));

                if (screenPoint.z < 0f)
                    return false;

                // Канвас боя — Overlay: камера для перевода экранной точки не нужна
                RectTransformUtility.ScreenPointToLocalPointInRectangle(space, screenPoint, null, out Vector2 localPoint);

                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            Vector2 size = max - min + Vector2.one * _padding * 2f;

            hole = new TutorialHole(TutorialHoleShapes.Rectangle, (min + max) / 2f, size);

            return true;
        }

        private Vector3 GetCorner(Bounds bounds, int index)
        {
            float x = (index & 1) == 0 ? bounds.min.x : bounds.max.x;
            float y = (index & 2) == 0 ? bounds.min.y : bounds.max.y;
            float z = (index & 4) == 0 ? bounds.min.z : bounds.max.z;

            return new Vector3(x, y, z);
        }
    }
}
