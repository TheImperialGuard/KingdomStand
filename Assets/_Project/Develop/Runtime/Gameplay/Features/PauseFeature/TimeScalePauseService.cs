using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.PauseFeature
{
    // Пауза боя через масштаб времени. Паузы вкладываются (окно обучения → меню паузы или книга поверх): каждый Pause
    // снимается своим Unpause, и бой идёт снова, когда снята последняя. При выгрузке сцены время возвращается к норме,
    // чтобы незакрытая пауза не перешла в следующую сцену
    public class TimeScalePauseService : IPauseService, IDisposable
    {
        private const float PausedTimeScale = 0f;
        private const float NormalTimeScale = 1f;

        private int _pausesCount;

        public bool IsPaused => _pausesCount > 0;

        public void Pause()
        {
            _pausesCount++;

            Time.timeScale = PausedTimeScale;
        }

        public void Unpause()
        {
            if (_pausesCount == 0)
            {
                Debug.LogWarning("Снятие паузы, которая не ставилась");
                return;
            }

            _pausesCount--;

            if (_pausesCount == 0)
                Time.timeScale = NormalTimeScale;
        }

        public void Dispose()
        {
            _pausesCount = 0;

            Time.timeScale = NormalTimeScale;
        }
    }
}
