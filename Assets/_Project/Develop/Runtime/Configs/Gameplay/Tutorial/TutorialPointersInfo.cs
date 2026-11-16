using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial
{
    // Подсказки без окна в уровне LevelNumber: «Королевский взор» и указатели — порядок и поведение описаны в плане
    // обучения (tutorial-plan.md → «Уровень 1 — цепочка»). Отступ выреза — в единицах канваса. Указатель на только что
    // построенную башню ставит паузу не сразу, а через TowerPauseDelay секунд игрового времени: башня успевает доиграть появление.
    [Serializable]
    public class TutorialPointersInfo
    {
        [field: SerializeField, Min(1)] public int LevelNumber { get; private set; } = 1;
        [field: SerializeField] public string ContinueText { get; private set; }

        [field: SerializeField] public TutorialCameraHintInfo CameraHint { get; private set; }
        [field: SerializeField] public TutorialPointerInfo Build { get; private set; }
        [field: SerializeField] public TutorialPointerInfo Tower { get; private set; }
        [field: SerializeField] public TutorialPointerInfo StartWave { get; private set; }
        [field: SerializeField] public TutorialPointerInfo SkipWave { get; private set; }
        [field: SerializeField] public TutorialPointerInfo Upgrade { get; private set; }

        [field: SerializeField, Min(0f)] public float HolePadding { get; private set; } = 12f;
        [field: SerializeField, Min(0f)] public float TowerPauseDelay { get; private set; } = 0.6f;
    }
}
