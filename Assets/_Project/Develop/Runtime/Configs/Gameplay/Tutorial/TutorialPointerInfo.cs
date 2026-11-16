using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial
{
    // Указатель обучения. Id — ключ в сохранениях игрока, у готовой строки не меняется. Второй текст и сторона — для
    // продолжения указателя: у медальонов волны — текст после первого нажатия, у улучшения — указатель на кнопку в
    // открывшемся окне башни.
    [Serializable]
    public class TutorialPointerInfo
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField, TextArea] public string Text { get; private set; }
        [field: SerializeField] public TutorialPointerSides Side { get; private set; }
        [field: SerializeField, TextArea] public string SecondText { get; private set; }
        [field: SerializeField] public TutorialPointerSides SecondSide { get; private set; }
    }
}
