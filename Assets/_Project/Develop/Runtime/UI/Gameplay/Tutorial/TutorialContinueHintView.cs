using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // Надпись «Нажмите в любом месте, чтобы продолжить» внизу safe area — у «Королевского взора» и необязательных
    // указателей. Прозрачность пульсирует .55 → 1 → .55 за 1,6 с.
    public class TutorialContinueHintView : MonoBehaviour
    {
        private const float DimmedAlpha = 0.55f;
        private const float PulseHalfDuration = 0.8f;
        private const float HideDuration = 0.25f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _text;

        private Tween _animation;

        private void OnDestroy() => _animation?.Kill();

        public void Show(string text)
        {
            gameObject.SetActive(true);
            _animation?.Kill();

            _text.text = text;
            _group.alpha = DimmedAlpha;

            _animation = _group
                .DOFade(1f, PulseHalfDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        public void Hide()
        {
            if (gameObject.activeSelf == false)
                return;

            _animation?.Kill();

            _animation = _group
                .DOFade(0f, HideDuration)
                .SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}
