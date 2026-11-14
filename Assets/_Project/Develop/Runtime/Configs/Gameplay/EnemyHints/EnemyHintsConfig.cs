using System.Collections.Generic;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.EnemyHints
{
    // Подсказки о новых врагах кампании. Моменты посчитаны заранее по уровням и балансу — пересчёт описан в плане
    // подсказок (enemy-hints-unity\timings).
    [CreateAssetMenu(menuName = "Configs/Gameplay/EnemyHintsConfig", fileName = "EnemyHintsConfig")]
    public class EnemyHintsConfig : ScriptableObject
    {
        [SerializeField] private List<EnemyHintInfo> _hints = new();

        public IReadOnlyList<EnemyHintInfo> Hints => _hints;
    }
}
