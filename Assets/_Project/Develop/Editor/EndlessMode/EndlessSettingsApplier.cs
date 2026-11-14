using System.Collections.Generic;
using Assets._Project.Develop.Editor.LevelEditor;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless;
using UnityEditor;

namespace Assets._Project.Develop.Editor.EndlessMode
{
    // Записывает блок endless среза в SerializedObject EndlessModeConfig. Применяет изменения вызывающий код:
    // ApplyModifiedProperties — с Undo, ApplyModifiedPropertiesWithoutUndo — для копии в памяти. Враги должны быть уже
    // проверены: имя каждого есть в словаре enemies и это MeleeConfig. Описание, уровень и погода режима не трогаются.
    public static class EndlessSettingsApplier
    {
        private const string Seed = "_seed";
        private const string WaveBudget = "_waveBudget";
        private const string CycleStrength = "_cycleStrength";
        private const string SpawnTime = "_spawnTime";
        private const string RestTime = "_restTime";
        private const string Tiers = "_tiers";
        private const string ThemeWeight = "_themeWeight";
        private const string MinSpacing = "_minSpacing";
        private const string SafeCatchUpDistance = "_safeCatchUpDistance";
        private const string BossWavesStep = "_bossWavesStep";
        private const string Bosses = "_bosses";
        private const string BossWaveBudgetShare = "_bossWaveBudgetShare";
        private const string HealthMultiplier = "_healthMultiplier";
        private const string BossHealthMultiplier = "_bossHealthMultiplier";
        private const string MaxEnemiesToSkipWave = "_maxEnemiesToSkipWave";

        private const string TableKeys = "_keys";
        private const string TierName = "_name";
        private const string TierEnemies = "_enemies";
        private const string BossEscort = "_escort";

        private static readonly string _tableTailMode = ToBackingField(nameof(WaveValueTable.TailMode));
        private static readonly string _tableTailStep = ToBackingField(nameof(WaveValueTable.TailStep));
        private static readonly string _keyWave = ToBackingField(nameof(WaveValueKey.Wave));
        private static readonly string _keyValue = ToBackingField(nameof(WaveValueKey.Value));
        private static readonly string _tierRoad = ToBackingField(nameof(EndlessTier.Road));
        private static readonly string _tierShare = ToBackingField(nameof(EndlessTier.Share));
        private static readonly string _tierKindsPerWave = ToBackingField(nameof(EndlessTier.KindsPerWave));
        private static readonly string _tierGroupSize = ToBackingField(nameof(EndlessTier.GroupSize));
        private static readonly string _tierEnemyConfig = ToBackingField(nameof(EndlessTierEnemy.EnemyConfig));
        private static readonly string _tierEnemyFirstWave = ToBackingField(nameof(EndlessTierEnemy.FirstWave));
        private static readonly string _tierEnemyLastWave = ToBackingField(nameof(EndlessTierEnemy.LastWave));
        private static readonly string _tierEnemyPrice = ToBackingField(nameof(EndlessTierEnemy.Price));
        private static readonly string _tierEnemyMaxPerWave = ToBackingField(nameof(EndlessTierEnemy.MaxPerWave));
        private static readonly string _tierEnemySpawnsLast = ToBackingField(nameof(EndlessTierEnemy.SpawnsLast));
        private static readonly string _bossConfig = ToBackingField(nameof(EndlessBoss.Boss));
        private static readonly string _escortEnemy = ToBackingField(nameof(EndlessEscortGroup.Enemy));
        private static readonly string _escortGroupSize = ToBackingField(nameof(EndlessEscortGroup.GroupSize));
        private static readonly string _escortFirstDelay = ToBackingField(nameof(EndlessEscortGroup.FirstDelay));
        private static readonly string _escortPeriod = ToBackingField(nameof(EndlessEscortGroup.Period));
        private static readonly string _escortGroupsCount = ToBackingField(nameof(EndlessEscortGroup.GroupsCount));

        public static void Apply(
            SerializedObject config,
            EndlessSettingsSlice settings,
            IReadOnlyDictionary<string, CharacterConfig> enemies)
        {
            LevelSliceApplier.SetInt(Find(config, Seed), settings.Seed);
            ApplyTable(Find(config, WaveBudget), settings.WaveBudget);
            ApplyFloats(Find(config, CycleStrength), settings.CycleStrength);
            ApplyTable(Find(config, SpawnTime), settings.SpawnTime);
            ApplyTable(Find(config, RestTime), settings.RestTime);
            ApplyTiers(Find(config, Tiers), settings.Tiers, enemies);
            LevelSliceApplier.SetFloat(Find(config, ThemeWeight), settings.ThemeWeight);
            LevelSliceApplier.SetFloat(Find(config, MinSpacing), settings.MinSpacing);
            LevelSliceApplier.SetFloat(Find(config, SafeCatchUpDistance), settings.SafeCatchUpDistance);
            LevelSliceApplier.SetInt(Find(config, BossWavesStep), settings.BossWavesStep);
            ApplyBosses(Find(config, Bosses), settings.Bosses, enemies);
            LevelSliceApplier.SetFloat(Find(config, BossWaveBudgetShare), settings.BossWaveBudgetShare);
            ApplyTable(Find(config, HealthMultiplier), settings.HealthMultiplier);
            ApplyTable(Find(config, BossHealthMultiplier), settings.BossHealthMultiplier);
            LevelSliceApplier.SetInt(Find(config, MaxEnemiesToSkipWave), settings.MaxEnemiesToSkipWave);
        }

