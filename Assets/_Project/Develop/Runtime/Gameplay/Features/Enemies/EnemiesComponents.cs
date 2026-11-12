using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Utilities.Reactive;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.Enemies
{
    public class EnemyName : IEntityComponent
    {
        public ReactiveVariable<string> Value;
    }

    public class EnemyKind : IEntityComponent
    {
        public ReactiveVariable<EnemyKinds> Value;
    }

    public class IsBoss : IEntityComponent
    {
    }

    // Один удар снимает с босса не больше 1/Value его максимального здоровья
    public class BossMaxHealthDividerPerHit : IEntityComponent
    {
        public ReactiveVariable<float> Value;
    }

    public class IsFlying : IEntityComponent
    {
    }

    // Оглушения башен на врага не действуют
    public class IsStunImmune : IEntityComponent
    {
    }
}
