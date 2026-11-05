using System.Collections.Generic;

namespace Assets._Project.Develop.Editor.LevelEditor
{
    // Отрезок пути врага: расстояния от начала полосы.
    public readonly struct PathInterval
    {
        public PathInterval(float from, float to)
        {
            From = from;
            To = to;
        }

        public float From { get; }
        public float To { get; }
        public float Length => To - From;

        public bool Contains(float distance) => distance >= From && distance <= To;
    }

    // Покрытие полосы одним местом: отрезки пути под огнём для каждой дальности — по индексу в списке дальностей.
    public class SlotCoverage
    {
        private readonly List<PathInterval>[] _intervalsByRange;

        public SlotCoverage(int slot, List<PathInterval>[] intervalsByRange)
        {
            Slot = slot;
            _intervalsByRange = intervalsByRange;
        }

        public int Slot { get; }

        public IReadOnlyList<PathInterval> GetIntervals(int rangeIndex) => _intervalsByRange[rangeIndex];

        public float GetLength(int rangeIndex)
        {
            float length = 0f;

            foreach (PathInterval interval in _intervalsByRange[rangeIndex])
                length += interval.Length;

            return length;
        }

        public bool Contains(int rangeIndex, float distance)
        {
            foreach (PathInterval interval in _intervalsByRange[rangeIndex])
            {
                if (interval.Contains(distance))
                    return true;
            }

            return false;
        }
    }

    // Покрытие полосы дороги местами, которые достают до неё хотя бы на одной дальности.
    public class LaneCoverage
    {
        public LaneCoverage(IReadOnlyList<float> ranges)
        {
            Ranges = ranges;
        }

        public IReadOnlyList<float> Ranges { get; }
        public List<SlotCoverage> Slots { get; } = new();

        // С какого расстояния от начала полосы враг впервые под огнём; null — ни одно место не достаёт.
        public float? GetFirstCovered(int rangeIndex)
        {
            float? firstCovered = null;

            foreach (SlotCoverage slot in Slots)
            {
                IReadOnlyList<PathInterval> intervals = slot.GetIntervals(rangeIndex);

                if (intervals.Count == 0)
                    continue;

                if (firstCovered.HasValue == false || intervals[0].From < firstCovered.Value)
                    firstCovered = intervals[0].From;
            }

            return firstCovered;
        }

        public float GetTotalLength(int rangeIndex)
        {
            float totalLength = 0f;

            foreach (SlotCoverage slot in Slots)
                totalLength += slot.GetLength(rangeIndex);

            return totalLength;
        }
    }
}
