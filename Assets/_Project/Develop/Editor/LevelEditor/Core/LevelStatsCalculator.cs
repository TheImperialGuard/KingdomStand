using System;
using System.Collections.Generic;
using System.Linq;
using Assets._Project.Develop.Runtime.Configs.Gameplay;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Abilities.Enemies;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Towers;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Stages;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Assets._Project.Develop.Runtime.Gameplay.Features.LevelNavigation;

namespace Assets._Project.Develop.Editor.LevelEditor
{
    // Статистика уровня по LevelConfig и дорогам префаба. Формулы повторяют рантайм: порядок спавна — WavesSpawner,
    // сопротивление урону — EntitiesHelper, золото за скип — EarnGoldOnSkipStageService.
    // Время в пути и пик врагов на поле — оценки без убийств: каждый враг доходит до базы, кастующие останавливаются
    // на каждый каст (EnemyAbilitiesFactory). Призванные в пик и здоровье не входят — для них верхняя граница призывов.
    // Покрытие дорог местами под башни — RoadCoverageCalculator; по нему же касты до зоны башен и время боссов под огнём.
    public static class LevelStatsCalculator
    {
        private const int FirstCastNumber = 1;

        private static readonly IReadOnlyList<EnemiesWaveConfig> _noWaves = Array.Empty<EnemiesWaveConfig>();
        private static readonly IReadOnlyList<WaveEnemyGroup> _noGroups = Array.Empty<WaveEnemyGroup>();

        public static LevelStats Calculate(LevelConfig config, LevelPrefabInfo prefab, GameConfig gameConfig)
            => Calculate(config, config.Stages, hasLastStage: true, prefab, gameConfig);

        // Стейджи не из конфига — например, волны бесконечного режима, собранные генератором; золото на старте и здоровье
        // игрока — из конфига. Без последнего стейджа скип есть у каждого: за посчитанными стейджами идут следующие.
        public static LevelStats Calculate(
            LevelConfig config,
            IReadOnlyList<EnemiesWavesStageConfig> stages,
            bool hasLastStage,
            LevelPrefabInfo prefab,
            GameConfig gameConfig)
        {
            LevelStats levelStats = new();
            List<List<EnemySpawn>> stagesSpawns = new();
            LevelCoverage coverage = new(prefab, RoadCoverageCalculator.CollectTowerRanges());

            for (int i = 0; i < stages.Count; i++)
            {
                bool isLastStage = hasLastStage && i == stages.Count - 1;
                List<EnemySpawn> stageSpawns = new();

                levelStats.Stages.Add(CalculateStage(stages[i], isLastStage, config, prefab, coverage, gameConfig, stageSpawns));
                stagesSpawns.Add(stageSpawns);
            }

            SumStages(config, levelStats);
            CalculateDurations(stages, levelStats);
            CalculatePeaks(stages, stagesSpawns, levelStats);

            levelStats.CoverageRanges = coverage.Ranges.ToList();
            levelStats.Roads = CalculateRoads(stages, prefab, coverage);

            return levelStats;
        }

        // Как EntitiesHelper.CalculateFinalDamage: сопротивление режет урон только своего типа.
        public static float CalculateEffectiveHealth(MeleeConfig enemy, DamageTypes damageType)
        {
            if (enemy.DamageResistanceType == DamageTypes.None || enemy.DamageResistanceType != damageType)
                return enemy.MaxHealth;

            float damageFactor = 1f - enemy.DamageResistanceIndex;

            if (damageFactor <= 0f)
                return float.PositiveInfinity;

            return enemy.MaxHealth / damageFactor;
        }

        // Как EarnGoldOnSkipStageService: золото за оставшееся время, если скипнуть, как только разрешено.
        // В последнем стейдже скип не предлагается.
        public static int CalculateMaxSkipGold(EnemiesWavesStageConfig stage, bool isLastStage, GameConfig gameConfig)
        {
            if (isLastStage || gameConfig == null)
                return 0;

            float remainingTime = stage.StageTime - stage.TimeToSkipStage;

            if (remainingTime <= 0f)
                return 0;

            float earnedGold = remainingTime * gameConfig.GoldIndexForRemainingStageSeconds;
            int gold = (int)Math.Round(earnedGold, MidpointRounding.AwayFromZero);

            return Math.Min(gold, gameConfig.MaxGoldForSkipStage);
        }

