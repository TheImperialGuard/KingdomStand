using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // «Королевский взор» (spec.md → «Королевский взор»): белый текст и две группы анимированных иконок — перемещение и
    // приближение камеры. На ПК — WASD и мышь с колёсиком, на телефоне и планшете — свайп и щипок. Бой под паузой,
    // поэтому анимации идут на неотмасштабированном времени.
    public class TutorialCameraHintView : MonoBehaviour
    {
        private const float HideDuration = 0.25f;
        private const float DesktopStepDuration = 0.2f;
        private const int DesktopStepsPerCycle = 12;
        private const int WheelFramesCount = 3;
        private const float GestureDuration = 1.4f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private TMP_Text _moveCaption;
        [SerializeField] private TMP_Text _zoomCaption;

        [SerializeField] private List<GameObject> _desktopBlocks = new();
        [SerializeField] private List<GameObject> _mobileBlocks = new();

        // Клавиши по порядку нажатия: W, A, S, D
        [SerializeField] private List<Image> _keys = new();
        [SerializeField] private List<Sprite> _keySprites = new();
        [SerializeField] private List<Sprite> _pressedKeySprites = new();

        [SerializeField] private Image _wheel;
        [SerializeField] private List<Sprite> _wheelSprites = new();
        [SerializeField] private Image _scrollArrow;
        [SerializeField] private Sprite _scrollUpSprite;
        [SerializeField] private Sprite _scrollDownSprite;
        [SerializeField] private Image _magnifier;
        [SerializeField] private Sprite _zoomInSprite;
        [SerializeField] private Sprite _zoomOutSprite;

        [SerializeField] private RectTransform _hand;
        [SerializeField] private float _handTravel = 63f;

        // Кончики пальцев щипка: из своего места в сторону, а затем навстречу друг другу
        [SerializeField] private RectTransform _firstTip;
        [SerializeField] private RectTransform _secondTip;
        [SerializeField] private Vector2 _tipsCloseOffset = new Vector2(18f, 18f);
        [SerializeField] private Vector2 _tipsApartOffset = new Vector2(-27f, -27f);

        private readonly List<Tween> _animations = new();

        private Vector2 _handPosition;
        private Vector2 _firstTipPosition;
        private Vector2 _secondTipPosition;

        private void Awake()
        {
            _handPosition = _hand.anchoredPosition;
            _firstTipPosition = _firstTip.anchoredPosition;
            _secondTipPosition = _secondTip.anchoredPosition;
        }

        private void OnDestroy() => KillAnimations();

        public void Show(string text, string moveCaption, string zoomCaption, bool isMobile)
        {
            gameObject.SetActive(true);
            KillAnimations();

            _group.alpha = 1f;
            _text.text = text;
            _moveCaption.text = moveCaption;
            _zoomCaption.text = zoomCaption;

            SetActive(_desktopBlocks, isMobile == false);
            SetActive(_mobileBlocks, isMobile);

            if (isMobile)
                StartGestures();
            else
                StartDesktopCycle();
        }

        public void Hide()
        {
            if (gameObject.activeSelf == false)
                return;

            KillAnimations();

            _animations.Add(_group
                .DOFade(0f, HideDuration)
                .SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false)));
        }

        // Цикл 2,4 с шагами по 0,2: клавиши нажимаются по очереди на три шага, колёсико полцикла катится вверх
        // (лупа «+»), полцикла — вниз (лупа «−»)
        private void StartDesktopCycle()
        {
            Sequence cycle = DOTween.Sequence();

            for (int step = 0; step < DesktopStepsPerCycle; step++)
            {
                int capturedStep = step;

                cycle.InsertCallback(step * DesktopStepDuration, () => ShowDesktopStep(capturedStep));
            }

            cycle.AppendInterval(DesktopStepDuration);

            _animations.Add(cycle.SetLoops(-1).SetUpdate(true));
        }

        private void ShowDesktopStep(int step)
        {
            int stepsPerKey = DesktopStepsPerCycle / _keys.Count;
            int pressedKey = step / stepsPerKey;

            for (int i = 0; i < _keys.Count; i++)
                _keys[i].sprite = i == pressedKey ? _pressedKeySprites[i] : _keySprites[i];

            bool isScrollingUp = step < DesktopStepsPerCycle / 2;
            int frame = step % WheelFramesCount;

            _wheel.sprite = _wheelSprites[isScrollingUp ? frame : WheelFramesCount - 1 - frame];
            _scrollArrow.sprite = isScrollingUp ? _scrollUpSprite : _scrollDownSprite;
            _magnifier.sprite = isScrollingUp ? _zoomInSprite : _zoomOutSprite;
        }

        private void StartGestures()
        {
            _hand.anchoredPosition = _handPosition + Vector2.left * _handTravel;
            _animations.Add(CreateGesture(_hand.DOAnchorPosX(_handPosition.x + _handTravel, GestureDuration)));

            _firstTip.anchoredPosition = _firstTipPosition + _tipsCloseOffset;
            _animations.Add(CreateGesture(_firstTip.DOAnchorPos(_firstTipPosition + _tipsApartOffset, GestureDuration)));

            _secondTip.anchoredPosition = _secondTipPosition - _tipsCloseOffset;
            _animations.Add(CreateGesture(_secondTip.DOAnchorPos(_secondTipPosition - _tipsApartOffset, GestureDuration)));
        }

        private Tween CreateGesture(Tween tween)
            => tween.SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);

        private void SetActive(List<GameObject> blocks, bool isActive)
        {
            foreach (GameObject block in blocks)
                block.SetActive(isActive);
        }

        private void KillAnimations()
        {
            foreach (Tween animation in _animations)
                animation.Kill();

            _animations.Clear();
        }
    }
}
