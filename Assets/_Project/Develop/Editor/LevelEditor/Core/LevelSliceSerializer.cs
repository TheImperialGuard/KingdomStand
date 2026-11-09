using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Assets._Project.Develop.Runtime.Configs.Gameplay;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Abilities;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Abilities.Enemies;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Projectiles;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Towers;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Stages;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Assets._Project.Develop.Runtime.Gameplay.Features.Projectiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Assets._Project.Develop.Editor.LevelEditor
{
    // Формат срезов: ключи в camelCase, enum строками, числа с точкой и не больше трёх знаков после неё, массивы чисел —
    // одной строкой, переводы строк CRLF, как в .editorconfig. При разборе неизвестный ключ — ошибка: опечатка не должна
    // молча превращаться в значение по умолчанию.
    public static class LevelSliceSerializer
    {
        private const string LINE_BREAK = "\r\n";
        private const int MIN_TOWER_LEVEL = 1;
        private const int MAX_TOWER_LEVEL = 5;
        private const int FirstAbilitySlot = 1;
        private const int SecondAbilitySlot = 2;

        // Эффективность башни — урон в секунду на столько золота полной цены.
        private const float EFFICIENCY_GOLD_AMOUNT = 100f;

        private const string NUMBERS_SEPARATOR = ", ";

        private static readonly JsonSerializerSettings _settings = CreateSettings();
        private static readonly JsonSerializer _serializer = JsonSerializer.Create(_settings);

        // Массив чисел и null, разбитый отступами на строки: массивы покрытия по дальностям в столбик растянули бы срез,
        // поэтому такие массивы пишутся одной строкой. Внутри строки JSON перевода строки нет — текст заметок не задет.
        private static readonly Regex _multilineNumbersArray = new(
            @"\[\s*\r?\n\s*(?:(?:-?\d+(?:\.\d+)?|null)\s*,\s*)*(?:-?\d+(?:\.\d+)?|null)\s*\]",
            RegexOptions.Compiled);

        private static readonly Regex _arrayItem = new(@"-?\d+(?:\.\d+)?|null", RegexOptions.Compiled);

        public static LevelSlice CreateSlice(
            LevelConfig config,
            int? campaignNumber,
            LevelPrefabInfo prefab,
            LevelStats stats,
            IReadOnlyList<LevelIssue> issues)
            => CreateSlice(config, config.Stages, campaignNumber, prefab, stats, issues);

        // Стейджи не из конфига — волны бесконечного режима, собранные генератором; stats посчитаны по ним же.
        public static LevelSlice CreateSlice(
            LevelConfig config,
            IReadOnlyList<EnemiesWavesStageConfig> stages,
            int? campaignNumber,
            LevelPrefabInfo prefab,
            LevelStats stats,
            IReadOnlyList<LevelIssue> issues)
        {
            LevelSlice slice = CreateDataSlice(config, stages);

            slice.CampaignNumber = campaignNumber;
            slice.PrefabPath = config.PrefabPath;
            slice.GameMode = config.GameMode.ToString();
            slice.TowerSlotsCount = prefab.Status == LevelPrefabStatus.Found ? prefab.TowerSlotsCount : null;
            slice.CoverageRanges = stats.CoverageRanges.Count > 0 ? stats.CoverageRanges : null;
            slice.Roads = JToken.FromObject(stats.Roads, _serializer);
            slice.Analysis = JToken.FromObject(stats, _serializer);

            for (int i = 0; i < slice.Stages.Count; i++)
                AttachStageAnalysis(slice.Stages[i], stats.Stages[i]);

            foreach (LevelIssue issue in issues)
            {
                if (issue.IsError)
                    slice.Errors.Add(issue.ToString());
                else
                    slice.Warnings.Add(issue.ToString());
            }

            return slice;
        }

        // Только импортируемые данные уровня — для сравнения с импортируемым срезом.
        public static LevelSlice CreateDataSlice(LevelConfig config) => CreateDataSlice(config, config.Stages);

        public static LevelSlice CreateDataSlice(LevelConfig config, IReadOnlyList<EnemiesWavesStageConfig> stages)
        {
            LevelSlice slice = new()
            {
                Level = config.name,
                StartGold = config.StartGold,
                StartPlayerHealth = config.StartPlayerHealth,
            };

            foreach (EnemiesWavesStageConfig stage in stages)
                slice.Stages.Add(CreateStageSlice(stage));

            return slice;
        }

        public static BalanceReference CreateReference(
            GameConfig gameConfig,
            IReadOnlyList<CharacterConfig> enemies,
            IReadOnlyList<ShootingTowerConfig> towers,
            TowersListConfig towersList,
            TowersAbilitiesListConfig towersAbilities,
            ProjectilesListConfig projectiles)
        {
            BalanceReference reference = new();

            if (gameConfig != null)
                reference.Game = CreateGameReference(gameConfig);

            foreach (CharacterConfig enemy in enemies)
            {
                if (enemy is MeleeConfig meleeEnemy)
                    reference.Enemies.Add(CreateEnemyReference(meleeEnemy));
            }

            Dictionary<ShootingTowerConfig, TowerUpgradeStep> upgradeSteps = MapUpgradeSteps(towersList);

            reference.Towers = towers
                .Select(tower => CreateTowerReference(tower, upgradeSteps, projectiles))
                .OrderBy(tower => tower.TowerType)
                .ThenBy(tower => tower.Level ?? int.MaxValue)
                .ThenBy(tower => tower.Asset, StringComparer.Ordinal)
                .ToList();

            reference.TowerAbilities = CreateTowerAbilityReferences(towersAbilities, projectiles);

            return reference;
        }

        public static string ToJson(object value)
        {
            StringBuilder builder = new();

            using (StringWriter writer = new(builder, CultureInfo.InvariantCulture))
            {
                writer.NewLine = LINE_BREAK;
                _serializer.Serialize(writer, value);
            }

            builder.Append(LINE_BREAK);

            return _multilineNumbersArray.Replace(builder.ToString(), JoinNumbersArray);
        }

        // Срезы стейджа и волны с блоком analysis — для буфера обмена окна редактора.
        public static StageSlice CreateStageSlice(EnemiesWavesStageConfig stage, StageStats stats)
        {
            StageSlice stageSlice = CreateStageSlice(stage);
            AttachStageAnalysis(stageSlice, stats);

            return stageSlice;
        }

        public static WaveSlice CreateWaveSlice(EnemiesWaveConfig wave, WaveStats stats)
        {
            WaveSlice waveSlice = CreateWaveSlice(wave);
            waveSlice.Analysis = JToken.FromObject(stats, _serializer);

            return waveSlice;
        }

        public static LevelSlice ParseSlice(string json) => JsonConvert.DeserializeObject<LevelSlice>(json, _settings);

        // JSON из буфера обмена разбирается в два шага: сначала дерево — по его ключам видно, уровень это, стейдж
        // или волна, — затем срез нужного типа. Даты не распознаются: заметки остаются строками как есть.
        public static JToken ParseToken(string json)
        {
            using StringReader stringReader = new(json);
            using JsonTextReader reader = new(stringReader)
            {
                DateParseHandling = DateParseHandling.None,
            };

            JToken token = JToken.ReadFrom(reader);

            if (reader.Read())
                throw new JsonReaderException("После JSON есть лишний текст");

            return token;
        }

        public static T FromToken<T>(JToken token) => token.ToObject<T>(_serializer);

        // Дерево JSON в формате срезов: ключи в camelCase, enum строками, числа округлены.
        public static JToken ToToken(object value) => JToken.FromObject(value, _serializer);

        private static JsonSerializerSettings CreateSettings()
        {
            DefaultContractResolver contractResolver = new()
            {
                // Ключи словарей — имена ассетов врагов, регистр в них сохраняем.
                NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = false },
            };

            return new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented,
                Culture = CultureInfo.InvariantCulture,
                DateParseHandling = DateParseHandling.None,
                MissingMemberHandling = MissingMemberHandling.Error,
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter { AllowIntegerValues = false },
                    new RoundingJsonConverter(LevelSliceNumbers.DECIMALS),
                },
            };
        }

        private static string JoinNumbersArray(Match array)
        {
            IEnumerable<string> items = _arrayItem.Matches(array.Value).Select(item => item.Value);

            return $"[{string.Join(NUMBERS_SEPARATOR, items)}]";
        }

        private static void AttachStageAnalysis(StageSlice stageSlice, StageStats stageStats)
        {
            stageSlice.Analysis = JToken.FromObject(stageStats, _serializer);

            for (int i = 0; i < stageSlice.Waves.Count; i++)
                stageSlice.Waves[i].Analysis = JToken.FromObject(stageStats.Waves[i], _serializer);
        }

        private static StageSlice CreateStageSlice(EnemiesWavesStageConfig stage)
        {
            StageSlice stageSlice = new()
            {
                StageTime = stage.StageTime,
                TimeToSkipStage = stage.TimeToSkipStage,
                Note = stage.Note ?? string.Empty,
            };

            foreach (EnemiesWaveConfig wave in LevelStatsCalculator.GetWaves(stage))
                stageSlice.Waves.Add(CreateWaveSlice(wave));

            return stageSlice;
        }

        private static WaveSlice CreateWaveSlice(EnemiesWaveConfig wave)
        {
            WaveSlice waveSlice = new()
            {
                PatternRepeats = wave.PatternRepeats,
                PathNumber = wave.PathNumber,
                Lane = wave.Lane,
                StartDelay = wave.StartDelay,
                DelayBetweenSpawns = wave.DelayBetweenSpawns,
                Note = wave.Note ?? string.Empty,
            };

            foreach (WaveEnemyGroup group in LevelStatsCalculator.GetGroups(wave))
            {
                waveSlice.Groups.Add(new EnemyGroupSlice
                {
                    Enemy = group.EnemyConfig != null ? group.EnemyConfig.name : null,
                    Count = group.Count,
                });
            }

            return waveSlice;
        }

        private static GameReference CreateGameReference(GameConfig gameConfig)
        {
            return new GameReference
            {
                GoldIndexForRemainingStageSeconds = gameConfig.GoldIndexForRemainingStageSeconds,
                MaxGoldForSkipStage = gameConfig.MaxGoldForSkipStage,
                SellTowerGoldReturnIndex = gameConfig.SellTowerGoldReturnIndex,
                GoldRewardByAdvert = gameConfig.GoldRewardByAdvert,
            };
        }

        private static EnemyReference CreateEnemyReference(MeleeConfig enemy)
        {
            return new EnemyReference
            {
                Asset = enemy.name,
                Name = enemy.CharacterName,
                MaxHealth = enemy.MaxHealth,
                MoveSpeed = enemy.MoveSpeed,
                DamageResistanceType = enemy.DamageResistanceType,
                DamageResistanceIndex = enemy.DamageResistanceIndex,
                EffectiveHealthPhysic = LevelStatsCalculator.CalculateEffectiveHealth(enemy, DamageTypes.Physic),
                EffectiveHealthMagic = LevelStatsCalculator.CalculateEffectiveHealth(enemy, DamageTypes.Magic),
                GoldOnDeath = enemy.GoldOnDeath,
                DamageOnFinishPath = enemy.DamageOnFinishPath,
                IsBoss = enemy.IsBoss,
                BossMaxHealthDividerPerHit = enemy.IsBoss ? enemy.BossMaxHealthDividerPerHit : null,
                IsFlying = enemy.IsFlying,
                Kind = enemy.Kind,
                Ability = enemy.Ability != null ? CreateEnemyAbilityReference(enemy.Ability) : null,
            };
        }

        private static EnemyAbilityReference CreateEnemyAbilityReference(EnemyAbilityConfig ability)
        {
            EnemyAbilityReference reference = new()
            {
                Type = LevelStatsCalculator.GetAbilityType(ability),
                Asset = ability.name,
                Name = ability.Name,
                Cooldown = ability.Cooldown,
                Duration = ability.Duration,
            };

            switch (ability)
            {
                case SummonAbilityConfig summon:
                    reference.SummonedEnemy = summon.SummonedConfig != null ? summon.SummonedConfig.name : null;
                    reference.Count = summon.Count;
                    break;

                case WarCryAbilityConfig warCry:
                    reference.Radius = warCry.Radius;
                    reference.SpeedBoostIndex = warCry.SpeedBoostIndex;
                    reference.SpeedBoostDuration = warCry.SpeedBoostDuration;
                    reference.TargetKinds = warCry.TargetKinds.ToList();
                    break;

                case SnowballThrowAbilityConfig snowballThrow:
                    reference.ThrowRange = snowballThrow.ThrowRange;
                    reference.FreezeDuration = snowballThrow.FreezeDuration;
                    break;
            }

            return reference;
        }

        // Слоты способностей — как в окне способностей башни IV уровня: первая группа, затем вторая.
        private static List<TowerAbilityReference> CreateTowerAbilityReferences(
            TowersAbilitiesListConfig towersAbilities,
            ProjectilesListConfig projectiles)
        {
            List<TowerAbilityReference> references = new();

            if (towersAbilities == null)
                return references;

            foreach (TowerTypes towerType in Enum.GetValues(typeof(TowerTypes)))
            {
                TowersAbilitiesConfig towerAbilities = FindTowerAbilities(towersAbilities, towerType);

                if (towerAbilities == null)
                    continue;

                AddTowerAbilityReferences(towerType, FirstAbilitySlot, towerAbilities.FirstAbilityGroup, projectiles, references);
                AddTowerAbilityReferences(towerType, SecondAbilitySlot, towerAbilities.SecondAbilityGroup, projectiles, references);
            }

            return references;
        }

        private static void AddTowerAbilityReferences(
            TowerTypes towerType,
            int slot,
            IReadOnlyList<AbilityConfig> abilities,
            ProjectilesListConfig projectiles,
            List<TowerAbilityReference> references)
        {
            if (abilities == null)
                return;

            foreach (AbilityConfig ability in abilities)
            {
                if (ability == null)
                    continue;

                TowerAbilityReference reference = new()
                {
                    TowerType = towerType,
                    Slot = slot,
                    Asset = ability.name,
                    Name = ability.Name,
                };

                int totalCost = 0;

                for (int level = 1; level <= ability.MaxLevel; level++)
                {
                    totalCost += ability.GetCostBy(level);
                    reference.Levels.Add(CreateTowerAbilityLevel(ability, level, totalCost, projectiles));
                }

                references.Add(reference);
            }
        }

        // Числа уровня — те же, что берут системы способностей башни из своего конфига.
        private static TowerAbilityLevelReference CreateTowerAbilityLevel(
            AbilityConfig ability,
            int level,
            int totalCost,
            ProjectilesListConfig projectiles)
        {
            TowerAbilityLevelReference reference = new()
            {
                Level = level,
                Cost = ability.GetCostBy(level),
                TotalCost = totalCost,
            };

            switch (ability)
            {
                case AdditionalShootingTargetsAbilityConfig additionalTargets:
                    reference.AdditionalTargets = additionalTargets.GetAdditionsTargetsBy(level);
                    break;

                case AdditionalArrowForSingleTargetAbilityConfig additionalArrows:
                    reference.AdditionalArrows = additionalArrows.GetAdditionsArrowsBy(level);
                    break;

                case PoisonedAttackAbilityConfig poisonedAttack:
                    reference.PoisonDamage = poisonedAttack.GetDamageByLevel(level);
                    reference.PoisonDuration = poisonedAttack.GetEffectTimeByLevel(level);
                    reference.PoisonInterval = poisonedAttack.GetEffectIntervalByLevel(level);
                    break;

                case OneShotAttackAbilityConfig oneShotAttack:
                    reference.Cooldown = oneShotAttack.GetCooldownBy(level);
                    break;

                case StunAbilityConfig stun:
                    reference.StunTime = stun.GetStunTimeBy(level);
                    reference.Radius = stun.Radius;
                    reference.Cooldown = stun.Cooldown;
                    break;

                case AttackStunAbilityConfig attackStun:
                    reference.StunTime = attackStun.GetStunTimeBy(level);
                    break;

                case ProjectileFragmentsAbilityConfig projectileFragments:
                    reference.FragmentDamage = projectileFragments.GetFragmentsDamageBy(level);
                    reference.FragmentsCount = projectileFragments.FragmentsCount;
                    reference.FragmentAreaRadius = FindDamageAreaRadius(projectiles, ProjectilesTypes.CannonballFragment);
                    break;
            }

            return reference;
        }

        // GetBy бросает исключение, если способностей для типа башни нет.
        private static TowersAbilitiesConfig FindTowerAbilities(TowersAbilitiesListConfig towersAbilities, TowerTypes towerType)
        {
            try
            {
                return towersAbilities.GetBy(towerType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Радиус урона по площади — у снарядов-ядер; null, если снаряд бьёт одну цель или его конфига нет.
        private static float? FindDamageAreaRadius(ProjectilesListConfig projectiles, ProjectilesTypes projectileType)
        {
            if (projectiles == null)
                return null;

            try
            {
                return projectiles.GetProjectileConfigBy(projectileType) is CannonballProjectileConfig cannonball
                    ? cannonball.DamageAreaRadius
                    : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static TowerReference CreateTowerReference(
            ShootingTowerConfig tower,
            IReadOnlyDictionary<ShootingTowerConfig, TowerUpgradeStep> upgradeSteps,
            ProjectilesListConfig projectiles)
        {
            float damagePerSecond = tower.AttackDamage * tower.AttackPerSecond;

            TowerReference reference = new()
            {
                Asset = tower.name,
                TowerType = tower.TowerType,
                Name = tower.TowerName,
                Cost = tower.Cost,
                AttackDamage = tower.AttackDamage,
                DamageType = tower.DamageType,
                AttackPerSecond = tower.AttackPerSecond,
                DamagePerSecond = damagePerSecond,
                AttackRange = tower.AttackRange,
                HitsFlying = LevelStatsCalculator.CanHitFlying(tower.TowerType),
                DamageAreaRadius = FindDamageAreaRadius(projectiles, tower.Projectile),
            };

            if (upgradeSteps.TryGetValue(tower, out TowerUpgradeStep upgradeStep))
            {
                reference.Level = upgradeStep.Level;
                reference.TotalCost = upgradeStep.TotalCost;

                if (upgradeStep.TotalCost > 0)
                    reference.DamagePerSecondPer100Gold = damagePerSecond / upgradeStep.TotalCost * EFFICIENCY_GOLD_AMOUNT;
            }

            return reference;
        }

        // Уровни и полная цена башен — как в игре: TowersListConfig.GetBy(тип, уровень), а улучшение всегда ведёт
        // на следующий уровень. Если конфига какого-то уровня нет, следующие уровни этого типа недостижимы.
        private static Dictionary<ShootingTowerConfig, TowerUpgradeStep> MapUpgradeSteps(TowersListConfig towersList)
        {
            Dictionary<ShootingTowerConfig, TowerUpgradeStep> upgradeSteps = new();

            if (towersList == null)
                return upgradeSteps;

            foreach (TowerTypes towerType in Enum.GetValues(typeof(TowerTypes)))
            {
                int totalCost = 0;

                for (int level = MIN_TOWER_LEVEL; level <= MAX_TOWER_LEVEL; level++)
                {
                    ShootingTowerConfig tower = FindTower(towersList, towerType, level);

                    if (tower == null)
                        break;

                    totalCost += tower.Cost;
                    upgradeSteps.TryAdd(tower, new TowerUpgradeStep(level, totalCost));
                }
            }

            return upgradeSteps;
        }

        // GetBy бросает исключение, если для типа нет дерева улучшений или уровень не поддерживается.
        private static ShootingTowerConfig FindTower(TowersListConfig towersList, TowerTypes towerType, int level)
        {
            try
            {
                return towersList.GetBy(towerType, level);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private readonly struct TowerUpgradeStep
        {
            public TowerUpgradeStep(int level, int totalCost)
            {
                Level = level;
                TotalCost = totalCost;
            }

            public int Level { get; }
            public int TotalCost { get; }
        }
    }
}
