using System;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless
{
    // Сопровождение босса: группы одного врага, которые выходят по дороге босса по расписанию — первая через задержку
    // от начала волны, каждая следующая через период.
    [Serializable]
    public class EndlessEscortGroup
    {
        [field: SerializeField] public MeleeConfig Enemy { get; private set; }

        [field: Tooltip("Врагов в группе; группа выходит рядами по 2–3 полосы")]
        [field: SerializeField, Min(1)] public int GroupSize { get; private set; } = 1;

        [field: Tooltip("Задержка первой группы от начала волны, с")]
        [field: SerializeField, Min(0)] public float FirstDelay { get; private set; }

        [field: Tooltip("Время между группами, с")]
        [field: SerializeField, Min(0)] public float Period { get; private set; }

        [field: SerializeField, Min(1)] public int GroupsCount { get; private set; } = 1;
    }
}
