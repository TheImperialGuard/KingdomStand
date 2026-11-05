using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Gameplay.Features.Enemies;
using Assets._Project.Develop.Runtime.Gameplay.Features.Projectiles;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
#endif

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AOE
{
    public class DealAreaDamageOnDeathSystem : IInitializableSystem, IDisposableSystem
    {
        private readonly AreaEntitiesDetectorService _areaEntitiesDetectorService;

        #if UNITY_EDITOR
            private readonly ProjectilesTypes _projectileType;
        #endif

        private ReactiveVariable<float> _damage;
        private ReactiveVariable<float> _areaRadius;

        private Entity _source;
        private Transform _deathPoint;

        private ReactiveVariable<bool> _isDead;

        private IDisposable _isDeadDisposable;

        // projectileType нужен только логу плейтеста: ядро и осколок он считает отдельно
        public DealAreaDamageOnDeathSystem(AreaEntitiesDetectorService areaEntitiesDetectorService, ProjectilesTypes projectileType)
        {
            _areaEntitiesDetectorService = areaEntitiesDetectorService;

            #if UNITY_EDITOR
                _projectileType = projectileType;
            #endif
        }

        public void OnInit(Entity entity)
        {
            _damage = entity.InstantAttackDamage;
            _areaRadius = entity.AreaEffectRadius;

            _source = entity;
            _deathPoint = entity.Transform;

            _isDead = entity.IsDead;

            _isDeadDisposable = _isDead.Subscribe(OnIsDeadChanged);
        }

        public void OnDispose()
        {
            _isDeadDisposable.Dispose();
        }

        private static bool IsAlive(Entity entity)
            => entity.TryGetIsDead(out ReactiveVariable<bool> isDead) == false || isDead.Value == false;

        private void OnIsDeadChanged(bool arg1, bool isDead)
        {
            if (isDead == true)
                DealAreaDamage();
        }

        private void DealAreaDamage()
        {
            List<Entity> entities = _areaEntitiesDetectorService.GetEntitiesInArea(
                _deathPoint.position,
                _areaRadius.Value);

            // Задетые — живые до удара: урон по умирающему тоже уходит, но попаданием не считается
            int hitEnemiesCount = 0;

            foreach (Entity entityInArea in entities)
            {
                // Снаряд пушки взрывается на земле: летающих не задевает, как и не берёт в цель
                if (entityInArea.HasComponent<IsFlying>())
                    continue;

                bool wasAlive = IsAlive(entityInArea);

                if (EntitiesHelper.TryTakeDamageFrom(_source, entityInArea, _damage.Value) && wasAlive)
                    hitEnemiesCount++;
            }

            #if UNITY_EDITOR
                PlaytestHooks.NotifyAreaProjectileExploded(_projectileType, hitEnemiesCount);
            #endif
        }
    }
}
