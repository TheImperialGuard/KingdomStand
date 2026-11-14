using System.Collections.Generic;
using System.Linq;
using Assets._Project.Develop.Editor.LevelEditor;
using Assets._Project.Develop.Runtime.Configs.Gameplay;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Weather;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Assets._Project.Develop.Runtime.Gameplay.Features.EndlessMode;
using Newtonsoft.Json.Linq;

namespace Assets._Project.Develop.Editor.EndlessMode
{
    // Срез бесконечного уровня — тот же LevelSlice, что у кампании (формат — LevelSliceSerializer). Данные уровня
    // и дороги — из его LevelConfig, блок endless — настройки режима, weather — справочно правила погоды. Вместо своих
    // стейджей уровня — первые волны, собранные генератором режима по зерну-образцу, с тем же анализом, что у стейджей
    // кампании, и добавками: цикл, тяжёлая дорога и акценты, бюджет и сила, задержки дорог и момент вызова, множители
    // здоровья волны и босса, здоровье с ними, босс, его сопровождение и число врагов по тирам.
    public static class EndlessSliceSerializer
    {
        // Сколько волн в срезе Docs/Balance. Срез лежит в git и должен выходить одинаковым на любой машине, поэтому
        // число — в коде, а не в настройках редактора
        public const int SliceWavesCount = 50;

        private const string TierNumberPrefix = "Тир ";

        // wavesCount — сколько волн выгрузить; 0 — только данные уровня и настройки режима, как снимок в логе плейтеста
        public static LevelSlice CreateSlice(
            LevelConfig level,
            int? campaignNumber,
            LevelPrefabInfo prefab,
            GameConfig gameConfig,
            int wavesCount)
        {
            List<LevelIssue> issues = LevelValidator.Validate(level, prefab);
            EndlessModeConfig config = LevelEditorAssets.FindEndlessModeConfig(level);

            if (config == null)
            {
                issues.Add(LevelIssue.Error($"Уровень бесконечного режима {level.name} не задан ни в одном EndlessModeConfig"));
                return CreateLevelSlice(level, new List<EnemiesWavesStageConfig>(), campaignNumber, prefab, gameConfig, issues);
            }

            // С ошибками в конфиге режима волны не собрать — в срезе остаются настройки и сами ошибки
            bool hasGenerator = EndlessEditorWaves.TryCreateGenerator(config, issues, out EndlessWavesGenerator generator);

            List<EnemiesWavesStageConfig> waves = hasGenerator
                ? EndlessEditorWaves.Generate(generator, wavesCount)
                : new List<EnemiesWavesStageConfig>();

            LevelSlice slice = CreateLevelSlice(level, waves, campaignNumber, prefab, gameConfig, issues, out LevelStats stats);

            slice.Endless = LevelSliceSerializer.ToToken(CreateSettings(config));

            if (config.Weather != null)
                slice.Weather = LevelSliceSerializer.ToToken(CreateWeather(config.Weather));

            if (hasGenerator)
                AttachAnalysis(slice, stats, waves, generator, config);

            return slice;
        }

        public static EndlessSettingsSlice CreateSettings(EndlessModeConfig config)
        {
            return new EndlessSettingsSlice
            {
                Seed = config.Seed,
                WaveBudget = CreateTable(config.WaveBudget),
                CycleStrength = config.CycleStrength.ToList(),
                SpawnTime = CreateTable(config.SpawnTime),
                RestTime = CreateTable(config.RestTime),
                Tiers = config.Tiers.Select(CreateTier).ToList(),
                ThemeWeight = config.ThemeWeight,
                MinSpacing = config.MinSpacing,
                SafeCatchUpDistance = config.SafeCatchUpDistance,
                BossWavesStep = config.BossWavesStep,
                Bosses = config.Bosses.Select(CreateBoss).ToList(),
                BossWaveBudgetShare = config.BossWaveBudgetShare,
                HealthMultiplier = CreateTable(config.HealthMultiplier),
                BossHealthMultiplier = CreateTable(config.BossHealthMultiplier),
                MaxEnemiesToSkipWave = config.MaxEnemiesToSkipWave,
            };
        }

        private static LevelSlice CreateLevelSlice(
            LevelConfig level,
            List<EnemiesWavesStageConfig> waves,
            int? campaignNumber,
            LevelPrefabInfo prefab,
            GameConfig gameConfig,
            List<LevelIssue> issues)
            => CreateLevelSlice(level, waves, campaignNumber, prefab, gameConfig, issues, out _);

