using System.Collections.Generic;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Utilities.SimpleMono
{
    public class ParticleEffectTimeScaler : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _targetDuration = 1f;
        [SerializeField] private float _additionalTime = 0f;

        private ParticleSystem _rootSystem;
        private ParticleSystem[] _systems;

        private Dictionary<ParticleSystem, float> _baseSimulationSpeeds = new();

        private void Awake()
        {
            CacheParticleSystems();
        }

        private void OnEnable()
        {
            if (_systems == null || _systems.Length == 0)
                CacheParticleSystems();

            SetDuration(_targetDuration);
        }

        public void SetDuration(float newTargetDuration)
        {
            _targetDuration = Mathf.Max(0.01f, newTargetDuration);

            float originalDuration = CalculateEffectDuration();

            if (originalDuration <= 0f)
            {
                Debug.LogWarning(
                    $"[{name}] Не удалось определить длительность particle effect.",
                    this);

                return;
            }

            float scale = originalDuration / (_targetDuration + _additionalTime);
            SetTimeScale(scale);
        }

        public void SetTimeScale(float scale)
        {
            scale = Mathf.Max(0.001f, scale);

            foreach (ParticleSystem system in _systems)
            {
                if (system == null)
                    continue;

                var main = system.main;
                main.simulationSpeed = _baseSimulationSpeeds[system] * scale;
            }
        }

        public void ResetTimeScale()
        {
            foreach (ParticleSystem system in _systems)
            {
                if (system == null)
                    continue;

                var main = system.main;
                main.simulationSpeed = _baseSimulationSpeeds[system];
            }
        }

        public void PlayForDuration(float duration)
        {
            SetDuration(duration);

            ParticleSystem rootSystem = GetComponent<ParticleSystem>();
            rootSystem.Play(withChildren: true);
        }

        private void CacheParticleSystems()
        {
            _systems = GetComponentsInChildren<ParticleSystem>(true);

            _baseSimulationSpeeds.Clear();

            foreach (ParticleSystem system in _systems)
            {
                _baseSimulationSpeeds[system] = system.main.simulationSpeed;
            }
        }

        private float CalculateEffectDuration()
        {
            float longestDuration = 0f;

            foreach (ParticleSystem system in _systems)
            {
                if (system == null)
                    continue;

                var main = system.main;

                if (main.loop)
                {
                    Debug.LogWarning(
                        $"[{system.name}] Включён Looping: у такого эффекта нет конечной длительности.",
                        system);

                    continue;
                }

                float particleLifetime = GetMaxValue(main.startLifetime);

                float systemDuration =
                    main.startDelay.constantMax +
                    GetEmissionEnd(system) +
                    particleLifetime;

                longestDuration = Mathf.Max(longestDuration, systemDuration);
            }

            return longestDuration;
        }

        // Непрерывный выпуск идёт весь цикл системы, а у системы из одних вспышек последние частицы появляются
        // с последней вспышкой: вспышка в начале цикла живёт время жизни частиц, а не цикл плюс время жизни
        private static float GetEmissionEnd(ParticleSystem system)
        {
            ParticleSystem.MainModule main = system.main;
            ParticleSystem.EmissionModule emission = system.emission;

            if (GetMaxValue(emission.rateOverTime) > 0 || GetMaxValue(emission.rateOverDistance) > 0)
                return main.duration;

            float lastBurstTime = 0f;

            for (int i = 0; i < emission.burstCount; i++)
            {
                ParticleSystem.Burst burst = emission.GetBurst(i);

                float burstEnd = burst.cycleCount == 0
                    ? main.duration
                    : burst.time + (burst.cycleCount - 1) * burst.repeatInterval;

                lastBurstTime = Mathf.Max(lastBurstTime, Mathf.Min(burstEnd, main.duration));
            }

            return lastBurstTime;
        }

        private static float GetMaxValue(ParticleSystem.MinMaxCurve curve)
        {
            return curve.mode switch
            {
                ParticleSystemCurveMode.Constant =>
                    curve.constant,

                ParticleSystemCurveMode.TwoConstants =>
                    curve.constantMax,

                ParticleSystemCurveMode.Curve =>
                    curve.curveMultiplier,

                ParticleSystemCurveMode.TwoCurves =>
                    curve.curveMultiplier,

                _ => curve.constantMax
            };
        }
    }
}
