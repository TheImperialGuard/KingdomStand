using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Tutorial
{
    // «Королевский взор» — подсказка об управлении камерой. Id — ключ в сохранениях игрока, у готовой строки не меняется.
    [Serializable]
    public class TutorialCameraHintInfo
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField, TextArea] public string Text { get; private set; }
        [field: SerializeField] public string MoveCaption { get; private set; }
        [field: SerializeField] public string ZoomCaption { get; private set; }
    }
}
