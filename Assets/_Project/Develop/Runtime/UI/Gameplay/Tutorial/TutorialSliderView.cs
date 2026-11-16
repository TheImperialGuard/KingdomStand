using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // Слайдер заметок обучения (spec.md → «Слайдер заметок»): картинка в рамке, заголовок, текст, точки страниц и кнопки.
    // Колонка стоит по центру safe area и, если не помещается по высоте, уменьшается целиком. Вью растянута на safe area
    // и пересчитывает масштаб колонки при смене страницы и размера экрана.
    public class TutorialSliderView : MonoBehaviour
    {
        private const float ColumnVerticalMargins = 48f;
        private const float DisabledBackAlpha = 0.4f;
        private const float EnabledAlpha = 1f;

        public event Action BackClicked;
        public event Action NextClicked;
        public event Action SkipClicked;

        [SerializeField] private RectTransform _column;

        [SerializeField] private Image _picture;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _text;

        [SerializeField] private GameObject _dotsRow;
        [SerializeField] private Image _dotTemplate;
        [SerializeField] private Sprite _currentDotSprite;
        [SerializeField] private Sprite _otherDotSprite;

        [SerializeField] private Button _backButton;
        [SerializeField] private CanvasGroup _backGroup;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TMP_Text _nextLabel;
        [SerializeField] private GameObject _nextTriangle;
        [SerializeField] private Button _skipButton;
        [SerializeField] private RectTransform _buttonsCenter;

        // Кнопки-тексты без плашки: ширина корня — по содержимому, а сдвиг при нажатии — у содержимого, которое
        // группа ряда не двигает (components.md скилла → «Кнопка-текст в ряду с layout-группой»)
        [SerializeField] private List<RectTransform> _textButtonContents = new();

        private readonly List<Image> _dots = new();

        private RectTransform _area;
        private string _nextText;

        private void Awake()
        {
            _area = (RectTransform)transform;
            _nextText = _nextLabel.text;

            _dotTemplate.gameObject.SetActive(false);

            _backButton.onClick.AddListener(OnBackButtonClicked);
            _nextButton.onClick.AddListener(OnNextButtonClicked);
            _skipButton.onClick.AddListener(OnSkipButtonClicked);
        }

        private void OnDestroy()
        {
            _backButton.onClick.RemoveListener(OnBackButtonClicked);
            _nextButton.onClick.RemoveListener(OnNextButtonClicked);
            _skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                UpdateLayout();
        }

        public void SetVisible(bool isVisible) => gameObject.SetActive(isVisible);

        public void SetNote(Sprite picture, string title, string text)
        {
            _picture.sprite = picture;
            _title.text = title;
            _text.text = text;
        }

        // Одной странице точки не нужны
        public void SetDots(int count, int currentIndex)
        {
            bool hasDots = count > 1;

            _dotsRow.SetActive(hasDots);

            if (hasDots == false)
                return;

            while (_dots.Count < count)
                _dots.Add(Instantiate(_dotTemplate, _dotTemplate.transform.parent));

            for (int i = 0; i < _dots.Count; i++)
            {
                bool isUsed = i < count;

                _dots[i].gameObject.SetActive(isUsed);

                if (isUsed)
                    _dots[i].sprite = i == currentIndex ? _currentDotSprite : _otherDotSprite;
            }
        }

        public void SetBackButton(bool isVisible, bool isInteractable)
        {
            _backButton.gameObject.SetActive(isVisible);
            _backButton.interactable = isInteractable;
            _backGroup.alpha = isInteractable ? EnabledAlpha : DisabledBackAlpha;
        }

        public void SetSkipButtonVisible(bool isVisible) => _skipButton.gameObject.SetActive(isVisible);

        public void ShowNextButton()
        {
            _nextLabel.text = _nextText;
            _nextTriangle.SetActive(true);
        }

        // Последняя страница: вместо «Далее ▶» — завершение без треугольника
        public void ShowFinishButton(string text)
        {
            _nextLabel.text = text;
            _nextTriangle.SetActive(false);
        }

        public void UpdateLayout()
        {
            FitTextButtons();

            _column.localScale = Vector3.one;

            // Ряд кнопок и точки стоят в контейнерах без своей группы раскладки: перестройка колонки до них не доходит
            LayoutRebuilder.ForceRebuildLayoutImmediate(_buttonsCenter);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_dotTemplate.transform.parent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_column);

            float columnHeight = _column.rect.height;

            if (columnHeight <= 0f)
                return;

            float scale = Mathf.Min(1f, (_area.rect.height - ColumnVerticalMargins) / columnHeight);

            _column.localScale = new Vector3(scale, scale, 1f);
        }

        private void FitTextButtons()
        {
            foreach (RectTransform content in _textButtonContents)
            {
                if (content.gameObject.activeInHierarchy == false)
                    continue;

                LayoutRebuilder.ForceRebuildLayoutImmediate(content);

                float width = LayoutUtility.GetPreferredWidth(content);
                RectTransform root = (RectTransform)content.parent;

                root.sizeDelta = new Vector2(width, root.sizeDelta.y);

                if (root.TryGetComponent(out LayoutElement layoutElement))
                    layoutElement.preferredWidth = width;
            }
        }

        private void OnBackButtonClicked() => BackClicked?.Invoke();

        private void OnNextButtonClicked() => NextClicked?.Invoke();

        private void OnSkipButtonClicked() => SkipClicked?.Invoke();
    }
}