        private static void ApplyTable(SerializedProperty table, WaveValueTableSlice tableSlice)
        {
            SerializedProperty keys = Find(table, TableKeys);
            keys.arraySize = tableSlice.Keys.Count;

            for (int i = 0; i < tableSlice.Keys.Count; i++)
            {
                SerializedProperty key = keys.GetArrayElementAtIndex(i);

                LevelSliceApplier.SetInt(Find(key, _keyWave), tableSlice.Keys[i].Wave);
                LevelSliceApplier.SetFloat(Find(key, _keyValue), tableSlice.Keys[i].Value);
            }

            LevelSliceApplier.SetInt(Find(table, _tableTailMode), (int)tableSlice.TailMode);
            LevelSliceApplier.SetFloat(Find(table, _tableTailStep), tableSlice.TailStep);
        }

        private static void ApplyFloats(SerializedProperty list, IReadOnlyList<float> values)
        {
            list.arraySize = values.Count;

            for (int i = 0; i < values.Count; i++)
                LevelSliceApplier.SetFloat(list.GetArrayElementAtIndex(i), values[i]);
        }

        private static void ApplyTiers(
            SerializedProperty tiers,
            IReadOnlyList<EndlessTierSlice> tierSlices,
            IReadOnlyDictionary<string, CharacterConfig> enemies)
        {
            tiers.arraySize = tierSlices.Count;

            for (int i = 0; i < tierSlices.Count; i++)
            {
                SerializedProperty tier = tiers.GetArrayElementAtIndex(i);
                EndlessTierSlice tierSlice = tierSlices[i];

                LevelSliceApplier.SetString(Find(tier, TierName), tierSlice.Name);
                LevelSliceApplier.SetInt(Find(tier, _tierRoad), (int)tierSlice.Road);
                ApplyTable(Find(tier, _tierShare), tierSlice.Share);
                ApplyTable(Find(tier, _tierKindsPerWave), tierSlice.KindsPerWave);
                ApplyTable(Find(tier, _tierGroupSize), tierSlice.GroupSize);
                ApplyTierEnemies(Find(tier, TierEnemies), tierSlice.Enemies, enemies);
            }
        }

        private static void ApplyTierEnemies(
            SerializedProperty tierEnemies,
            IReadOnlyList<EndlessTierEnemySlice> enemySlices,
            IReadOnlyDictionary<string, CharacterConfig> enemies)
        {
            tierEnemies.arraySize = enemySlices.Count;

            for (int i = 0; i < enemySlices.Count; i++)
            {
                SerializedProperty tierEnemy = tierEnemies.GetArrayElementAtIndex(i);
                EndlessTierEnemySlice enemySlice = enemySlices[i];

                LevelSliceApplier.SetObject(Find(tierEnemy, _tierEnemyConfig), FindEnemy(enemies, enemySlice.Enemy));
                LevelSliceApplier.SetInt(Find(tierEnemy, _tierEnemyFirstWave), enemySlice.FirstWave);
                LevelSliceApplier.SetInt(Find(tierEnemy, _tierEnemyLastWave), enemySlice.LastWave);
                LevelSliceApplier.SetFloat(Find(tierEnemy, _tierEnemyPrice), enemySlice.Price);
                ApplyTable(Find(tierEnemy, _tierEnemyMaxPerWave), enemySlice.MaxPerWave);
                LevelSliceApplier.SetBool(Find(tierEnemy, _tierEnemySpawnsLast), enemySlice.SpawnsLast);
            }
        }

        private static void ApplyBosses(
            SerializedProperty bosses,
            IReadOnlyList<EndlessBossSlice> bossSlices,
            IReadOnlyDictionary<string, CharacterConfig> enemies)
        {
            bosses.arraySize = bossSlices.Count;

            for (int i = 0; i < bossSlices.Count; i++)
            {
                SerializedProperty boss = bosses.GetArrayElementAtIndex(i);
                SerializedProperty escort = Find(boss, BossEscort);
                IReadOnlyList<EndlessEscortGroupSlice> escortSlices = bossSlices[i].Escort;

                LevelSliceApplier.SetObject(Find(boss, _bossConfig), FindEnemy(enemies, bossSlices[i].Boss));
                escort.arraySize = escortSlices.Count;

                for (int j = 0; j < escortSlices.Count; j++)
                    ApplyEscortGroup(escort.GetArrayElementAtIndex(j), escortSlices[j], enemies);
            }
        }

        private static void ApplyEscortGroup(
            SerializedProperty escortGroup,
            EndlessEscortGroupSlice escortSlice,
            IReadOnlyDictionary<string, CharacterConfig> enemies)
        {
            LevelSliceApplier.SetObject(Find(escortGroup, _escortEnemy), FindEnemy(enemies, escortSlice.Enemy));
            LevelSliceApplier.SetInt(Find(escortGroup, _escortGroupSize), escortSlice.GroupSize);
            LevelSliceApplier.SetFloat(Find(escortGroup, _escortFirstDelay), escortSlice.FirstDelay);
            LevelSliceApplier.SetFloat(Find(escortGroup, _escortPeriod), escortSlice.Period);
            LevelSliceApplier.SetInt(Find(escortGroup, _escortGroupsCount), escortSlice.GroupsCount);
        }

        // null в срезе — пустой элемент списка
        private static CharacterConfig FindEnemy(IReadOnlyDictionary<string, CharacterConfig> enemies, string enemyName)
            => enemyName != null ? enemies[enemyName] : null;

        private static SerializedProperty Find(SerializedObject owner, string fieldName)
            => LevelConfigFields.FindRequired(owner, fieldName);

        private static SerializedProperty Find(SerializedProperty owner, string fieldName)
            => LevelConfigFields.FindRequired(owner, fieldName);

        private static string ToBackingField(string propertyName) => $"<{propertyName}>k__BackingField";
    }
}
