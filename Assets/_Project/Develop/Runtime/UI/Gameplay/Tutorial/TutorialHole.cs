using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // Вырез в затемнении обучения — в локальных координатах затемнения. У круга размер — диаметр по обеим осям.
    public readonly struct TutorialHole
    {
        public TutorialHole(TutorialHoleShapes shape, Vector2 center, Vector2 size)
        {
            Shape = shape;
            Center = center;
            Size = size;
        }

        public TutorialHoleShapes Shape { get; }

        public Vector2 Center { get; }

        public Vector2 Size { get; }

        public Vector2 Extents => Size / 2f;

        public bool Contains(Vector2 point)
        {
            Vector2 offset = point - Center;

            if (Shape == TutorialHoleShapes.Circle)
                return offset.sqrMagnitude <= Extents.x * Extents.x;

            return Mathf.Abs(offset.x) <= Extents.x && Mathf.Abs(offset.y) <= Extents.y;
        }

        public bool IsSameAs(TutorialHole other) => Shape == other.Shape && Center == other.Center && Size == other.Size;
    }
}
