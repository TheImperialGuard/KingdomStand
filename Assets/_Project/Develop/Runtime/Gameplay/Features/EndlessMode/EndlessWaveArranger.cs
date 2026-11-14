using System.Collections.Generic;
using System.Linq;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Stages;
using Assets._Project.Develop.Runtime.Gameplay.Features.LevelNavigation;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode
{
    // Раскладка волны бесконечного режима: по какой дороге и полосе, в каком порядке и когда выходят враги состава.
    //
    // Тяжёлые тиры идут по тяжёлой дороге цикла, лёгкие — по лёгкой; лёгких дорог несколько — звенья раздаются им по
    // очереди, дорога одна — обе части идут по ней. Если у тяжёлой дороги в волне нет своей части (начало забега, пока
    // толстые не открыты), лёгкие виды раздаются целиком на все дороги так, чтобы сила дорог вышла ровной. На дороге виды стоят от быстрых к медленным, вид «выходит последним»
    // — в самом конце. Враги вида делятся на звенья по размеру звена тира; одиночки чередуют полосы, звено из двух идёт
    // по краям, из трёх — по всем полосам, больше — рядами по 2–3, ряд от ряда на минимальной дистанции. Звенья дороги
    // стоят равномерно в окне выхода, начиная с задержки дороги; не умещаются — окно растягивается.
    //
    // Эскорт — звенья поровну по всему окну тяжёлой дороги, вне порядка. Босс выходит первым по тяжёлой дороге,
    // сопровождение — по расписанию. Выходы одного вида на одной полосе с равным шагом сливаются в один поток.
    public class EndlessWaveArranger
    {
        private const int MaxLanesInRow = 3;
        private const int PairLanesCount = 2;

        private const float MinDelayBetweenSpawns = 0.1f;
        private const float BossStartTime = 0f;

        // Моменты выхода округляются до сотых секунды, поэтому соседние шаги одного ряда расходятся на сотую: равенство
        // шагов — с допуском чуть больше неё
        private const float TimeRounding = 100f;
        private const float SameStepTolerance = 0.015f;

        // Звенья эскорта стоят посередине своих долей окна, а не на их краях, где выходят звенья основной части
        private const float EscortOffsetInStep = 0.5f;

        private const float NoHeadSpeed = 0f;

        private static readonly PathLane[] _singleLanes = { PathLane.Left, PathLane.Right };
        private static readonly PathLane[] _pairLanes = { PathLane.Left, PathLane.Right };
        private static readonly PathLane[] _tripleLanes = { PathLane.Left, PathLane.Center, PathLane.Right };

        private readonly EndlessModeConfig _config;
        private readonly IReadOnlyList<PathNumber> _pathNumbers;

        public EndlessWaveArranger(EndlessModeConfig config, IReadOnlyList<PathNumber> pathNumbers)
        {
            _config = config;
            _pathNumbers = pathNumbers;
        }

        // Скорость головы — самого быстрого врага вне эскорта и сопровождения — на каждой дороге; 0 — дорога пуста
        public float[] GetHeadSpeeds(EndlessWaveComposition composition, int heavyPathIndex)
        {
            float[] headSpeeds = new float[_pathNumbers.Count];
            List<EnemyGroup>[] pathsGroups = SplitByPaths(composition, heavyPathIndex);

            for (int i = 0; i < headSpeeds.Length; i++)
                headSpeeds[i] = pathsGroups[i].Count > 0 ? pathsGroups[i][0].Speed : NoHeadSpeed;

            if (composition.HasBoss)
                headSpeeds[heavyPathIndex] = Mathf.Max(headSpeeds[heavyPathIndex], composition.Boss.Boss.MoveSpeed);

            return headSpeeds;
        }

        public EndlessWaveArrangement Arrange(EndlessWaveComposition composition, int heavyPathIndex, IReadOnlyList<float> pathsDelays)
        {
            int waveNumber = composition.WaveNumber;
            float spawnWindow = Mathf.Max(0f, _config.SpawnTime.GetValue(waveNumber));
            float restTime = Mathf.Max(0f, _config.RestTime.GetValue(waveNumber));

            List<Spawn> spawns = new();
            List<EndlessRoadTail>[] pathsTails = new List<EndlessRoadTail>[_pathNumbers.Count];
            List<EnemyGroup>[] pathsGroups = SplitByPaths(composition, heavyPathIndex);

            for (int i = 0; i < pathsTails.Length; i++)
                pathsTails[i] = new List<EndlessRoadTail>();

            if (composition.HasBoss)
                PlaceBoss(composition.Boss, heavyPathIndex, spawns, pathsTails[heavyPathIndex]);

            for (int i = 0; i < pathsGroups.Length; i++)
                PlacePathGroups(i, pathsGroups[i], pathsDelays[i], spawnWindow, spawns, pathsTails[i]);

            PlaceEscortTier(composition, heavyPathIndex, pathsDelays[heavyPathIndex], spawnWindow, spawns);

            float spawnEnd = spawns.Count > 0 ? spawns.Max(spawn => spawn.Time) : 0f;

            return new EndlessWaveArrangement(
                MergeIntoStreams(spawns),
                spawnEnd,
                spawnEnd + restTime,
                heavyPathIndex,
                pathsDelays.ToArray(),
                pathsTails);
        }

        // Звенья основной части по дорогам, на каждой — в порядке выхода
        private List<EnemyGroup>[] SplitByPaths(EndlessWaveComposition composition, int heavyPathIndex)
        {
            List<EnemyGroup>[] pathsGroups = new List<EnemyGroup>[_pathNumbers.Count];

            for (int i = 0; i < pathsGroups.Length; i++)
                pathsGroups[i] = new List<EnemyGroup>();

            List<EndlessWaveKind> heavyKinds = composition.Kinds.Where(kind => kind.Road == EndlessRoadRoles.Heavy).ToList();
            List<EndlessWaveKind> lightKinds = composition.Kinds.Where(kind => kind.Road == EndlessRoadRoles.Light).ToList();
            List<int> lightPathIndices = Enumerable.Range(0, _pathNumbers.Count).Where(i => i != heavyPathIndex).ToList();

            // Одна дорога: обе части идут по ней в общем порядке
            if (lightPathIndices.Count == 0)
            {
                pathsGroups[heavyPathIndex].AddRange(SplitIntoGroups(SortByExitOrder(heavyKinds.Concat(lightKinds))));
                return pathsGroups;
            }

            // У тяжёлой дороги нет своей части (начало забега, пока толстые не открыты): лёгкая часть делится на все дороги
            if (heavyKinds.Count == 0 && composition.HasBoss == false)
            {
                SpreadKindsByStrength(pathsGroups, lightKinds, lightPathIndices.Append(heavyPathIndex).ToList());
                return pathsGroups;
            }

            pathsGroups[heavyPathIndex].AddRange(SplitIntoGroups(SortByExitOrder(heavyKinds)));

            List<EnemyGroup> lightGroups = SplitIntoGroups(SortByExitOrder(lightKinds));

            for (int i = 0; i < lightGroups.Count; i++)
                pathsGroups[lightPathIndices[i % lightPathIndices.Count]].Add(lightGroups[i]);

            return pathsGroups;
        }

        // Каждый вид целиком — на одну дорогу: от сильного к слабому, каждый на дорогу, где сила сейчас меньше (при
        // равенстве — на первую по списку). Так дороги выходят примерно ровными, а вид идёт по одной дороге
        private static void SpreadKindsByStrength(List<EnemyGroup>[] pathsGroups, List<EndlessWaveKind> kinds, List<int> pathIndices)
        {
            float[] pathsStrengths = new float[pathIndices.Count];
            List<EndlessWaveKind>[] pathsKinds = new List<EndlessWaveKind>[pathIndices.Count];

            for (int i = 0; i < pathsKinds.Length; i++)
                pathsKinds[i] = new List<EndlessWaveKind>();

            foreach (EndlessWaveKind kind in kinds.OrderByDescending(kind => kind.Strength).ThenBy(kind => kind.TierIndex))
            {
                int weakestIndex = 0;

                for (int i = 1; i < pathsStrengths.Length; i++)
                {
                    if (pathsStrengths[i] < pathsStrengths[weakestIndex])
                        weakestIndex = i;
                }

                pathsKinds[weakestIndex].Add(kind);
                pathsStrengths[weakestIndex] += kind.Strength;
            }

            for (int i = 0; i < pathIndices.Count; i++)
                pathsGroups[pathIndices[i]].AddRange(SplitIntoGroups(SortByExitOrder(pathsKinds[i])));
        }

        // От быстрых к медленным, при равной скорости — по порядку тиров; «выходит последним» — в конце
        private static List<EndlessWaveKind> SortByExitOrder(IEnumerable<EndlessWaveKind> kinds)
        {
            return kinds
                .OrderBy(kind => kind.SpawnsLast)
                .ThenByDescending(kind => kind.Enemy.MoveSpeed)
                .ThenBy(kind => kind.TierIndex)
                .ToList();
        }

        // Враги вида делятся на звенья размера звена тира; размеры звеньев вида — поровну
        private static List<EnemyGroup> SplitIntoGroups(IEnumerable<EndlessWaveKind> kinds)
        {
            List<EnemyGroup> groups = new();

            foreach (EndlessWaveKind kind in kinds)
            {
                int groupsCount = Mathf.CeilToInt((float)kind.Count / kind.GroupSize);
                int baseSize = kind.Count / groupsCount;
                int extraCount = kind.Count % groupsCount;

                for (int i = 0; i < groupsCount; i++)
                    groups.Add(new EnemyGroup(kind.Enemy, i < extraCount ? baseSize + 1 : baseSize));
            }

            return groups;
        }

        private void PlaceBoss(EndlessBoss boss, int pathIndex, List<Spawn> spawns, List<EndlessRoadTail> tails)
        {
            AddSpawn(spawns, pathIndex, PathLane.Center, BossStartTime, boss.Boss);
            tails.Add(new EndlessRoadTail(boss.Boss.MoveSpeed, BossStartTime));

            int singleLaneIndex = 0;

            foreach (EndlessEscortGroup escortGroup in boss.Escort)
            {
                if (escortGroup.Enemy == null)
                    continue;

                EnemyGroup group = new(escortGroup.Enemy, escortGroup.GroupSize);

                for (int i = 0; i < escortGroup.GroupsCount; i++)
                {
                    float startTime = escortGroup.FirstDelay + i * escortGroup.Period;

                    PlaceGroup(spawns, pathIndex, group, startTime, ref singleLaneIndex);
                }
            }
        }

        // Звенья дороги стоят равномерно в окне от задержки дороги; звено не выходит раньше, чем предыдущее отойдёт на
        // минимальную дистанцию
        private void PlacePathGroups(
            int pathIndex,
            List<EnemyGroup> groups,
            float pathDelay,
            float spawnWindow,
            List<Spawn> spawns,
            List<EndlessRoadTail> tails)
        {
            if (groups.Count == 0)
                return;

            float step = Mathf.Max(0f, (spawnWindow - pathDelay) / groups.Count);
            float freeTime = pathDelay;
            int singleLaneIndex = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                EnemyGroup group = groups[i];
                float startTime = Mathf.Max(pathDelay + i * step, freeTime);
                float lastRowTime = PlaceGroup(spawns, pathIndex, group, startTime, ref singleLaneIndex);

                tails.Add(new EndlessRoadTail(group.Speed, lastRowTime));
                freeTime = lastRowTime + GetRowInterval(group.Speed);
            }
        }

        private void PlaceEscortTier(
            EndlessWaveComposition composition,
            int heavyPathIndex,
            float pathDelay,
            float spawnWindow,
            List<Spawn> spawns)
        {
            List<EndlessWaveKind> escortKinds = composition.Kinds.Where(kind => kind.Road == EndlessRoadRoles.Escort).ToList();
            List<EnemyGroup> groups = SplitIntoGroups(SortByExitOrder(escortKinds));

            if (groups.Count == 0)
                return;

            float step = Mathf.Max(0f, (spawnWindow - pathDelay) / groups.Count);
            int singleLaneIndex = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                float startTime = pathDelay + (i + EscortOffsetInStep) * step;

                PlaceGroup(spawns, heavyPathIndex, groups[i], startTime, ref singleLaneIndex);
            }
        }

        // Звено рядами по полосам. Возвращает момент выхода последнего ряда
        private float PlaceGroup(List<Spawn> spawns, int pathIndex, EnemyGroup group, float startTime, ref int singleLaneIndex)
        {
            if (group.Size == 1)
            {
                PathLane lane = _singleLanes[singleLaneIndex % _singleLanes.Length];

                singleLaneIndex++;
                AddSpawn(spawns, pathIndex, lane, startTime, group.Enemy);

                return startTime;
            }

            int rowsCount = Mathf.CeilToInt((float)group.Size / MaxLanesInRow);
            int lanesCount = Mathf.CeilToInt((float)group.Size / rowsCount);
            PathLane[] lanes = lanesCount == PairLanesCount ? _pairLanes : _tripleLanes;
            int baseCount = group.Size / lanesCount;
            int extraCount = group.Size % lanesCount;
            float rowInterval = GetRowInterval(group.Speed);
            float lastRowTime = startTime;

            for (int i = 0; i < lanesCount; i++)
            {
                int laneCount = i < extraCount ? baseCount + 1 : baseCount;

                for (int row = 0; row < laneCount; row++)
                {
                    float time = startTime + row * rowInterval;

                    AddSpawn(spawns, pathIndex, lanes[i], time, group.Enemy);
                    lastRowTime = Mathf.Max(lastRowTime, time);
                }
            }

            return lastRowTime;
        }

        private float GetRowInterval(float speed) => speed > 0f ? _config.MinSpacing / speed : MinDelayBetweenSpawns;

        private static void AddSpawn(List<Spawn> spawns, int pathIndex, PathLane lane, float time, MeleeConfig enemy)
        {
            float roundedTime = Mathf.Round(time * TimeRounding) / TimeRounding;

            spawns.Add(new Spawn(pathIndex, lane, roundedTime, enemy));
        }

        // Выходы одного вида на одной полосе дороги с равным шагом — один поток; потоки — по времени старта
        private List<EnemiesWaveConfig> MergeIntoStreams(List<Spawn> spawns)
        {
            List<Spawn> sortedSpawns = spawns
                .OrderBy(spawn => spawn.PathIndex)
                .ThenBy(spawn => spawn.Lane)
                .ThenBy(spawn => spawn.Time)
                .ToList();

            List<EnemiesWaveConfig> streams = new();
            int index = 0;

            while (index < sortedSpawns.Count)
            {
                Spawn first = sortedSpawns[index];
                int count = 1;
                float step = MinDelayBetweenSpawns;

                if (index + 1 < sortedSpawns.Count && IsSameStream(first, sortedSpawns[index + 1]))
                {
                    float secondStep = sortedSpawns[index + 1].Time - first.Time;

                    if (secondStep >= MinDelayBetweenSpawns)
                    {
                        step = secondStep;
                        count = 2;

                        while (index + count < sortedSpawns.Count && IsNextInStream(sortedSpawns, index, count, step))
                            count++;
                    }
                }

                List<WaveEnemyGroup> enemyGroups = new() { new WaveEnemyGroup(first.Enemy, count) };

                streams.Add(new EnemiesWaveConfig(enemyGroups, _pathNumbers[first.PathIndex], first.Lane, first.Time, step));
                index += count;
            }

            return streams.OrderBy(stream => stream.StartDelay).ToList();
        }

        private static bool IsNextInStream(List<Spawn> sortedSpawns, int firstIndex, int count, float step)
        {
            Spawn next = sortedSpawns[firstIndex + count];
            Spawn previous = sortedSpawns[firstIndex + count - 1];

            return IsSameStream(sortedSpawns[firstIndex], next) && Mathf.Abs(next.Time - previous.Time - step) <= SameStepTolerance;
        }

        private static bool IsSameStream(Spawn first, Spawn second)
            => first.PathIndex == second.PathIndex && first.Lane == second.Lane && first.Enemy == second.Enemy;

        // Звено: враги одного вида, которые выходят вместе
        private class EnemyGroup
        {
            public EnemyGroup(MeleeConfig enemy, int size)
            {
                Enemy = enemy;
                Size = size;
            }

            public MeleeConfig Enemy { get; }
            public int Size { get; }
            public float Speed => Enemy.MoveSpeed;
        }

        // Выход одного врага: дорога, полоса, момент от начала волны
        private readonly struct Spawn
        {
            public Spawn(int pathIndex, PathLane lane, float time, MeleeConfig enemy)
            {
                PathIndex = pathIndex;
                Lane = lane;
                Time = time;
                Enemy = enemy;
            }

            public int PathIndex { get; }
            public PathLane Lane { get; }
            public float Time { get; }
            public MeleeConfig Enemy { get; }
        }
    }
}
