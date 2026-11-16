using System;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial;
using Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Tutorial.Steps
{
    // Слайдер заметок: шаг кончается, когда слайдер закрыли любым способом.
    public class TutorialSliderStep : ITutorialStep
    {
        private readonly TutorialSliderInfo _info;

        public TutorialSliderStep(TutorialSliderInfo info)
        {
            _info = info;
        }

        public string Id => _info.Id;

        public bool IsNeeded() => true;

        public void Start(TutorialPopupPresenter popup, Action completed) => popup.ShowSlider(_info, completed);

        public void Stop()
        {
        }
    }
}