        private static LevelSlice CreateLevelSlice(
            LevelConfig level,
            List<EnemiesWavesStageConfig> waves,
            int? campaignNumber,
            LevelPrefabInfo prefab,
            GameConfig gameConfig,
            List<LevelIssue> issues,
            out LevelStats stats)
        {
            stats = EndlessEditorWaves.CalculateStats(level, waves, prefab, gameConfig);

            return LevelSliceSerializer.CreateSlice(level, waves, campaignNumber, prefab, stats, issues);
        }

        private static WaveValueTableSlice CreateTable(WaveValueTable table)
        {
            return new WaveValueTableSlice
            {
                Keys = table.Keys.Select(key => new WaveValueKeySlice { Wave = key.Wave, Value = key.Value }).ToList(),
                TailMode = table.TailMode,
                TailStep = table.TailStep,
            };
        }

        private static EndlessTierSlice CreateTier(EndlessTier tier)
        {
            return new EndlessTierSlice
            {
                Name = tier.Name ?? string.Empty,
                Road = tier.Road,
                Share = CreateTable(tier.Share),
                KindsPerWave = CreateTable(tier.KindsPerWave),
                GroupSize = CreateTable(tier.GroupSize),
                Enemies = tier.Enemies.Select(CreateTierEnemy).ToList(),
            };
        }

        private static EndlessTierEnemySlice CreateTierEnemy(EndlessTierEnemy tierEnemy)
        {
            return new EndlessTierEnemySlice
            {
                Enemy = GetEnemyName(tierEnemy.EnemyConfig),
                FirstWave = tierEnemy.FirstWave,
                LastWave = tierEnemy.LastWave,
                Price = tierEnemy.Price,
                MaxPerWave = CreateTable(tierEnemy.MaxPerWave),
                SpawnsLast = tierEnemy.SpawnsLast,
            };
        }

        private static EndlessBossSlice CreateBoss(EndlessBoss boss)
        {
            return new EndlessBossSlice
            {
                Boss = GetEnemyName(boss.Boss),
                Escort = boss.Escort.Select(CreateEscortGroup).ToList(),
            };
        }

        private static EndlessEscortGroupSlice CreateEscortGroup(EndlessEscortGroup escortGroup)
        {
            return new EndlessEscortGroupSlice
            {
                Enemy = GetEnemyName(escortGroup.Enemy),
                GroupSize = escortGroup.GroupSize,
                FirstDelay = escortGroup.FirstDelay,
                Period = escortGroup.Period,
                GroupsCount = escortGroup.GroupsCount,
            };
        }

        private static EndlessWeatherSlice CreateWeather(WeatherConfig weather)
        {
            EndlessWeatherSlice slice = new()
            {
                StartWeather = weather.StartWeather,
                ChangeChance = weather.ChangeChance,
                ChangeChanceGrowth = weather.ChangeChanceGrowth,
                TransitionDuration = weather.TransitionDuration,
                Lightning = CreateLightning(weather.Lightning),
            };

            foreach (WeatherKindConfig kind in weather.Weathers)
            {
                slice.Weathers.Add(new WeatherKindSlice
                {
                    Kind = kind.Kind,
                    WavesCount = kind.WavesCount,
                    HasRain = kind.HasRain,
                    HasLightning = kind.HasLightning,
                });

                slice.Transitions[kind.Kind] = weather.Weathers
                    .Where(next => weather.CanFollow(kind.Kind, next.Kind))
                    .Select(next => next.Kind)
                    .ToList();
            }

            return slice;
        }

        private static LightningSlice CreateLightning(LightningConfig lightning)
        {
            return new LightningSlice
            {
                MinInterval = lightning.MinInterval,
                MaxInterval = lightning.MaxInterval,
                RoadStrikesShare = lightning.RoadStrikesShare,
                Radius = lightning.Radius,
                BaseDamage = lightning.BaseDamage,
                DamageType = lightning.DamageType,
            };
        }

