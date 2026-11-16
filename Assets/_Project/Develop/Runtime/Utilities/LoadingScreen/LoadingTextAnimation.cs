using TMPro;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Utilities.LoadingScreen
{
    public class LoadingTextAnimation : MonoBehaviour
    {
        private const char Dot = '.';
        private const int AllCharacters = int.MaxValue;

        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _dotInterval = 0.4f;

        private float _startTime;
        private int _characterCount;
        private int _dotsCount;

        private void OnEnable()
        {
            _startTime = Time.unscaledTime;

            _text.maxVisibleCharacters = AllCharacters;
            _text.ForceMeshUpdate();

            _characterCount = _text.textInfo.characterCount;
            _dotsCount = CountTrailingDots();
        }

        private void OnDisable() => _text.maxVisibleCharacters = AllCharacters;

        private void Update()
        {
            float elapsed = Time.unscaledTime - _startTime;
            int shownDots = (int)(elapsed / _dotInterval) % (_dotsCount + 1);

            _text.maxVisibleCharacters = _characterCount - _dotsCount + shownDots;
        }

        private int CountTrailingDots()
        {
            TMP_CharacterInfo[] characters = _text.textInfo.characterInfo;
            int count = 0;

            while (count < _characterCount && characters[_characterCount - 1 - count].character == Dot)
                count++;

            return count;
        }
    }
}
