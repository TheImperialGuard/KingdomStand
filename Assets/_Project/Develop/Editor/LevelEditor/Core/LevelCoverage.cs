using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Gameplay.Features.LevelNavigation;
using UnityEngine;

namespace Assets._Project.Develop.Editor.LevelEditor
{
    // Покрытие полос дорог уровня местами под башни. Полоса считается по первому обращению: одна и та же нужна многим
    // волнам.
    public class LevelCoverage
    {
        private readonly LevelPrefabInfo _prefab;
        private readonly Dictionary<(LevelRoad, PathLane), LaneCoverage> _lanes = new();

        public LevelCoverage(LevelPrefabInfo prefab, IReadOnlyList<float> ranges)
        {
            _prefab = prefab;
            Ranges = ranges;
        }

        public IReadOnlyList<float> Ranges { get; }

        // Без целого пути, мест или дальностей покрытия нет.
        public bool TryGet(LevelRoad road, PathLane lane, out LaneCoverage coverage)
        {
            coverage = null;

            if (road == null || Ranges.Count == 0 || _prefab.TowerSlots.Count == 0)
                return false;

            if (_lanes.TryGetValue((road, lane), out coverage))
                return coverage != null;

            if (road.LanePoints.TryGetValue(lane, out Vector3[] points))
                coverage = RoadCoverageCalculator.Calculate(points, _prefab.TowerSlots, Ranges);

            _lanes[(road, lane)] = coverage;

            return coverage != null;
        }
    }
}