        public static int CalculateKillGold(EnemiesWaveConfig wave) => SumStat(wave, enemy => enemy.GoldOnDeath);

        public static int CountEnemies(EnemiesWaveConfig wave)
        {
            int patternCount = GetGroups(wave).Sum(group => Math.Max(0, group.Count));

            return Math.Max(0, wave.PatternRepeats) * patternCount;
        }

        // Как WavesSpawner: первый враг выходит через StartDelay, каждый следующий — через DelayBetweenSpawns.
        public static float CalculateLastSpawnTime(EnemiesWaveConfig wave)
        {
            int enemiesCount = CountEnemies(wave);

            if (enemiesCount == 0)
                return wave.StartDelay;

            return wave.StartDelay + (enemiesCount - 1) * wave.DelayBetweenSpawns;
        }

        // Порядок спавна как в WavesSpawner: повторы шаблона × группы × количество в группе;
        // первый враг выходит через StartDelay, каждый следующий — через DelayBetweenSpawns.
        public static IEnumerable<WaveSpawn> EnumerateSpawns(EnemiesWaveConfig wave)
        {
            int repeats = Math.Max(0, wave.PatternRepeats);
            int spawnIndex = 0;

            for (int repeat = 0; repeat < repeats; repeat++)
            {
                foreach (WaveEnemyGroup group in GetGroups(wave))
                {
                    int count = Math.Max(0, group.Count);

                    for (int i = 0; i < count; i++)
                    {
                        float spawnTime = wave.StartDelay + spawnIndex * wave.DelayBetweenSpawns;

                        yield return new WaveSpawn(spawnTime, group.EnemyConfig);
                        spawnIndex++;
                    }
                }
            }
        }

        // Как TowersFactory: пушка выбирает цели селектором наземных врагов, остальные башни бьют и летающих.
        public static bool CanHitFlying(TowerTypes towerType) => towerType != TowerTypes.Cannon;

        // Как EnemyAbilitiesFactory.AddAbilityTo: вид способности задаёт класс конфига.
        public static EnemyAbilityTypes GetAbilityType(EnemyAbilityConfig ability)
        {
            return ability switch
            {
                SummonAbilityConfig => EnemyAbilityTypes.Summon,
                WarCryAbilityConfig => EnemyAbilityTypes.WarCry,
                SnowballThrowAbilityConfig => EnemyAbilityTypes.SnowballThrow,
                _ => throw new ArgumentException($"Not supported {ability.GetType()} enemy ability config"),
            };
        }

        public static IReadOnlyList<EnemiesWaveConfig> GetWaves(EnemiesWavesStageConfig stage)
            => stage.EnemiesWaveConfigs ?? _noWaves;

        public static IReadOnlyList<WaveEnemyGroup> GetGroups(EnemiesWaveConfig wave)
            => wave.EnemyGroups ?? _noGroups;

