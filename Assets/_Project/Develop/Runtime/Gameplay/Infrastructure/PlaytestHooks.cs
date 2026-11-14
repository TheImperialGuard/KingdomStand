#if UNITY_EDITOR
using Assets._Project.Develop.Runtime.Configs.Gameplay.Abilities;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Towers;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Weather;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.Features.Projectiles;
using Assets._Project.Develop.Runtime.Infrastructure.DI;
using Assets._Project.Develop.Runtime.Meta.Features.Levels;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    // Точки, через которые плейтест из редактора уровней подключается к игре. Сессию подключает редакторская часть
    // (Editor/LevelEditor/Playtest), а игра обращается сюда только из блоков #if UNITY_EDITOR — поэтому ни этот файл,
    // ни обращения к нему в билд не попадают. Пока сессия не подключена, игра идёт как обычно, а уведомления никуда
    // не уходят.
    public static class PlaytestHooks
    {
        private static IPlaytestSession _session;

        public static bool IsPlaytestRunning => _session != null;

        public static void Connect(IPlaytestSession session) => _session = session;

        public static void Disconnect(IPlaytestSession session)
        {
            if (_session == session)
                _session = null;
        }

        public static bool TryTakeStartArgs(out GameplayInputArgs args)
        {
            args = _session?.TakeStartArgs();

            return args != null;
        }

        public static bool TryCreateLevelConfig(out LevelConfig levelConfig)
        {
            levelConfig = _session?.CreateLevelConfig();

            return levelConfig != null;
        }

        public static bool TryGetStartWaveNumber(out int waveNumber)
        {
            int? sessionWaveNumber = _session?.GetStartWaveNumber();
            waveNumber = sessionWaveNumber ?? 0;

            return sessionWaveNumber.HasValue;
        }

        // Плейтест кампании со стейджа N играет копию уровня без стейджей до N; вне плейтеста — 0.
        public static int RemovedStagesCount => _session?.GetRemovedStagesCount() ?? 0;

        public static void ProcessRegistrations(DIContainer container) => _session?.ProcessRegistrations(container);

        public static void NotifyGameEnded(LevelResults results) => _session?.OnGameEnded(results);

        public static void NotifyEnemySpawned(Entity enemy, CharacterConfig enemyConfig, EnemiesWaveConfig wave)
            => _session?.OnEnemySpawned(enemy, enemyConfig, wave);

        public static void NotifyEnemySummoned(Entity summoned, CharacterConfig summonedConfig, Entity summoner)
            => _session?.OnEnemySummoned(summoned, summonedConfig, summoner);

        public static void NotifyAreaProjectileExploded(ProjectilesTypes projectileType, int hitEnemiesCount)
            => _session?.OnAreaProjectileExploded(projectileType, hitEnemiesCount);

        public static void NotifyTowerBought(TowerTypes towerType, int level, int cost, Vector3 position)
            => _session?.OnTowerBought(towerType, level, cost, position);

        public static void NotifyTowerSold(TowerTypes towerType, int level, int gold, Vector3 position)
            => _session?.OnTowerSold(towerType, level, gold, position);

        public static void NotifyAbilityBought(TowerTypes towerType, AbilityConfig ability, int cost, Vector3 position)
            => _session?.OnAbilityBought(towerType, ability, cost, position);

        public static void NotifySkipGoldEarned(int gold) => _session?.OnSkipGoldEarned(gold);

        public static void NotifyWaveWeather(WeatherKinds weather, int waveNumber) => _session?.OnWaveWeather(weather, waveNumber);

        public static void NotifyLightningStruck(float damage, int hitEnemiesCount)
            => _session?.OnLightningStruck(damage, hitEnemiesCount);
    }
}
#endif
