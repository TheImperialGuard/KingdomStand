using System;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode
{
    // Циклы ролей дорог. Цикл волны — её номер, делённый на шаг боссов: волны 1–9 — цикл 0, 10–19 — цикл 1, боссовая
    // волна открывает новый цикл. В каждом цикле одна дорога тяжёлая — по кругу от сдвига, выбранного зерном забега, —
    // остальные лёгкие. С цикла 1 у лёгкой и тяжёлой части свой акцент: тип сопротивления, виды с которым выпадают чаще.
    public class EndlessCycles
    {
        public const int FirstCycle = 0;

        // Акценты по сопротивлению: бронированные, магостойкие и без защиты
        private static readonly DamageTypes[] _accents = { DamageTypes.Physic, DamageTypes.Magic, DamageTypes.None };

        private readonly int _bossWavesStep;
        private readonly int _pathsCount;
        private readonly int _runShift;
        private readonly EndlessRandomStreams _randomStreams;

        public EndlessCycles(EndlessModeConfig config, int pathsCount, EndlessRandomStreams randomStreams)
        {
            if (pathsCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(pathsCount), pathsCount, "Для циклов ролей нужна хотя бы одна дорога");

            _bossWavesStep = Math.Max(1, config.BossWavesStep);
            _pathsCount = pathsCount;
            _randomStreams = randomStreams;
            _runShift = randomStreams.CreateRunRandom().Next(pathsCount);
        }

        public int GetCycle(int waveNumber) => waveNumber / _bossWavesStep;

        // Место волны в цикле: 0 — боссовая волна, 1 — первая волна после неё
        public int GetPlaceInCycle(int waveNumber) => waveNumber % _bossWavesStep;

        public int GetHeavyPathIndex(int cycle) => (_runShift + cycle) % _pathsCount;

        // Акцент части волны в цикле; null — без акцента (цикл 0, универсальный старт). Тиры лёгкой дороги берут акцент
        // лёгкой части, тяжёлой и эскорта — тяжёлой
        public DamageTypes? GetAccent(int cycle, EndlessRoadRoles road)
        {
            if (cycle == FirstCycle)
                return null;

            Random random = _randomStreams.CreateCycleRandom(cycle);
            DamageTypes lightAccent = PickAccent(random);
            DamageTypes heavyAccent = PickAccent(random);

            return road == EndlessRoadRoles.Light ? lightAccent : heavyAccent;
        }

        private static DamageTypes PickAccent(Random random) => _accents[random.Next(_accents.Length)];
    }
}