        private static StageStats CalculateStage(
            EnemiesWavesStageConfig stage,
            bool isLastStage,
            LevelConfig config,
            LevelPrefabInfo prefab,
            LevelCoverage coverage,
            GameConfig gameConfig,
            List<EnemySpawn> stageSpawns)
        {
            StageStats stageStats = new();
            List<RoadTrafficStats> wavesTraffic = new();

            foreach (EnemiesWaveConfig wave in GetWaves(stage))
            {
                LevelRoad road = FindRoad(prefab, wave.PathNumber);
                float? length = GetLength(road, wave.Lane);
                coverage.TryGet(road, wave.Lane, out LaneCoverage laneCoverage);

                List<EnemySpawn> waveSpawns = CollectSpawns(wave, length);
                WaveStats waveStats = CalculateWave(wave, waveSpawns, length, laneCoverage);

                stageStats.Waves.Add(waveStats);
                stageSpawns.AddRange(waveSpawns);

                stageStats.EnemiesCount += waveStats.EnemiesCount;
                CountEnemiesByType(wave, stageStats.EnemiesByType);
                stageStats.Health += waveStats.Health;
                stageStats.EffectiveHealthPhysic += SumStat(wave, enemy => CalculateEffectiveHealth(enemy, DamageTypes.Physic));
                stageStats.EffectiveHealthMagic += SumStat(wave, enemy => CalculateEffectiveHealth(enemy, DamageTypes.Magic));
                stageStats.KillGold += waveStats.KillGold;
                stageStats.BreakthroughDamage += waveStats.BreakthroughDamage;
                stageStats.SpawnEnd = Math.Max(stageStats.SpawnEnd, waveStats.SpawnEnd);
                stageStats.FlyingHealth = AddOptional(stageStats.FlyingHealth, waveStats.FlyingHealth);
                AddSummoned(waveStats, stageStats);

                wavesTraffic.Add(new RoadTrafficStats
                {
                    PathNumber = wave.PathNumber,
                    Lane = wave.Lane,
                    WavesCount = 1,
                    EnemiesCount = waveStats.EnemiesCount,
                    Health = waveStats.Health,
                });
            }

            stageStats.WavesCount = stageStats.Waves.Count;
            stageStats.HealthPerSecond = stage.StageTime > 0f ? stageStats.Health / stage.StageTime : 0f;
            stageStats.MaxSkipGold = CalculateMaxSkipGold(stage, isLastStage, gameConfig);
            stageStats.BreakthroughThreat = CalculateThreat(stageStats.BreakthroughDamage, config.StartPlayerHealth);
            stageStats.SpawnOverflow = Math.Max(0f, stageStats.SpawnEnd - stage.StageTime);
            stageStats.LastArrival = CalculateLastArrival(stageSpawns);
            stageStats.Traffic = MergeTraffic(wavesTraffic);

            return stageStats;
        }

        // Покрытие полосы волны — null, если его нет: тогда нет ни кастов до зоны башен, ни времени боссов под огнём.
        private static WaveStats CalculateWave(
            EnemiesWaveConfig wave,
            IReadOnlyList<EnemySpawn> spawns,
            float? length,
            LaneCoverage laneCoverage)
        {
            float flyingHealth = SumStat(wave, enemy => enemy.IsFlying ? enemy.MaxHealth : 0f);
            List<WaveAbilityStats> abilities = CalculateAbilities(wave, length, laneCoverage);
            List<EnemyUnderFireStats> underFire = CalculateUnderFire(wave, length, laneCoverage);

            return new WaveStats
            {
                EnemiesCount = spawns.Count,
                Health = SumStat(wave, enemy => enemy.MaxHealth),
                KillGold = CalculateKillGold(wave),
                BreakthroughDamage = SumStat(wave, enemy => enemy.DamageOnFinishPath),
                SpawnEnd = CalculateLastSpawnTime(wave),
                FirstArrival = CalculateFirstArrival(spawns),
                LastArrival = CalculateLastArrival(spawns),
                FlyingHealth = flyingHealth > 0f ? flyingHealth : null,
                Abilities = abilities.Count > 0 ? abilities : null,
                UnderFire = underFire.Count > 0 ? underFire : null,
            };
        }

        private static List<EnemySpawn> CollectSpawns(EnemiesWaveConfig wave, float? length)
        {
            return EnumerateSpawns(wave)
                .Select(spawn => new EnemySpawn(spawn.Time, CalculateTravelTime(spawn.Enemy, length)))
                .ToList();
        }

        // Подъём после появления, ход по дороге и остановки на каст: враг стоит, пока кастует.
        private static float? CalculateTravelTime(CharacterConfig enemyConfig, float? length)
        {
            if (TryGetWalkTime(enemyConfig, length, out MeleeConfig enemy, out float walkTime) == false)
                return null;

            return enemy.SpawnProcessTime + walkTime + CalculateCastDelay(enemy, walkTime);
        }

        private static bool TryGetWalkTime(CharacterConfig enemyConfig, float? length, out MeleeConfig enemy, out float walkTime)
        {
            walkTime = 0f;

            if (TryGetMelee(enemyConfig, out enemy) == false || length.HasValue == false || enemy.MoveSpeed <= 0f)
                return false;

            walkTime = length.Value / enemy.MoveSpeed;

            return true;
        }

