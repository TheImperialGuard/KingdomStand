using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay.Tutorial
{
    // На что показывает указатель обучения. Вырез считается заново каждый кадр: цель может двигать камера или анимация.
    public interface ITutorialPointerTarget
    {
        // Вырез вокруг цели в локальных координатах space; false — цели больше нет или она за камерой
        bool TryGetHole(RectTransform space, out TutorialHole hole);
    }
}
