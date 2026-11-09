using System;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.Features.Enemies;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Level
{
    // Открывает ворота уровня, когда на поле появляется босс: на уровне 6 вожак орков выходит из хижины вождя.
    // Дорога вожака начинается внутри хижины, за дверями, поэтому он выходит уже в открытые
    public class BossGatesService : IDisposable
    {
        private readonly Level _level;
        private readonly EntitiesLifeContext _entitiesLifeContext;

        public BossGatesService(Level level, EntitiesLifeContext entitiesLifeContext)
        {
            _level = level;
            _entitiesLifeContext = entitiesLifeContext;

            _entitiesLifeContext.Added += OnEntityAdded;
        }

        public void Dispose()
        {
            _entitiesLifeContext.Added -= OnEntityAdded;
        }

        private void OnEntityAdded(Entity entity)
        {
            if (entity.HasComponent<IsBoss>() == false)
                return;

            foreach (BossGate gate in _level.BossGates)
                gate.Open();
        }
    }
}