        private static float CalculateCastDelay(MeleeConfig enemy, float walkTime)
        {
            int castsCount = CountCastsToBase(enemy, walkTime);

            return castsCount > 0 ? castsCount * enemy.Ability.Duration : 0f;
        }

        // Касты, которые враг начнёт, пока идёт до базы. Без перезарядки цикл каста не оценить — считаем, что кастов нет.
        private static int CountCastsToBase(MeleeConfig enemy, float walkTime)
        {
            if (enemy.Ability == null || enemy.Ability.Cooldown <= 0f)
                return 0;

            int castsCount = 0;

            while (CalculateWalkTimeBeforeCast(enemy, castsCount + 1) < walkTime)
                castsCount++;

            return castsCount;
        }

        // Как EnemyAbilitiesFactory.AddCastCycleTo: перезарядка тикает с появления и после каждого каста, поэтому каст
        // с номером castNumber (с единицы) начинается через castNumber × перезарядка + (castNumber − 1) × каст после
        // появления. Из этого времени враг стоял на подъёме и на прошлых кастах, остальное — шёл.
        private static float CalculateWalkTimeBeforeCast(MeleeConfig enemy, int castNumber)
            => Math.Max(0f, castNumber * enemy.Ability.Cooldown - enemy.SpawnProcessTime);

        private static float CalculateCastStartTime(MeleeConfig enemy, int castNumber)
            => castNumber * enemy.Ability.Cooldown + (castNumber - 1) * enemy.Ability.Duration;

        // Путь от начала дороги, на котором враг встанет на каст.
        private static float CalculateCastDistance(MeleeConfig enemy, int castNumber)
            => CalculateWalkTimeBeforeCast(enemy, castNumber) * enemy.MoveSpeed;

        // Касты по типам кастующих врагов волны, в порядке первого выхода. Без длины дороги касты не оценить.
        private static List<WaveAbilityStats> CalculateAbilities(EnemiesWaveConfig wave, float? length, LaneCoverage laneCoverage)
        {
            List<WaveAbilityStats> abilities = new();
            Dictionary<MeleeConfig, WaveAbilityStats> abilitiesByEnemy = new();

            foreach (WaveSpawn spawn in EnumerateSpawns(wave))
            {
                if (TryGetWalkTime(spawn.Enemy, length, out MeleeConfig enemy, out float walkTime) == false || enemy.Ability == null)
                    continue;

                if (abilitiesByEnemy.TryGetValue(enemy, out WaveAbilityStats abilityStats) == false)
                {
                    abilityStats = CreateAbilityStats(enemy, spawn.Time, walkTime, laneCoverage);
                    abilitiesByEnemy.Add(enemy, abilityStats);
                    abilities.Add(abilityStats);
                }

                abilityStats.CastersCount++;
                abilityStats.CasterSpawns.Add(spawn.Time);
            }

            foreach (KeyValuePair<MeleeConfig, WaveAbilityStats> casterAbility in abilitiesByEnemy)
                AddSummonedToBase(casterAbility.Value, casterAbility.Key);

            return abilities;
        }

        private static WaveAbilityStats CreateAbilityStats(
            MeleeConfig enemy,
            float firstSpawnTime,
            float walkTime,
            LaneCoverage laneCoverage)
        {
            int castsCount = CountCastsToBase(enemy, walkTime);

            WaveAbilityStats abilityStats = new()
            {
                Enemy = enemy.name,
                Ability = GetAbilityType(enemy.Ability),
                CastsToBase = castsCount,
                CastDelay = CalculateCastDelay(enemy, walkTime),
                CastInterval = enemy.Ability.Cooldown + enemy.Ability.Duration,
                CastDistanceStep = enemy.Ability.Cooldown * enemy.MoveSpeed,
            };

            if (castsCount > 0)
            {
                abilityStats.FirstCast = CreateCastStats(enemy, firstSpawnTime, FirstCastNumber);
                abilityStats.LastCast = CreateCastStats(enemy, firstSpawnTime, castsCount);
            }

            if (laneCoverage != null)
                abilityStats.CastsBeforeTowers = CountCastsBeforeTowers(enemy, castsCount, laneCoverage);

            return abilityStats;
        }

