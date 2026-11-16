using Assets._Project.Develop.Runtime.UI.Core.Popups;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // Общий слой обучения в бою: одно затемнение на всю цепочку подсказок уровня, поверх — слайдер заметок (дальше —
    // «Королевский взор» и указатели), чтобы между ними затемнение не мигало.
    public class TutorialPopupView : PopupViewBase
    {
        [SerializeField] private TutorialSliderView _sliderView;

        public TutorialSliderView SliderView => _sliderView;
    }
}
