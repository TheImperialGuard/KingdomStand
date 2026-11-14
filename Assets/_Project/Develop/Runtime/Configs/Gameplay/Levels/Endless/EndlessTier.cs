using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless
{
    // Тир врагов бесконечного режима: дорога, его доля в бюджете силы волны, сколько видов из него выходит в волне,
    // размер звена и враги, из которых генератор выбирает виды. Тиры в списке идут от дорогих к дешёвым: остаток бюджета
    // переходит от тира к следующему. Имя стоит первым полем — по нему Unity подписывает элементы списка тиров.
    [Serializable]
    public class EndlessTier
    {
        [SerializeField] private string _name;
        [SerializeField] private List<EndlessTierEnemy> _enemies = new();

        [field: Tooltip("Дорога тира: лёгкая, тяжёлая или эскорт по тяжёлой")]
        [field: SerializeField] public EndlessRoadRoles Road { get; private set; } = EndlessRoadRoles.Light;

        [field: Tooltip("Доля тира в бюджете силы волны. Доли тиров не обязаны давать в сумме 1: делятся пропорционально")]
        [field: SerializeField] public WaveValueTable Share { get; private set; } = new();

        [field: Tooltip("Сколько разных видов тира бывает в одной волне, округляется")]
        [field: SerializeField] public WaveValueTable KindsPerWave { get; private set; } = new();

        [field: Tooltip("Размер звена — сколько врагов вида выходит вместе, рядами по 2–3 полосы; округляется")]
        [field: SerializeField] public WaveValueTable GroupSize { get; private set; } = new();

        public string Name => _name;
        public IReadOnlyList<EndlessTierEnemy> Enemies => _enemies;
    }
}