        // Касты, начатые до первой точки полосы под огнём, — по каждой дальности. Если полосу не покрывает ни одно
        // место, все касты до базы — до зоны башен.
        private static List<int> CountCastsBeforeTowers(MeleeConfig enemy, int castsToBase, LaneCoverage laneCoverage)
        {
            List<int> castsBeforeTowers = new();

            for (int i = 0; i < laneCoverage.Ranges.Count; i++)
            {
                float? firstCovered = laneCoverage.GetFirstCovered(i);
                int castsCount = 0;

                while (castsCount < castsToBase && IsBeforeCoverage(CalculateCastDistance(enemy, castsCount + 1), firstCovered))
                    castsCount++;

                castsBeforeTowers.Add(castsCount);
            }

            return castsBeforeTowers;
        }

        private static bool IsBeforeCoverage(float distance, float? firstCovered)
            => firstCovered.HasValue == false || distance < firstCovered.Value;

        // Время под огнём — только у боссов: по нему подбирается их здоровье (справочник кампании, раздел 5).
        private static List<EnemyUnderFireStats> CalculateUnderFire(EnemiesWaveConfig wave, float? length, LaneCoverage laneCoverage)
        {
            List<EnemyUnderFireStats> underFire = new();

            if (laneCoverage == null)
                return underFire;

            HashSet<MeleeConfig> countedBosses = new();

            foreach (WaveEnemyGroup group in GetGroups(wave))
            {
                if (TryGetWalkTime(group.EnemyConfig, length, out MeleeConfig enemy, out float walkTime) == false)
                    continue;

                if (enemy.IsBoss && countedBosses.Add(enemy))
                    underFire.Add(CreateUnderFireStats(enemy, walkTime, laneCoverage));
            }

            return underFire;
        }

        // Ход по покрытым отрезкам места плюс каст на каждой остановке внутри них: стоящего на касте бьют.
        private static EnemyUnderFireStats CreateUnderFireStats(MeleeConfig enemy, float walkTime, LaneCoverage laneCoverage)
        {
            int castsCount = CountCastsToBase(enemy, walkTime);
            float castDuration = castsCount > 0 ? enemy.Ability.Duration : 0f;
            List<float> castDistances = new();

            for (int castNumber = FirstCastNumber; castNumber <= castsCount; castNumber++)
                castDistances.Add(CalculateCastDistance(enemy, castNumber));

            EnemyUnderFireStats underFireStats = new() { Enemy = enemy.name };

            foreach (SlotCoverage slot in laneCoverage.Slots)
            {
                SlotUnderFireStats slotStats = new() { Slot = slot.Slot };

                for (int i = 0; i < laneCoverage.Ranges.Count; i++)
                {
                    int castsInZone = castDistances.Count(distance => slot.Contains(i, distance));

                    slotStats.Seconds.Add(slot.GetLength(i) / enemy.MoveSpeed + castsInZone * castDuration);
                }

                underFireStats.Slots.Add(slotStats);
            }

            return underFireStats;
        }

        private static AbilityCastStats CreateCastStats(MeleeConfig enemy, float spawnTime, int castNumber)
        {
            return new AbilityCastStats
            {
                Time = spawnTime + CalculateCastStartTime(enemy, castNumber),
                Distance = CalculateCastDistance(enemy, castNumber),
            };
        }

        private static void AddSummonedToBase(WaveAbilityStats abilityStats, MeleeConfig caster)
        {
            if (caster.Ability is not SummonAbilityConfig summon || summon.SummonedConfig == null)
                return;

            int summonedCount = abilityStats.CastersCount * abilityStats.CastsToBase * summon.Count;

            abilityStats.SummonedToBase = summonedCount;
            abilityStats.SummonedHealthToBase = summonedCount * summon.SummonedConfig.MaxHealth;
        }

        private static void AddSummoned(WaveStats waveStats, StageStats stageStats)
        {
            if (waveStats.Abilities == null)
                return;

            foreach (WaveAbilityStats abilityStats in waveStats.Abilities)
            {
                if (abilityStats.SummonedToBase.HasValue == false)
                    continue;

                stageStats.SummonedToBase = (stageStats.SummonedToBase ?? 0) + abilityStats.SummonedToBase.Value;
                stageStats.SummonedHealthToBase = AddOptional(stageStats.SummonedHealthToBase, abilityStats.SummonedHealthToBase);
            }
        }

