using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Assets._Project.Develop.Runtime.Gameplay.Features.LevelNavigation;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode
{
    // Сводка волны бесконечного режима для инструментов редактора: роли дорог, акценты, бюджет и сила, задержки дорог
    // и момент, с которого волну можно вызвать досрочно.
    public class EndlessWavePlan
    {
        public EndlessWavePlan(
            EndlessWaveComposition composition,
            EndlessWaveArrangement arrangement,
            PathNumber heavyPathNumber,
            float timeToSkip)
        {
            WaveNumber = composition.WaveNumber;
            Cycle = composition.Cycle;
            Boss = composition.Boss;
            Kinds = composition.Kinds;
            Budget = composition.Budget;
            Strength = composition.Strength;
            LightAccent = composition.LightAccent;
            HeavyAccent = composition.HeavyAccent;
            HeavyPathNumber = heavyPathNumber;
            PathsDelays = arrangement.PathsDelays;
            SpawnEnd = arrangement.SpawnEnd;
            StageTime = arrangement.StageTime;
            TimeToSkip = timeToSkip;
        }

        public int WaveNumber { get; }
        public int Cycle { get; }

        // Босс волны; null — волна обычная
        public EndlessBoss Boss { get; }
        public IReadOnlyList<EndlessWaveKind> Kinds { get; }
        public float Budget { get; }
        public float Strength { get; }
        public DamageTypes? LightAccent { get; }
        public DamageTypes? HeavyAccent { get; }
        public PathNumber HeavyPathNumber { get; }

        // Задержки дорог в порядке EndlessWavesGenerator.PathNumbers, с
        public IReadOnlyList<float> PathsDelays { get; }
        public float SpawnEnd { get; }
        public float StageTime { get; }

        // С какой секунды волны открывается вызов следующей: позже конца спавна, если иначе голова следующей волны
        // догонит хвост этой ближе безопасного расстояния
        public float TimeToSkip { get; }
    }
}
