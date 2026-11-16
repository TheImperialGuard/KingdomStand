using System;
using Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Tutorial.Steps
{
    // Шаг обучения в окне обучения: слайдер заметок, «Королевский взор» или указатель. Id — ключ в сохранениях игрока.
    // Stop — только отписки: его зовут и после завершения шага, и при выгрузке сцены.
    public interface ITutorialStep
    {
        string Id { get; }

        // Игрок мог уже сделать то, чему учит шаг, — тогда шаг засчитывается без показа
        bool IsNeeded();

        void Start(TutorialPopupPresenter popup, Action completed);

        void Stop();
    }
}
