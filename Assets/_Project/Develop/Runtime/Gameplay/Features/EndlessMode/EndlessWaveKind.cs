using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode
{
    // Вид врага в составе волны бесконечного режима: сколько врагов, из какого тира, по какой дороге и каким звеном.
    public class EndlessWaveKind
    {
        public EndlessWaveKind(
            MeleeConfig enemy,
            int count,
            float price,
            int tierIndex,
            EndlessRoadRoles road,
            int groupSize,
            bool spawnsLast)
        {
            Enemy = enemy;
            Count = count;
            Price = price;
            TierIndex = tierIndex;
            Road = road;
            GroupSize = groupSize;
            SpawnsLast = spawnsLast;
        }

        public MeleeConfig Enemy { get; }
        public int Count { get; }
        public float Price { get; }
        public int TierIndex { get; }
        public EndlessRoadRoles Road { get; }
        public int GroupSize { get; }
        public bool SpawnsLast { get; }
        public float Strength => Count * Price;
    }
}
