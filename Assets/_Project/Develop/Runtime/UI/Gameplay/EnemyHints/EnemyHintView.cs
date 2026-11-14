using System;
using Assets._Project.Develop.Runtime.UI.Core.Views;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.EnemyHints
{
    // Медальон подсказки о новом враге: портрет в железной оправе и качающийся «!». Анимации идут на игровом времени:
    // медальон — часть боя, в паузе и под открытой книгой он стоит, а сжатие нажатого доигрывается после закрытия книги
    public class EnemyHintView : MonoBehaviour, IView
    {
        private const float AppearDuration = 0.3f;
        private const float MoveDuration = 0.25f;
        private const float HideDuration = 0.2f;

        public event Action<EnemyHintView> Clicked;

        [SerializeField] private RectTransform _rect;
        [SerializeField] private Button _button;
        [SerializeField] private Image _portrait;

        private Tween _moveTween;
        private Tween _hideTween;

        public void SetPortrait(Sprite portrait) => _portrait.sprite = portrait;

        // Выезд из-за левого края экрана на своё место в стопке
        public void PlayAppear(Vector2 startPosition, Vector2 place)
        {
            _rect.anchoredPosition = startPosition;

            Move(place, AppearDuration, Ease.OutQuart);
        }

        // Стопка перестроилась: другой медальон добавился или ушёл
        public void MoveTo(Vector2 place) => Move(place, MoveDuration, Ease.InOutQuad);

        public void PlayHide(Action hidden)
        {
            _button.interactable = false;

            _hideTween?.Kill();
            _hideTween = _rect
                .DOScale(0f, HideDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => hidden?.Invoke());
        }

        private void OnEnable() => _button.onClick.AddListener(OnButtonClicked);

        private void OnDisable() => _button.onClick.RemoveListener(OnButtonClicked);

        private void OnDestroy()
        {
            _moveTween?.Kill();
            _hideTween?.Kill();
        }

        private void Move(Vector2 place, float duration, Ease ease)
        {
            _moveTween?.Kill();
            _moveTween = _rect
                .DOAnchorPos(place, duration)
                .SetEase(ease);
        }

        private void OnButtonClicked() => Clicked?.Invoke(this);
    }
}