        private static float? AddOptional(float? total, float? value)
            => value.HasValue ? (total ?? 0f) + value.Value : total;

        private static float? CalculateFirstArrival(IReadOnlyList<EnemySpawn> spawns)
        {
            if (spawns.Count == 0 || spawns.Any(spawn => spawn.ArrivalTime.HasValue == false))
                return null;

            return spawns.Min(spawn => spawn.ArrivalTime.Value);
        }

        private static float? CalculateLastArrival(IReadOnlyList<EnemySpawn> spawns)
        {
            if (spawns.Count == 0 || spawns.Any(spawn => spawn.ArrivalTime.HasValue == false))
                return null;

            return spawns.Max(spawn => spawn.ArrivalTime.Value);
        }

        // Сумма по всем врагам волны: шаблон групп × число повторов. Враги не MeleeConfig не учитываются:
        // игра их создать не может, об этом сообщает валидатор.
        private static float SumStat(EnemiesWaveConfig wave, Func<MeleeConfig, float> selector)
        {
            float patternSum = 0f;

            foreach (WaveEnemyGroup group in GetGroups(wave))
            {
                if (TryGetMelee(group.EnemyConfig, out MeleeConfig enemy))
                    patternSum += Math.Max(0, group.Count) * selector(enemy);
            }

            return Math.Max(0, wave.PatternRepeats) * patternSum;
        }

        private static int SumStat(EnemiesWaveConfig wave, Func<MeleeConfig, int> selector)
        {
            int patternSum = 0;

            foreach (WaveEnemyGroup group in GetGroups(wave))
            {
                if (TryGetMelee(group.EnemyConfig, out MeleeConfig enemy))
                    patternSum += Math.Max(0, group.Count) * selector(enemy);
            }

            return Math.Max(0, wave.PatternRepeats) * patternSum;
        }

        private static void CountEnemiesByType(EnemiesWaveConfig wave, SortedDictionary<string, int> enemiesByType)
        {
            int repeats = Math.Max(0, wave.PatternRepeats);

            foreach (WaveEnemyGroup group in GetGroups(wave))
            {
                int count = repeats * Math.Max(0, group.Count);

                if (group.EnemyConfig == null || count == 0)
                    continue;

                AddCount(enemiesByType, group.EnemyConfig.name, count);
            }
        }

        private static void AddCount(SortedDictionary<string, int> counts, string key, int count)
        {
            counts.TryGetValue(key, out int currentCount);
            counts[key] = currentCount + count;
        }

        private static bool TryGetMelee(CharacterConfig enemyConfig, out MeleeConfig enemy)
        {
            enemy = enemyConfig as MeleeConfig;

            return enemy != null;
        }

        // Как WavesSpawner: берётся первая дорога с нужным номером.
        private static LevelRoad FindRoad(LevelPrefabInfo prefab, PathNumber number)
            => prefab.Roads.FirstOrDefault(road => road.Number == number);

        private static float? GetLength(LevelRoad road, PathLane lane)
        {
            if (road == null || road.HasValidPath == false || road.Lengths.TryGetValue(lane, out float length) == false)
                return null;

            return length;
        }

        private static float CalculateThreat(int breakthroughDamage, int playerHealth)
            => playerHealth > 0 ? (float)breakthroughDamage / playerHealth : 0f;

        private static List<RoadTrafficStats> MergeTraffic(IEnumerable<RoadTrafficStats> entries)
        {
            return entries
                .GroupBy(entry => (entry.PathNumber, entry.Lane))
                .OrderBy(group => group.Key.PathNumber)
                .ThenBy(group => group.Key.Lane)
                .Select(group => new RoadTrafficStats
                {
                    PathNumber = group.Key.PathNumber,
                    Lane = group.Key.Lane,
                    WavesCount = group.Sum(entry => entry.WavesCount),
                    EnemiesCount = group.Sum(entry => entry.EnemiesCount),
                    Health = group.Sum(entry => entry.Health),
                })
                .ToList();
        }