        // Добавки дописываются в конец блоков analysis, которые уже собрал LevelSliceSerializer.
        private static void AttachAnalysis(
            LevelSlice slice,
            LevelStats stats,
            IReadOnlyList<EnemiesWavesStageConfig> waves,
            EndlessWavesGenerator generator,
            EndlessModeConfig config)
        {
            EndlessLevelAnalysis levelAnalysis = new();

            for (int i = 0; i < waves.Count; i++)
            {
                int waveNumber = EndlessEditorWaves.FirstWaveNumber + i;
                StageStats stageStats = stats.Stages[i];
                EndlessStageAnalysis stageAnalysis = CreateStageAnalysis(waveNumber, stageStats, generator, config);
                IReadOnlyList<EnemiesWaveConfig> streams = waves[i].EnemiesWaveConfigs;

                Merge(slice.Stages[i].Analysis, stageAnalysis);

                for (int j = 0; j < slice.Stages[i].Waves.Count; j++)
                {
                    bool isBossStream = streams[j].EnemyGroups.Any(group => group.EnemyConfig is MeleeConfig { IsBoss: true });
                    float multiplier = isBossStream ? config.BossHealthMultiplier.GetValue(waveNumber) : stageAnalysis.HealthMultiplier;
                    float streamHealth = stageStats.Waves[j].Health * multiplier;

                    Merge(slice.Stages[i].Waves[j].Analysis, new EndlessWaveAnalysis { HealthWithMultiplier = streamHealth });
                }

                levelAnalysis.HealthWithMultiplier += stageAnalysis.HealthWithMultiplier;
                levelAnalysis.EffectiveHealthPhysicWithMultiplier += stageAnalysis.EffectiveHealthPhysicWithMultiplier;
                levelAnalysis.EffectiveHealthMagicWithMultiplier += stageAnalysis.EffectiveHealthMagicWithMultiplier;
            }

            Merge(slice.Analysis, levelAnalysis);
        }

        private static EndlessStageAnalysis CreateStageAnalysis(
            int waveNumber,
            StageStats stageStats,
            EndlessWavesGenerator generator,
            EndlessModeConfig config)
        {
            EndlessWavePlan plan = generator.GetPlan(waveNumber);
            EndlessWaveSummary summary = EndlessWaveSummary.Create(plan, config);
            MeleeConfig boss = plan.Boss?.Boss;

            float bossHealth = boss != null ? boss.MaxHealth : 0f;
            float bossEffectiveHealthPhysic = boss != null ? LevelStatsCalculator.CalculateEffectiveHealth(boss, DamageTypes.Physic) : 0f;
            float bossEffectiveHealthMagic = boss != null ? LevelStatsCalculator.CalculateEffectiveHealth(boss, DamageTypes.Magic) : 0f;

            EndlessStageAnalysis analysis = new()
            {
                Cycle = plan.Cycle,
                HeavyPath = plan.HeavyPathNumber.ToString(),
                LightAccent = plan.LightAccent,
                HeavyAccent = plan.HeavyAccent,
                Budget = plan.Budget,
                Strength = plan.Strength,
                TimeToSkip = plan.TimeToSkip,
                HealthMultiplier = config.HealthMultiplier.GetValue(waveNumber),
                BossHealthMultiplier = boss != null ? config.BossHealthMultiplier.GetValue(waveNumber) : null,
                HealthWithMultiplier = EndlessEditorWaves.ApplyMultipliers(stageStats.Health, bossHealth, waveNumber, config),
                EffectiveHealthPhysicWithMultiplier = EndlessEditorWaves.ApplyMultipliers(
                    stageStats.EffectiveHealthPhysic, bossEffectiveHealthPhysic, waveNumber, config),
                EffectiveHealthMagicWithMultiplier = EndlessEditorWaves.ApplyMultipliers(
                    stageStats.EffectiveHealthMagic, bossEffectiveHealthMagic, waveNumber, config),
                Boss = boss != null ? boss.name : null,
                BossEscortCount = boss != null ? summary.BossEscortCount : null,
            };

            for (int i = 0; i < generator.PathNumbers.Count; i++)
                analysis.PathsDelays[generator.PathNumbers[i].ToString()] = plan.PathsDelays[i];

            for (int i = 0; i < config.Tiers.Count; i++)
                analysis.EnemiesByTier[GetTierName(config.Tiers[i], i)] = summary.TiersEnemiesCounts[i];

            return analysis;
        }

        // Имя тира — ключ в enemiesByTier; у тира без имени — его номер
        private static string GetTierName(EndlessTier tier, int tierIndex)
        {
            if (string.IsNullOrWhiteSpace(tier.Name) == false)
                return tier.Name;

            int tierNumber = tierIndex + 1;

            return TierNumberPrefix + tierNumber;
        }

        private static void Merge(JToken analysis, object additions)
            => ((JObject)analysis).Merge(LevelSliceSerializer.ToToken(additions));

        private static string GetEnemyName(CharacterConfig enemy) => enemy != null ? enemy.name : null;
    }
}
