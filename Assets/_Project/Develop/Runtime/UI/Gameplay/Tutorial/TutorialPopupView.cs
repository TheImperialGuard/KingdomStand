using Assets._Project.Develop.Runtime.Gameplay.Features.InputFeature;
using Assets._Project.Develop.Runtime.UI.Core.Popups;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // Общий слой обучения в бою: одно затемнение на всю цепочку подсказок уровня, поверх — слайдер заметок,
    // «Королевский взор», указатель и надпись «Нажмите в любом месте», чтобы между ними затемнение не мигало.
    // Затемнение — антикликер окна; метка на нём пропускает управление камерой под «Королевским взором».
    public class TutorialPopupView : PopupViewBase
    {
        [SerializeField] private TutorialSliderView _sliderView;
        [SerializeField] private TutorialDimmerView _dimmer;
        [SerializeField] private CameraInputPassThrough _cameraInputPassThrough;
        [SerializeField] private TutorialCameraHintView _cameraHintView;
        [SerializeField] private TutorialPointerView _pointerView;
        [SerializeField] private TutorialContinueHintView _continueHintView;

        public TutorialSliderView SliderView => _sliderView;

        public TutorialDimmerView Dimmer => _dimmer;

        public CameraInputPassThrough CameraInputPassThrough => _cameraInputPassThrough;

        public TutorialCameraHintView CameraHintView => _cameraHintView;

        public TutorialPointerView PointerView => _pointerView;

        public TutorialContinueHintView ContinueHintView => _continueHintView;

        // Окна башен, открытые позже, встают выше; указатель поднимает слой обучения над ними — они видны через вырез
        public void BringToFront() => transform.SetAsLastSibling();
    }
}
