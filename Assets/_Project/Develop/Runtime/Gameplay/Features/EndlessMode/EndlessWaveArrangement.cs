using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode
{
    // Раскладка волны бесконечного режима — второй шаг генератора: потоки по дорогам и полосам, конец спавна и длина
    // волны, задержки дорог и хвосты, которые волна оставляет на дорогах для стыка со следующей.
    public class EndlessWaveArrangement
    {
        public EndlessWaveArrangement(
            List<EnemiesWaveConfig> streams,
            float spawnEnd,
            float stageTime,
            int heavyPathIndex,
            IReadOnlyList<float> pathsDelays,
            IReadOnlyList<IReadOnlyList<EndlessRoadTail>> pathsTails)
        {
            Streams = streams;
            SpawnEnd = spawnEnd;
            StageTime = stageTime;
            HeavyPathIndex = heavyPathIndex;
            PathsDelays = pathsDelays;
            PathsTails = pathsTails;
        }

        public List<EnemiesWaveConfig> Streams { get; }

        // Момент выхода последнего врага волны, с от её начала
        public float SpawnEnd { get; }

        // Конец спавна плюс передышка: через столько следующая волна приходит сама
        public float StageTime { get; }
        public int HeavyPathIndex { get; }

        // Задержка первого звена каждой дороги, с: разрыв на стыке, который не уместился в передышку прошлой волны
        public IReadOnlyList<float> PathsDelays { get; }

        // Хвосты волны по дорогам, с от её начала
        public IReadOnlyList<IReadOnlyList<EndlessRoadTail>> PathsTails { get; }
    }
}
