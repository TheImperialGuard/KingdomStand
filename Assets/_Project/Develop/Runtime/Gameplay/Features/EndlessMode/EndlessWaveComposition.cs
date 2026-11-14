using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode
{
    // Состав волны бесконечного режима — первый шаг генератора: босс, виды врагов с числом и дорогой, бюджет силы и сила,
    // которая из него вышла, акценты цикла.
    public class EndlessWaveComposition
    {
        public EndlessWaveComposition(
            int waveNumber,
            int cycle,
            EndlessBoss boss,
            List<EndlessWaveKind> kinds,
            float budget,
            DamageTypes? lightAccent,
            DamageTypes? heavyAccent)
        {
            WaveNumber = waveNumber;
            Cycle = cycle;
            Boss = boss;
            Kinds = kinds;
            Budget = budget;
            LightAccent = lightAccent;
            HeavyAccent = heavyAccent;

            foreach (EndlessWaveKind kind in kinds)
                Strength += kind.Strength;
        }

        public int WaveNumber { get; }
        public int Cycle { get; }

        // Босс волны; null — волна обычная
        public EndlessBoss Boss { get; }
        public IReadOnlyList<EndlessWaveKind> Kinds { get; }
        public float Budget { get; }

        // Сумма цен вышедших врагов: сходится с бюджетом в пределах цены самого дешёвого вида
        public float Strength { get; }
        public DamageTypes? LightAccent { get; }
        public DamageTypes? HeavyAccent { get; }
        public bool HasBoss => Boss != null;
    }
}
