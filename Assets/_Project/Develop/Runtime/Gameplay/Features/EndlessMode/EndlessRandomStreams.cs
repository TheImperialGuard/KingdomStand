using System;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode
{
    // Три независимых потока случайности из зерна забега: на забег (сдвиг ролей дорог), на цикл (акценты) и на волну
    // (жребий видов). Каждый поток создаётся заново по своему номеру, поэтому волна воспроизводима при любом порядке
    // запросов, а правка одного шага не сдвигает жребий другого.
    public class EndlessRandomStreams
    {
        // Множитель смешивания зерна, соли потока и номера: близкие номера дают далёкие зёрна
        private const int MixMultiplier = 486187739;

        private const int RunSalt = 1;
        private const int CycleSalt = 2;
        private const int WaveSalt = 3;

        private const int RunIndex = 0;

        private readonly int _seed;

        public EndlessRandomStreams(int seed)
        {
            _seed = seed;
        }

        public Random CreateRunRandom() => Create(RunSalt, RunIndex);

        public Random CreateCycleRandom(int cycle) => Create(CycleSalt, cycle);

        public Random CreateWaveRandom(int waveNumber) => Create(WaveSalt, waveNumber);

        private Random Create(int salt, int index)
        {
            unchecked
            {
                int mixedSeed = (_seed * MixMultiplier + salt) * MixMultiplier + index;

                return new Random(mixedSeed);
            }
        }
    }
}
