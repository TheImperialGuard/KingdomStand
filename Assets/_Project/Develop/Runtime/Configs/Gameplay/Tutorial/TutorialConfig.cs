using System.Collections.Generic;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial
{
    // Обучение в уровнях кампании: слайдеры заметок и когда они всплывают, «Королевский взор» и указатели. Состав и
    // порядок — план обучения (tutorial-plan.md).
    [CreateAssetMenu(menuName = "Configs/Gameplay/TutorialConfig", fileName = "TutorialConfig")]
    public class TutorialConfig : ScriptableObject
    {
        [SerializeField] private List<TutorialSliderInfo> _sliders = new();

        [field: SerializeField] public TutorialPointersInfo Pointers { get; private set; }

        public IReadOnlyList<TutorialSliderInfo> Sliders => _sliders;
    }
}
