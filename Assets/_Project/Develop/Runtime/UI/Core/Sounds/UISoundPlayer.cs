using System;
using Assets._Project.Develop.Runtime.Utilities.Audio;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets._Project.Develop.Runtime.UI.Core.Sounds
{
    // Звуки интерфейса. Живёт всю сессию и играет через PlayOneShot, чтобы звук доигрывал, когда кнопка закрывает своё окно
    // или уводит на другую сцену. Высота тона — случайная в пределах звука, как у выстрелов башен (SoundEffectViewBase),
    // чтобы повторы звучали по-разному. У каждого звука свой источник: высота тона задаётся источнику целиком и сбила бы тон
    // другого звука, который ещё доигрывает. Выход источников — GeneralMixer/Sounds, под громкость из настроек.
    public class UISoundPlayer : MonoBehaviour
    {
        [SerializeField] private UISound _buttonClick;
        [SerializeField] private UISound _pageTurn;
        [SerializeField] private UISound _goldGain;

        public void PlayButtonClick() => _buttonClick.Play();

        public void PlayPageTurn() => _pageTurn.Play();

        public void PlayGoldGain() => _goldGain.Play();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnValidate()
        {
            _buttonClick?.Validate();
            _pageTurn?.Validate();
            _goldGain?.Validate();
        }

        [Serializable]
        private class UISound
        {
            private const float MinPitch = 0.5f;
            private const float MaxPitch = 2f;

            [SerializeField] private AudioSource _audioSource;
            [SerializeField] private AudioClip _clip;
            [SerializeField, Range(0f, 1f)] private float _volume = 1f;
            [SerializeField, Range(MinPitch, MaxPitch)] private float _pitchLowerLimit = 0.95f;
            [SerializeField, Range(MinPitch, MaxPitch)] private float _pitchUpperLimit = 1.05f;

            public void Play()
            {
                if (SoundRetriggerLimiter.TryStart(_clip) == false)
                    return;

                _audioSource.pitch = Random.Range(_pitchLowerLimit, _pitchUpperLimit);
                _audioSource.PlayOneShot(_clip, _volume);
            }

            public void Validate()
            {
                if (_pitchLowerLimit > _pitchUpperLimit)
                    _pitchUpperLimit = _pitchLowerLimit;
            }
        }
    }
}
