using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // Затемнение обучения во весь экран, с вырезом над объектом интереса или без него. Меш строится сам: кольцо между
    // контуром выреза и краем экрана, у контура — мягкий край. Внутри выреза щелчки проходят насквозь — в кнопку под ним
    // или лучом в мир; щелчок по самому затемнению приходит событием Clicked.
    public class TutorialDimmerView : Image, IPointerClickHandler
    {
        private const int CircleSegments = 64;
        private const int RectangleCorners = 4;
        private const float EdgeSoftness = 6f;
        private const float FadeDuration = 0.25f;

        public event Action Clicked;

        private TutorialHole _hole;
        private bool _hasHole;
        private float _visibleAlpha;

        private Tween _fade;

        protected override void Awake()
        {
            base.Awake();

            _visibleAlpha = color.a;
        }

        protected override void OnDestroy()
        {
            _fade?.Kill();

            base.OnDestroy();
        }

        public void SetHole(TutorialHole hole)
        {
            if (_hasHole && hole.IsSameAs(_hole))
                return;

            _hole = hole;
            _hasHole = true;

            SetVerticesDirty();
        }

        public void ClearHole()
        {
            if (_hasHole == false)
                return;

            _hasHole = false;

            SetVerticesDirty();
        }

        // Погасшее затемнение щелчков не забирает: между подсказками интерфейс и мир работают как обычно
        public void SetVisible(bool isVisible)
        {
            _fade?.Kill();

            raycastTarget = isVisible;
            _fade = this.DOFade(isVisible ? _visibleAlpha : 0f, FadeDuration).SetUpdate(true);
        }

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();

        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (_hasHole == false)
                return base.IsRaycastLocationValid(screenPoint, eventCamera);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPoint, eventCamera, out Vector2 localPoint) == false)
                return true;

            return _hole.Contains(localPoint) == false;
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            if (_hasHole == false)
            {
                base.OnPopulateMesh(toFill);
                return;
            }

            toFill.Clear();

            Color32 solid = color;
            Color32 clear = new Color32(solid.r, solid.g, solid.b, 0);

            Rect holeBounds = _hole.Shape == TutorialHoleShapes.Circle
                ? AddCircleEdge(toFill, solid, clear)
                : AddRectangleEdge(toFill, solid, clear);

            AddBandsAround(toFill, rectTransform.rect, holeBounds, solid);
        }

        // Три контура на одних и тех же лучах из центра: прозрачный круг, круг мягкого края и квадрат вокруг него. Углы
        // квадрата попадают на лучи (число отрезков кратно восьми), поэтому ломаная третьего контура и есть квадрат.
        private Rect AddCircleEdge(VertexHelper toFill, Color32 solid, Color32 clear)
        {
            Vector2 center = _hole.Center;
            float innerRadius = _hole.Extents.x;
            float outerRadius = innerRadius + EdgeSoftness;
            int firstVertex = toFill.currentVertCount;

            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / CircleSegments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float squareDistance = outerRadius / Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));

                toFill.AddVert(center + direction * innerRadius, clear, Vector4.zero);
                toFill.AddVert(center + direction * outerRadius, solid, Vector4.zero);
                toFill.AddVert(center + direction * squareDistance, solid, Vector4.zero);
            }

            for (int i = 0; i < CircleSegments; i++)
            {
                int current = firstVertex + i * 3;
                int next = firstVertex + (i + 1) % CircleSegments * 3;

                AddQuad(toFill, current, next, next + 1, current + 1);
                AddQuad(toFill, current + 1, next + 1, next + 2, current + 2);
            }

            return new Rect(center.x - outerRadius, center.y - outerRadius, outerRadius * 2f, outerRadius * 2f);
        }

        private Rect AddRectangleEdge(VertexHelper toFill, Color32 solid, Color32 clear)
        {
            Rect inner = new Rect(_hole.Center - _hole.Extents, _hole.Size);
            Rect outer = new Rect(inner.x - EdgeSoftness, inner.y - EdgeSoftness,
                inner.width + EdgeSoftness * 2f, inner.height + EdgeSoftness * 2f);

            int firstVertex = toFill.currentVertCount;

            AddCorners(toFill, inner, clear);
            AddCorners(toFill, outer, solid);

            for (int i = 0; i < RectangleCorners; i++)
            {
                int current = firstVertex + i;
                int next = firstVertex + (i + 1) % RectangleCorners;

                AddQuad(toFill, current, next, next + RectangleCorners, current + RectangleCorners);
            }

            return outer;
        }

        // Затемнение вокруг выреза — полосы сверху, снизу, слева и справа, обрезанные краем экрана
        private void AddBandsAround(VertexHelper toFill, Rect area, Rect hole, Color32 solid)
        {
            float holeBottom = Mathf.Clamp(hole.yMin, area.yMin, area.yMax);
            float holeTop = Mathf.Clamp(hole.yMax, area.yMin, area.yMax);
            float holeLeft = Mathf.Clamp(hole.xMin, area.xMin, area.xMax);
            float holeRight = Mathf.Clamp(hole.xMax, area.xMin, area.xMax);

            AddBand(toFill, Rect.MinMaxRect(area.xMin, holeTop, area.xMax, area.yMax), solid);
            AddBand(toFill, Rect.MinMaxRect(area.xMin, area.yMin, area.xMax, holeBottom), solid);
            AddBand(toFill, Rect.MinMaxRect(area.xMin, holeBottom, holeLeft, holeTop), solid);
            AddBand(toFill, Rect.MinMaxRect(holeRight, holeBottom, area.xMax, holeTop), solid);
        }

        private void AddBand(VertexHelper toFill, Rect band, Color32 solid)
        {
            if (band.width <= 0f || band.height <= 0f)
                return;

            int firstVertex = toFill.currentVertCount;

            AddCorners(toFill, band, solid);
            AddQuad(toFill, firstVertex, firstVertex + 1, firstVertex + 2, firstVertex + 3);
        }

        private void AddCorners(VertexHelper toFill, Rect rect, Color32 vertexColor)
        {
            toFill.AddVert(new Vector2(rect.xMin, rect.yMin), vertexColor, Vector4.zero);
            toFill.AddVert(new Vector2(rect.xMin, rect.yMax), vertexColor, Vector4.zero);
            toFill.AddVert(new Vector2(rect.xMax, rect.yMax), vertexColor, Vector4.zero);
            toFill.AddVert(new Vector2(rect.xMax, rect.yMin), vertexColor, Vector4.zero);
        }

        private void AddQuad(VertexHelper toFill, int first, int second, int third, int fourth)
        {
            toFill.AddTriangle(first, second, third);
            toFill.AddTriangle(first, third, fourth);
        }
    }
}
