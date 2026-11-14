using System.Collections.Generic;
using System.Linq;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode;

namespace Assets._Project.Develop.Editor.EndlessMode
{
    // Строка таблицы волн в инспекторе режима: что генератор собрал для одной волны. Виды врагов — по именам ассетов
    // (у волков нет CharacterName), в порядке тиров. Босс и его сопровождение — отдельно: в счёт тиров они не входят.
    public class EndlessWaveSummary
    {
        private const string KindsSeparator = ", ";
        private const string NoAccentText = "—";
        private const string UnprotectedAccentText = "без защиты";
        private const string PhysicAccentText = "броня";
        private const string MagicAccentText = "маг.";

        private EndlessWaveSummary(
            int waveNumber,
            int enemiesCount,
            IReadOnlyList<int> tiersEnemiesCounts,
            int bossEscortCount,
            string composition,
            string bossName,
            float budget,
            float strength,
            string heavyPath,
            string lightAccent,
            string heavyAccent,
            float healthMultiplier,
            IReadOnlyList<float> pathsDelays,
            float timeToSkip,
            float waveDuration)
        {
            WaveNumber = waveNumber;
            EnemiesCount = enemiesCount;
            TiersEnemiesCounts = tiersEnemiesCounts;
            BossEscortCount = bossEscortCount;
            Composition = composition;
            BossName = bossName;
            Budget = budget;
            Strength = strength;
            HeavyPath = heavyPath;
            LightAccent = lightAccent;
            HeavyAccent = heavyAccent;
            HealthMultiplier = healthMultiplier;
            PathsDelays = pathsDelays;
            TimeToSkip = timeToSkip;
            WaveDuration = waveDuration;
        }

        public int WaveNumber { get; }

        // Все враги волны, кроме самого босса: тиры и сопровождение
        public int EnemiesCount { get; }
        public IReadOnlyList<int> TiersEnemiesCounts { get; }
        public int BossEscortCount { get; }
        public string Composition { get; }
        public string BossName { get; }
        public float Budget { get; }
        public float Strength { get; }
        public string HeavyPath { get; }
        public string LightAccent { get; }
        public string HeavyAccent { get; }

        // Множитель здоровья обычных врагов волны
        public float HealthMultiplier { get; }

        // В порядке EndlessWavesGenerator.PathNumbers
        public IReadOnlyList<float> PathsDelays { get; }
        public float TimeToSkip { get; }
        public float WaveDuration { get; }

        public static EndlessWaveSummary Create(EndlessWavePlan plan, EndlessModeConfig config)
        {
            int[] tiersEnemiesCounts = new int[config.Tiers.Count];

            foreach (EndlessWaveKind kind in plan.Kinds)
                tiersEnemiesCounts[kind.TierIndex] += kind.Count;

            IEnumerable<string> kinds = plan.Kinds
                .OrderBy(kind => kind.TierIndex)
                .ThenBy(kind => GetIndexInTier(kind, config))
                .Select(kind => $"{kind.Enemy.name} ×{kind.Count}");

            int bossEscortCount = 0;
            string bossName = string.Empty;

            if (plan.Boss != null)
            {
                bossName = plan.Boss.Boss.name;

                foreach (EndlessEscortGroup escortGroup in plan.Boss.Escort)
                {
                    if (escortGroup.Enemy == null)
                        continue;

                    int escortCount = escortGroup.GroupSize * escortGroup.GroupsCount;

                    bossEscortCount += escortCount;
                    kinds = kinds.Append($"сопровождение {escortGroup.Enemy.name} ×{escortCount}");
                }
            }

            return new EndlessWaveSummary(
                plan.WaveNumber,
                tiersEnemiesCounts.Sum() + bossEscortCount,
                tiersEnemiesCounts,
                bossEscortCount,
                string.Join(KindsSeparator, kinds),
                bossName,
                plan.Budget,
                plan.Strength,
                plan.HeavyPathNumber.ToString(),
                FormatAccent(plan.LightAccent),
                FormatAccent(plan.HeavyAccent),
                config.HealthMultiplier.GetValue(plan.WaveNumber),
                plan.PathsDelays,
                plan.TimeToSkip,
                plan.StageTime);
        }

        public static string FormatAccent(DamageTypes? accent)
        {
            if (accent.HasValue == false)
                return NoAccentText;

            switch (accent.Value)
            {
                case DamageTypes.Physic:
                    return PhysicAccentText;

                case DamageTypes.Magic:
                    return MagicAccentText;

                default:
                    return UnprotectedAccentText;
            }
        }

        private static int GetIndexInTier(EndlessWaveKind kind, EndlessModeConfig config)
        {
            IReadOnlyList<EndlessTierEnemy> enemies = config.Tiers[kind.TierIndex].Enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].EnemyConfig == kind.Enemy)
                    return i;
            }

            return enemies.Count;
        }
    }
}
