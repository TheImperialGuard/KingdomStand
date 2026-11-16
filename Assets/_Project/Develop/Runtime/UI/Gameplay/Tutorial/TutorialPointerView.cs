using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // Указатель обучения (spec.md → «Указатель»): вырез в затемнении над целью, стрелка с заданной стороны смотрит на
    // него и покачивается к нему, за стрелкой — текст, прижатый к safe area. Цель могут двигать камера и анимации,
    // поэтому вырез и разметка пересчитываются в LateUpdate — только отображение. Вью растянута, как затемнение.
    public class TutorialPointerView : MonoBehaviour
    {
        private const float ArrowGap = 12f;
        private const float TextGap = 18f;
        private const float TextMaxWidth = 450f;
        private const float BobDistance = 12f;
        private const float BobDuration = 0.8f;
        private const float PointingDownAngle = 0f;
        private const float PointingUpAngle = 180f;
        private const float PointingRightAngle = 90f;
        private const float PointingLeftAngle = -90f;

        [SerializeField] private TutorialDimmerView _dimmer;

        // Корень стрелки ставится у выреза и поворачивается к нему, сама стрелка качается внутри по своей оси
        [SerializeField] private RectTransform _arrowRoot;
        [SerializeField] private RectTransform _arrow;

        [SerializeField] private RectTransform _textRect;
        [SerializeField] private TMP_Text _text;

        private RectTransform _area;
        private ITutorialPointerTarget _target;
        private TutorialPointerSides _side;

        private Tween _bob;

        private void Awake()
        {
            _area = (RectTransform)transform;
        }

        private void LateUpdate() => Refresh();

        private void OnDestroy() => _bob?.Kill();

        public void Show(ITutorialPointerTarget target, TutorialPointerSides side, string text)
        {
            _target = target;
            _side = side;

            gameObject.SetActive(true);
            SetText(text);
            StartBob();
            Refresh();
        }

        public void SetText(string text)
        {
            _text.text = text;

            float width = Mathf.Min(_text.GetPreferredValues(text, TextMaxWidth, 0f).x, TextMaxWidth);
            float height = _text.GetPreferredValues(text, width, 0f).y;

            _textRect.sizeDelta = new Vector2(width, height);
        }

        public void Hide()
        {
            _target = null;
            _bob?.Kill();
            _dimmer.ClearHole();

            gameObject.SetActive(false);
        }

        private void StartBob()
        {
            _bob?.Kill();
            _arrow.localPosition = Vector3.zero;

            // Спрайт стрелки смотрит вниз: к вырезу — по своей оси −Y
            _bob = _arrow
                .DOLocalMoveY(-BobDistance, BobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void Refresh()
        {
            if (_target == null)
                return;

            bool hasHole = _target.TryGetHole(_dimmer.rectTransform, out TutorialHole hole);

            _arrowRoot.gameObject.SetActive(hasHole);
            _textRect.gameObject.SetActive(hasHole);

            if (hasHole == false)
            {
                _dimmer.ClearHole();
                return;
            }

            _dimmer.SetHole(hole);
            Place(hole);
        }

        private void Place(TutorialHole hole)
        {
            Vector2 direction = GetDirection(_side);
            float holeReach = Mathf.Abs(direction.x) * hole.Extents.x + Mathf.Abs(direction.y) * hole.Extents.y;
            float arrowHalfLength = _arrow.rect.height / 2f;

            // В покое стрелка дальше всего от выреза: качнувшись, она подходит к нему на ArrowGap
            Vector2 arrowCenter = hole.Center + direction * (holeReach + ArrowGap + BobDistance + arrowHalfLength);
            Vector2 textPoint = arrowCenter + direction * (arrowHalfLength + TextGap);

            _arrowRoot.localPosition = ToLocal(arrowCenter);
            _arrowRoot.localRotation = Quaternion.Euler(0f, 0f, GetArrowAngle(_side));

            _textRect.pivot = GetTextPivot(_side);
            _textRect.localPosition = ToLocal(textPoint);

            KeepTextInSafeArea();
        }

        private Vector3 ToLocal(Vector2 dimmerPoint)
            => _area.InverseTransformPoint(_dimmer.rectTransform.TransformPoint(dimmerPoint));

        private void KeepTextInSafeArea()
        {
            Rect safeArea = GetSafeArea();
            Vector2 size = _textRect.rect.size;
            Vector2 min = (Vector2)_textRect.localPosition - Vector2.Scale(size, _textRect.pivot);
            Vector2 max = min + size;

            Vector2 shift = Vector2.zero;

            shift.x = Mathf.Max(0f, safeArea.xMin - min.x) - Mathf.Max(0f, max.x - safeArea.xMax);
            shift.y = Mathf.Max(0f, safeArea.yMin - min.y) - Mathf.Max(0f, max.y - safeArea.yMax);

            _textRect.localPosition += (Vector3)shift;
        }

        // Канвас боя — Overlay: экранные точки safe area переводятся в координаты вью без камеры
        private Rect GetSafeArea()
        {
            Rect safeArea = Screen.safeArea;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, safeArea.min, null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, safeArea.max, null, out Vector2 max);

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private Vector2 GetDirection(TutorialPointerSides side)
        {
            return side switch
            {
                TutorialPointerSides.Above => Vector2.up,
                TutorialPointerSides.Below => Vector2.down,
                TutorialPointerSides.Left => Vector2.left,
                TutorialPointerSides.Right => Vector2.right,
                _ => Vector2.up,
            };
        }

        private float GetArrowAngle(TutorialPointerSides side)
        {
            return side switch
            {
                TutorialPointerSides.Above => PointingDownAngle,
                TutorialPointerSides.Below => PointingUpAngle,
                TutorialPointerSides.Left => PointingRightAngle,
                TutorialPointerSides.Right => PointingLeftAngle,
                _ => PointingDownAngle,
            };
        }

        // Текст продолжает стрелку: опорная точка — у его края, обращённого к стрелке
        private Vector2 GetTextPivot(TutorialPointerSides side)
        {
            return side switch
            {
                TutorialPointerSides.Above => new Vector2(0.5f, 0f),
                TutorialPointerSides.Below => new Vector2(0.5f, 1f),
                TutorialPointerSides.Left => new Vector2(1f, 0.5f),
                TutorialPointerSides.Right => new Vector2(0f, 0.5f),
                _ => new Vector2(0.5f, 0f),
            };
        }
    }
}