        private static void SumStages(LevelConfig config, LevelStats levelStats)
        {
            int goldWithoutSkips = config.StartGold;
            int goldWithMaxSkips = config.StartGold;

            foreach (StageStats stageStats in levelStats.Stages)
            {
                stageStats.GoldAtStartWithoutSkips = goldWithoutSkips;
                stageStats.GoldAtStartWithMaxSkips = goldWithMaxSkips;

                goldWithoutSkips += stageStats.KillGold;
                goldWithMaxSkips += stageStats.KillGold + stageStats.MaxSkipGold;

                levelStats.WavesCount += stageStats.WavesCount;
                levelStats.EnemiesCount += stageStats.EnemiesCount;
                levelStats.Health += stageStats.Health;
                levelStats.EffectiveHealthPhysic += stageStats.EffectiveHealthPhysic;
                levelStats.EffectiveHealthMagic += stageStats.EffectiveHealthMagic;
                levelStats.KillGold += stageStats.KillGold;
                levelStats.MaxSkipGold += stageStats.MaxSkipGold;
                levelStats.BreakthroughDamage += stageStats.BreakthroughDamage;

                foreach (KeyValuePair<string, int> enemyCount in stageStats.EnemiesByType)
                    AddCount(levelStats.EnemiesByType, enemyCount.Key, enemyCount.Value);
            }

            levelStats.StagesCount = levelStats.Stages.Count;
            levelStats.TotalGoldWithoutSkips = goldWithoutSkips;
            levelStats.TotalGoldWithMaxSkips = goldWithMaxSkips;
            levelStats.BreakthroughThreat = CalculateThreat(levelStats.BreakthroughDamage, config.StartPlayerHealth);
            levelStats.Traffic = MergeTraffic(levelStats.Stages.SelectMany(stageStats => stageStats.Traffic));
        }

        // Длительность — момент, когда WavesSpawner закончит спавн всех волн: раньше победа невозможна.
        // Без скипов стейдж длится StageTime, с максимумом скипов — до первой возможности скипа.
        private static void CalculateDurations(IReadOnlyList<EnemiesWavesStageConfig> stages, LevelStats levelStats)
        {
            float stageStartWithoutSkips = 0f;
            float stageStartWithMaxSkips = 0f;

            foreach (EnemiesWavesStageConfig stage in stages)
            {
                float spawnCompletion = CalculateSpawnCompletion(stage);

                levelStats.DurationWithoutSkips = Math.Max(levelStats.DurationWithoutSkips, stageStartWithoutSkips + spawnCompletion);
                levelStats.DurationWithMaxSkips = Math.Max(levelStats.DurationWithMaxSkips, stageStartWithMaxSkips + spawnCompletion);

                stageStartWithoutSkips += stage.StageTime;
                stageStartWithMaxSkips += Math.Min(stage.TimeToSkipStage, stage.StageTime);
            }
        }

        // Спавн волны завершается через DelayBetweenSpawns после последнего врага.
        private static float CalculateSpawnCompletion(EnemiesWavesStageConfig stage)
        {
            float spawnCompletion = 0f;

            foreach (EnemiesWaveConfig wave in GetWaves(stage))
            {
                float waveCompletion = wave.StartDelay + CountEnemies(wave) * wave.DelayBetweenSpawns;
                spawnCompletion = Math.Max(spawnCompletion, waveCompletion);
            }

            return spawnCompletion;
        }

