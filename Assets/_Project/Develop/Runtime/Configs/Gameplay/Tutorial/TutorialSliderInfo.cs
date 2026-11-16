using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Encyclopedia;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial
{
    // Слайдер заметок обучения в уровне кампании. Id — ключ в сохранениях игрока: показанный слайдер больше не всплывает,
    // поэтому менять его у готовой строки нельзя. Враг нужен только триггеру EnemyHint. Последняя страница вместо
    // «Далее» показывает FinishText.
    [Serializable]
    public class TutorialSliderInfo
    {
        [SerializeField] private List<EncyclopediaNoteConfig> _pages = new();

        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField, Min(1)] public int LevelNumber { get; private set; } = 1;
        [field: SerializeField] public TutorialTriggers Trigger { get; private set; }
        [field: SerializeField] public CharacterConfig Enemy { get; private set; }
        [field: SerializeField] public string FinishText { get; private set; }

        public IReadOnlyList<EncyclopediaNoteConfig> Pages => _pages;
    }
}
