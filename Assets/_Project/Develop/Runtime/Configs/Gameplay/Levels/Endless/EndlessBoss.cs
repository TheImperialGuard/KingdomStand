using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless
{
    // Босс бесконечного режима и его сопровождение. Сопровождение идёт по дороге босса и в правила разрыва на стыке
    // волн не входит.
    [Serializable]
    public class EndlessBoss
    {
        [SerializeField] private List<EndlessEscortGroup> _escort = new();

        [field: SerializeField] public MeleeConfig Boss { get; private set; }

        public IReadOnlyList<EndlessEscortGroup> Escort => _escort;
    }
}