        // Пик врагов на поле без убийств: враг на поле от спавна до прихода на базу, стейджи идут без скипов.
        // Если время в пути хоть одного врага неизвестно, пик не считается.
        private static void CalculatePeaks(
            IReadOnlyList<EnemiesWavesStageConfig> stages,
            IReadOnlyList<List<EnemySpawn>> stagesSpawns,
            LevelStats levelStats)
        {
            List<EnemySpawn> levelSpawns = new();
            List<float> stageStarts = new();
            float stageStart = 0f;

            for (int i = 0; i < stages.Count; i++)
            {
                stageStarts.Add(stageStart);

                foreach (EnemySpawn spawn in stagesSpawns[i])
                {
                    if (spawn.TravelTime.HasValue == false)
                        return;

                    levelSpawns.Add(spawn.ShiftedBy(stageStart));
                }

                stageStart += stages[i].StageTime;
            }

            for (int i = 0; i < stages.Count; i++)
            {
                bool isLastStage = i == stages.Count - 1;
                float windowStart = stageStarts[i];
                float windowEnd = isLastStage ? float.PositiveInfinity : stageStarts[i + 1];
                int stagePeak = CountOnField(levelSpawns, windowStart);

                foreach (EnemySpawn spawn in levelSpawns)
                {
                    if (spawn.SpawnTime > windowStart && spawn.SpawnTime < windowEnd)
                        stagePeak = Math.Max(stagePeak, CountOnField(levelSpawns, spawn.SpawnTime));
                }

                levelStats.Stages[i].PeakEnemies = stagePeak;
                levelStats.PeakEnemies = Math.Max(levelStats.PeakEnemies ?? 0, stagePeak);
            }
        }

        private static int CountOnField(IReadOnlyList<EnemySpawn> spawns, float time)
            => spawns.Count(spawn => spawn.SpawnTime <= time && spawn.ArrivalTime > time);

        private static List<RoadStats> CalculateRoads(
            IReadOnlyList<EnemiesWavesStageConfig> stages,
            LevelPrefabInfo prefab,
            LevelCoverage coverage)
        {
            List<RoadStats> roadsStats = new();

            foreach (LevelRoad road in prefab.Roads)
            {
                RoadStats roadStats = new()
                {
                    PathNumber = road.Number,
                    Name = road.Name,
                    WaypointsCount = road.WaypointsCount,
                    LengthCenter = GetLength(road, PathLane.Center),
                    LengthLeft = GetLength(road, PathLane.Left),
                    LengthRight = GetLength(road, PathLane.Right),
                };

                if (roadStats.LengthCenter.HasValue)
                    AddTimesToBase(stages, roadStats);

                if (coverage.TryGet(road, PathLane.Center, out LaneCoverage laneCoverage))
                    roadStats.Coverage = CreateRoadCoverageStats(laneCoverage);

                roadsStats.Add(roadStats);
            }

            return roadsStats;
        }

        private static RoadCoverageStats CreateRoadCoverageStats(LaneCoverage laneCoverage)
        {
            RoadCoverageStats coverageStats = new();

            for (int i = 0; i < laneCoverage.Ranges.Count; i++)
            {
                coverageStats.FirstCovered.Add(laneCoverage.GetFirstCovered(i));
                coverageStats.TotalLength.Add(laneCoverage.GetTotalLength(i));
            }

            foreach (SlotCoverage slot in laneCoverage.Slots)
            {
                SlotCoverageStats slotStats = new() { Slot = slot.Slot };

                for (int i = 0; i < laneCoverage.Ranges.Count; i++)
                    slotStats.Length.Add(slot.GetLength(i));

                coverageStats.Slots.Add(slotStats);
            }

            return coverageStats;
        }

        private static void AddTimesToBase(IReadOnlyList<EnemiesWavesStageConfig> stages, RoadStats roadStats)
        {
            foreach (EnemiesWavesStageConfig stage in stages)
            {
                foreach (EnemiesWaveConfig wave in GetWaves(stage))
                {
                    if (wave.PathNumber != roadStats.PathNumber)
                        continue;

                    foreach (WaveEnemyGroup group in GetGroups(wave))
                    {
                        float? travelTime = CalculateTravelTime(group.EnemyConfig, roadStats.LengthCenter);

                        if (travelTime.HasValue)
                            roadStats.TimeToBase[group.EnemyConfig.name] = travelTime.Value;
                    }
                }
            }
        }

        private readonly struct EnemySpawn
        {
            public EnemySpawn(float spawnTime, float? travelTime)
            {
                SpawnTime = spawnTime;
                TravelTime = travelTime;
            }

            public float SpawnTime { get; }
            public float? TravelTime { get; }
            public float? ArrivalTime => SpawnTime + TravelTime;

            public EnemySpawn ShiftedBy(float offset) => new(SpawnTime + offset, TravelTime);
        }
    }
}
