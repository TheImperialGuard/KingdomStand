using System;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.EnemyHints
{
    // Подсказка о враге на стейдже его первого появления в кампании: через сколько секунд от начала стейджа всплывает
    // медальон с портретом. Секунда уже учитывает путь первого врага до видимой части поля.
    [Serializable]
    public class EnemyHintInfo
    {
        [field: SerializeField] public CharacterConfig Enemy { get; private set; }
        [field: SerializeField, Min(1)] public int LevelNumber { get; private set; } = 1;
        [field: SerializeField, Min(1)] public int StageNumber { get; private set; } = 1;
        [field: SerializeField, Min(0)] public float ShowTime { get; private set; }
        [field: SerializeField] public Sprite Portrait { get; private set; }
    }
}
