using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore.Systems;
using Assets._Project.Develop.Runtime.Gameplay.Features.StatusFeature;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Features.AOE
{
    // «Морозный шторм»: в момент срабатывания на месте цели ложится ледяное поле на время заморозки. Враги в поле
    // и все, кто войдёт в него позже, заморожены до его таяния. Поле проверяется не каждый кадр, а с интервалом,
    // как зона клича вожака (WarCryZoneSystem); повторная заморозка продлевает статус, а не обрезает его
    public class StunEntitiesInAreaSecondAbilitySystem : IInitializableSystem, IUpdatableSystem, IDisposableSystem
    {
        private const float FieldCheckInterval = 0.1f;

        private readonly AreaEntitiesDetectorService _areaEntitiesDetectorService;
        private readonly StatusesFactory _statusesFactory;

        private Entity _source;

        private ReactiveEvent _secondAbilityDelayEndEvent;
        private ReactiveVariable<Entity> _currentTarget;
        private ReactiveVariable<float> _stunDuration;
        private ReactiveVariable<float> _areaRadius;

        private IDisposable _abilityDelayDisposable;

        private Vector3 _fieldCenter;
        private float _fieldTimeLeft;
        private float _timeToFieldCheck;

        public StunEntitiesInAreaSecondAbilitySystem(
            AreaEntitiesDetectorService areaEntitiesDetectorService,
            StatusesFactory statusesFactory)
        {
            _areaEntitiesDetectorService = areaEntitiesDetectorService;
            _statusesFactory = statusesFactory;
        }

        public void OnInit(Entity entity)
        {
            _source = entity;

            _secondAbilityDelayEndEvent = entity.SecondAbilityDelayEndEvent;
            _currentTarget = entity.CurrentTarget;
            _stunDuration = entity.StunDuration;
            _areaRadius = entity.AreaEffectRadius;

            _abilityDelayDisposable = _secondAbilityDelayEndEvent.Subscribe(OnAbilityDelayEnd);
        }

        public void OnUpdate(float deltaTime)
        {
            if (_fieldTimeLeft <= 0)
                return;

            _fieldTimeLeft -= deltaTime;
            _timeToFieldCheck -= deltaTime;

            if (_fieldTimeLeft <= 0 || _timeToFieldCheck > 0)
                return;

            _timeToFieldCheck = FieldCheckInterval;

            FreezeEntitiesInField();
        }

        public void OnDispose()
        {
            _abilityDelayDisposable.Dispose();
        }

        private void OnAbilityDelayEnd()
        {
            Entity target = _currentTarget.Value;

            // За время задержки цель могла пропасть: атака кончилась, и башня не нашла новую, или враг освобождён
            // (его объект из пула уже может принадлежать другой сущности)
            if (target == null || target.IsInit == false)
                return;

            _fieldCenter = target.Transform.position;
            _fieldTimeLeft = _stunDuration.Value;
            _timeToFieldCheck = FieldCheckInterval;

            FreezeEntitiesInField();
        }

        // Заморозка — до таяния поля: кто вошёл позже, стоит меньше, а размораживаются все вместе, когда исчезает круг.
        // Уже замороженные в поле получают тот же срок, и их статус не меняется
        private void FreezeEntitiesInField()
        {
            List<Entity> entities = _areaEntitiesDetectorService.GetEntitiesInArea(_fieldCenter, _areaRadius.Value);

            foreach (Entity entity in entities)
            {
                if (entity.TryGetIsStunned(out ReactiveVariable<bool> isStunned) == false)
                    continue;

                Stun(entity, _fieldTimeLeft);
            }
        }

        private void Stun(Entity entity, float duration)
        {
            Status status = _statusesFactory.CreateFor(
                entity,
                StatusesTypes.Stun,
                _source,
                duration,
                duration);

            entity.Statuses.AddElement(status);
        }
    }
}
