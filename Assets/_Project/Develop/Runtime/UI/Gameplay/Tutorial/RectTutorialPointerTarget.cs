using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // Цель указателя — круглый элемент интерфейса (медальон, кольцо окна башни): вырез — круг по большей стороне
    // элемента с отступом.
    public class RectTutorialPointerTarget : ITutorialPointerTarget
    {
        private const int CornersCount = 4;

        private readonly RectTransform _target;
        private readonly float _padding;
        private readonly Vector3[] _corners = new Vector3[CornersCount];

        public RectTutorialPointerTarget(RectTransform target, float padding)
        {
            _target = target;
            _padding = padding;
        }

        public bool TryGetHole(RectTransform space, out TutorialHole hole)
        {
            hole = default;

            if (_target == null || _target.gameObject.activeInHierarchy == false)
                return false;

            _target.GetWorldCorners(_corners);

            Vector2 min = space.InverseTransformPoint(_corners[0]);
            Vector2 max = min;

            for (int i = 1; i < CornersCount; i++)
            {
                Vector2 corner = space.InverseTransformPoint(_corners[i]);

                min = Vector2.Min(min, corner);
                max = Vector2.Max(max, corner);
            }

            Vector2 size = max - min;
            float diameter = Mathf.Max(size.x, size.y) + _padding * 2f;

            hole = new TutorialHole(TutorialHoleShapes.Circle, (min + max) / 2f, Vector2.one * diameter);

            return true;
        }
    }
}
