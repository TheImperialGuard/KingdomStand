using System.Collections.Generic;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode
{
    // Разрывы на стыке волн. Помнит хвосты дорог — врагов прошлых волн, которых быстрый враг новой волны мог бы догнать
    // ближе безопасного расстояния S от спавна, — и по ним считает безопасный старт головы новой волны на каждой дороге:
    // голова со скоростью u догоняет хвост со скоростью v на расстоянии S, если выходит через S × (1/v − 1/u) после него.
    // Хвосты записаны от начала последней разложенной волны; при переходе к следующей сдвигаются на самый ранний её старт
    // — момент, с которого её можно вызвать, — а хвосты, ушедшие от спавна дальше S, отбрасываются: их уже не догнать
    // ближе S.
    public class EndlessWaveJoints
    {
        // Голова без ограничений выходит с начала волны
        private const float NoGapStart = 0f;

        private readonly float _safeCatchUpDistance;
        private readonly List<EndlessRoadTail>[] _pathsTails;

        public EndlessWaveJoints(float safeCatchUpDistance, int pathsCount)
        {
            _safeCatchUpDistance = safeCatchUpDistance;
            _pathsTails = new List<EndlessRoadTail>[pathsCount];

            for (int i = 0; i < pathsCount; i++)
                _pathsTails[i] = new List<EndlessRoadTail>();
        }

        // Безопасный старт головы дороги, с от начала последней разложенной волны
        public float GetSafeStart(int pathIndex, float headSpeed)
        {
            float safeStart = NoGapStart;

            foreach (EndlessRoadTail tail in _pathsTails[pathIndex])
            {
                if (headSpeed <= tail.Speed)
                    continue;

                float gap = _safeCatchUpDistance * (1f / tail.Speed - 1f / headSpeed);

                if (tail.ExitTime + gap > safeStart)
                    safeStart = tail.ExitTime + gap;
            }

            return safeStart;
        }

        // Переход к следующей волне: хвосты записываются от её самого раннего старта
        public void MoveToNextWave(float nextWaveEarliestStart)
        {
            for (int i = 0; i < _pathsTails.Length; i++)
            {
                List<EndlessRoadTail> shiftedTails = new();

                foreach (EndlessRoadTail tail in _pathsTails[i])
                {
                    EndlessRoadTail shiftedTail = new(tail.Speed, tail.ExitTime - nextWaveEarliestStart);

                    if (IsCatchable(shiftedTail))
                        shiftedTails.Add(shiftedTail);
                }

                _pathsTails[i] = shiftedTails;
            }
        }

        public void AddTails(IReadOnlyList<IReadOnlyList<EndlessRoadTail>> pathsTails)
        {
            for (int i = 0; i < _pathsTails.Length; i++)
            {
                foreach (EndlessRoadTail tail in pathsTails[i])
                {
                    if (tail.Speed > 0f)
                        _pathsTails[i].Add(tail);
                }
            }
        }

        // Хвост, который к началу волны ещё ближе S к спавну
        private bool IsCatchable(EndlessRoadTail tail) => -tail.ExitTime * tail.Speed < _safeCatchUpDistance;
    }
}
