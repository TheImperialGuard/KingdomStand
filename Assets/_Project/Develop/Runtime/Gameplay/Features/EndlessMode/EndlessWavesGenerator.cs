using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless;
using Assets._Project.Develop.Runtime.Gameplay.Features.LevelNavigation;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode
{
    // Собирает волну бесконечного режима по её номеру — обычный EnemiesWavesStageConfig, который играет конвейер стейджей.
    // Волна собирается в два шага: состав (EndlessWaveComposer — бюджет силы, тиры, жребий видов) и раскладка
    // (EndlessWaveArranger — роли дорог, звенья, окно выхода, потоки). Роли дорог и акценты дают циклы между боссами
    // (EndlessCycles), разрывы на стыке волн — EndlessWaveJoints.
    //
    // Случайность берётся из зерна забега, поэтому волна N одна и та же у одного зерна. Раскладка волны зависит от хвостов
    // прошлых: генератор раскладывает волны по порядку от первой и запоминает их. Момент вызова волны N зависит от головы
    // волны N + 1, поэтому, отдавая волну N, генератор раскладывает и следующую. Таблица волн в инспекторе и срез строят
    // волны тем же генератором по зерну-образцу конфига.
    public class EndlessWavesGenerator
    {
        public const int BossesInWave = 1;
        public const int FirstWaveNumber = 1;

        private readonly List<PathNumber> _pathNumbers = new();

        private readonly EndlessCycles _cycles;
        private readonly EndlessWaveComposer _composer;
        private readonly EndlessWaveArranger _arranger;
        private readonly EndlessWaveJoints _joints;

        // Разложенные волны по порядку, с первой; момент вызова известен для всех, кроме последней
        private readonly List<EndlessWaveComposition> _compositions = new();
        private readonly List<EndlessWaveArrangement> _arrangements = new();
        private readonly List<float> _timesToSkip = new();

        private readonly Dictionary<int, EnemiesWavesStageConfig> _stages = new();

        public EndlessWavesGenerator(EndlessModeConfig config, IReadOnlyList<RoadPath> roadPaths, int seed)
        {
            foreach (RoadPath roadPath in roadPaths)
            {
                if (_pathNumbers.Contains(roadPath.Number) == false)
                    _pathNumbers.Add(roadPath.Number);
            }

            if (_pathNumbers.Count == 0)
                throw new ArgumentException("Для волн бесконечного режима на уровне нужна хотя бы одна дорога");

            Seed = seed;

            EndlessRandomStreams randomStreams = new(seed);

            _cycles = new EndlessCycles(config, _pathNumbers.Count, randomStreams);
            _composer = new EndlessWaveComposer(config, _cycles, randomStreams);
            _arranger = new EndlessWaveArranger(config, _pathNumbers);
            _joints = new EndlessWaveJoints(config.SafeCatchUpDistance, _pathNumbers.Count);
        }

        public int Seed { get; }

        // Номера всех дорог уровня
        public IReadOnlyList<PathNumber> PathNumbers => _pathNumbers;

        public EnemiesWavesStageConfig Generate(int waveNumber)
        {
            ValidateWaveNumber(waveNumber);

            if (_stages.TryGetValue(waveNumber, out EnemiesWavesStageConfig cachedStage))
                return cachedStage;

            ArrangeUpTo(waveNumber + 1);

            EndlessWaveArrangement arrangement = _arrangements[waveNumber - FirstWaveNumber];
            EnemiesWavesStageConfig stage = new(arrangement.Streams, arrangement.StageTime, _timesToSkip[waveNumber - FirstWaveNumber]);

            _stages.Add(waveNumber, stage);

            return stage;
        }

        // Босс волны, если она боссовая: боссы идут по кругу, начиная с первого в списке
        public bool TryGetBoss(int waveNumber, out EndlessBoss boss) => _composer.TryGetBoss(waveNumber, out boss);

        // Сводка волны для инструментов редактора
        public EndlessWavePlan GetPlan(int waveNumber)
        {
            ValidateWaveNumber(waveNumber);
            ArrangeUpTo(waveNumber + 1);

            int index = waveNumber - FirstWaveNumber;
            EndlessWaveArrangement arrangement = _arrangements[index];

            return new EndlessWavePlan(_compositions[index], arrangement, _pathNumbers[arrangement.HeavyPathIndex], _timesToSkip[index]);
        }

        private static void ValidateWaveNumber(int waveNumber)
        {
            if (waveNumber < FirstWaveNumber)
                throw new ArgumentOutOfRangeException(nameof(waveNumber), waveNumber, "Волны нумеруются с единицы");
        }

        private void ArrangeUpTo(int wavesCount)
        {
            while (_arrangements.Count < wavesCount)
                ArrangeNextWave();
        }

        // Безопасные старты голов новой волны задают момент вызова прошлой — не раньше конца её спавна и не позже её
        // конца — и задержки дорог: разрыв, который не уместился в передышку
        private void ArrangeNextWave()
        {
            int waveNumber = FirstWaveNumber + _arrangements.Count;
            EndlessWaveComposition composition = _composer.Compose(waveNumber);
            int heavyPathIndex = _cycles.GetHeavyPathIndex(composition.Cycle);
            float[] headSpeeds = _arranger.GetHeadSpeeds(composition, heavyPathIndex);
            float[] pathsDelays = new float[_pathNumbers.Count];

            if (_arrangements.Count > 0)
            {
                EndlessWaveArrangement previous = _arrangements[_arrangements.Count - 1];
                float latestSafeStart = 0f;

                for (int i = 0; i < pathsDelays.Length; i++)
                {
                    float safeStart = _joints.GetSafeStart(i, headSpeeds[i]);

                    latestSafeStart = Mathf.Max(latestSafeStart, safeStart);
                    pathsDelays[i] = Mathf.Max(0f, safeStart - previous.StageTime);
                }

                float timeToSkip = Mathf.Min(previous.StageTime, Mathf.Max(previous.SpawnEnd, latestSafeStart));

                _timesToSkip.Add(timeToSkip);
                _joints.MoveToNextWave(timeToSkip);
            }

            EndlessWaveArrangement arrangement = _arranger.Arrange(composition, heavyPathIndex, pathsDelays);

            _joints.AddTails(arrangement.PathsTails);
            _compositions.Add(composition);
            _arrangements.Add(arrangement);
        }
    }
}
