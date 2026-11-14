using System;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless
{
    // Враг тира: с какой и до какой волны генератор может взять его в волну, его цена в бюджете силы волны и потолок
    // врагов этого вида на волну.
    [Serializable]
    public class EndlessTierEnemy
    {
        public const int NoLastWave = 0;

        [field: SerializeField] public MeleeConfig EnemyConfig { get; private set; }
        [field: SerializeField, Min(1)] public int FirstWave { get; private set; } = 1;

        [field: Tooltip("Последняя волна, на которой вид ещё выходит; 0 — без конца")]
        [field: SerializeField, Min(0)] public int LastWave { get; private set; } = NoLastWave;

        [field: Tooltip("Цена вида в бюджете силы волны: врагов вида = его доля бюджета / цена")]
        [field: SerializeField, Min(0)] public float Price { get; private set; } = 100f;

        [field: Tooltip("Потолок врагов вида на волну, округляется; пустая таблица — без потолка")]
        [field: SerializeField] public WaveValueTable MaxPerWave { get; private set; } = new();

        [field: Tooltip("Выходит на своей дороге последним, после более медленных видов (некромант)")]
        [field: SerializeField] public bool SpawnsLast { get; private set; }

        public bool HasMaxPerWave => MaxPerWave.Keys.Count > 0;

        // Вид может выйти в волне с этим номером, если бюджет тира его оплатит
        public bool IsOpen(int waveNumber)
        {
            return EnemyConfig != null
                && Price > 0f
                && FirstWave <= waveNumber
                && (LastWave == NoLastWave || waveNumber <= LastWave);
        }
    }
}
