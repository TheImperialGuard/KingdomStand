using System.Collections.Generic;
using System.Linq;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Towers;
using UnityEditor;
using UnityEngine;

namespace Assets._Project.Develop.Editor.LevelEditor
{
    // Покрытие дороги местами под башни — как TargetSelector: башня стоит в точке места и достаёт до врага, если между
    // их корнями не дальше дальности башни. Места и дороги уровней лежат на одной высоте, а корень летающего врага идёт
    // по дороге, поэтому расстояние берётся на плоскости XZ. Отрезки пути — по ломаной полосы, как в RoadLanes.
    [InitializeOnLoad]
    public static class RoadCoverageCalculator
    {
        // Куски ближе этого сливаются в один: стык отрезков полосы не должен рвать покрытие.
        private const float MERGE_GAP = 0.001f;

        // Поиск конфигов через AssetDatabase — почти весь пересчёт статистики уровня, поэтому список башен хранится
        // до изменения проекта; дальности читаются из конфигов каждый раз.
        private static List<ShootingTowerConfig> _towers;

        static RoadCoverageCalculator() => EditorApplication.projectChanged += () => _towers = null;

        public static List<float> CollectTowerRanges()
        {
            if (_towers == null || _towers.Any(tower => tower == null))
                _towers = LevelEditorAssets.FindTowerConfigs();

            return CollectRanges(_towers);
        }

        // Дальности башен без повторов, по возрастанию: покрытие зависит только от дальности.
        public static List<float> CollectRanges(IEnumerable<ShootingTowerConfig> towers)
        {
            return towers
                .Select(tower => tower.AttackRange)
                .Where(range => range > 0f)
                .Distinct()
                .OrderBy(range => range)
                .ToList();
        }

        public static LaneCoverage Calculate(
            IReadOnlyList<Vector3> lanePoints,
            IReadOnlyList<LevelTowerSlot> slots,
            IReadOnlyList<float> ranges)
        {
            LaneCoverage coverage = new(ranges);

            foreach (LevelTowerSlot slot in slots)
            {
                List<PathInterval>[] intervalsByRange = new List<PathInterval>[ranges.Count];
                bool isReaching = false;

                for (int i = 0; i < ranges.Count; i++)
                {
                    intervalsByRange[i] = CalculateIntervals(lanePoints, slot.Position, ranges[i]);
                    isReaching |= intervalsByRange[i].Count > 0;
                }

                if (isReaching)
                    coverage.Slots.Add(new SlotCoverage(slot.Number, intervalsByRange));
            }

            return coverage;
        }

        // Отрезки полосы в круге дальности вокруг center, по порядку пути.
        public static List<PathInterval> CalculateIntervals(IReadOnlyList<Vector3> points, Vector3 center, float range)
        {
            List<PathInterval> intervals = new();
            Vector2 planeCenter = ToPlane(center);
            float segmentStart = 0f;

            for (int i = 1; i < points.Count; i++)
            {
                float segmentLength = Vector3.Distance(points[i - 1], points[i]);

                if (TryIntersect(ToPlane(points[i - 1]), ToPlane(points[i]), planeCenter, range, out float fromShare, out float toShare))
                {
                    float from = segmentStart + fromShare * segmentLength;
                    float to = segmentStart + toShare * segmentLength;

                    AddMerged(intervals, new PathInterval(from, to));
                }

                segmentStart += segmentLength;
            }

            return intervals;
        }

        // Доли отрезка start → end внутри круга: корни |offset + t × direction|² = radius² на t от 0 до 1.
        private static bool TryIntersect(
            Vector2 start,
            Vector2 end,
            Vector2 center,
            float radius,
            out float fromShare,
            out float toShare)
        {
            fromShare = 0f;
            toShare = 0f;

            Vector2 direction = end - start;
            float directionSquared = direction.sqrMagnitude;

            if (directionSquared <= 0f)
                return false;

            Vector2 offset = start - center;
            float halfLinear = Vector2.Dot(offset, direction) / directionSquared;
            float constant = (offset.sqrMagnitude - radius * radius) / directionSquared;
            float discriminant = halfLinear * halfLinear - constant;

            if (discriminant < 0f)
                return false;

            float root = Mathf.Sqrt(discriminant);

            fromShare = Mathf.Max(0f, -halfLinear - root);
            toShare = Mathf.Min(1f, -halfLinear + root);

            return fromShare < toShare;
        }

        private static void AddMerged(List<PathInterval> intervals, PathInterval interval)
        {
            if (intervals.Count > 0)
            {
                PathInterval last = intervals[intervals.Count - 1];

                if (interval.From <= last.To + MERGE_GAP)
                {
                    intervals[intervals.Count - 1] = new PathInterval(last.From, Mathf.Max(last.To, interval.To));
                    return;
                }
            }

            intervals.Add(interval);
        }

        private static Vector2 ToPlane(Vector3 point) => new(point.x, point.z);
    }
}
