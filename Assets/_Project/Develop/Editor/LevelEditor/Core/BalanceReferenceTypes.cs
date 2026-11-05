using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Towers;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Assets._Project.Develop.Runtime.Gameplay.Features.Enemies;
using Newtonsoft.Json;

namespace Assets._Project.Develop.Editor.LevelEditor
{
    // Справочник для балансировки — Docs/Balance/Reference.json: параметры GameConfig, враги, башни и способности башен.
    // Поля, которые есть не у всех записей, пишутся, только когда заданы.
    public class BalanceReference
    {
        public GameReference Game { get; set; }
        public List<EnemyReference> Enemies { get; set; } = new();
        public List<TowerReference> Towers { get; set; } = new();
        public List<TowerAbilityReference> TowerAbilities { get; set; } = new();
    }

    public class GameReference
    {
        public float GoldIndexForRemainingStageSeconds { get; set; }
        public int MaxGoldForSkipStage { get; set; }
        public float SellTowerGoldReturnIndex { get; set; }
        public int GoldRewardByAdvert { get; set; }
    }

    // Враг — MeleeConfig: другие конфиги персонажей EnemiesFactory не создаёт.
    public class EnemyReference
    {
        public string Asset { get; set; }
        public string Name { get; set; }
        public float MaxHealth { get; set; }
        public float MoveSpeed { get; set; }
        public DamageTypes DamageResistanceType { get; set; }
        public float DamageResistanceIndex { get; set; }
        public float EffectiveHealthPhysic { get; set; }
        public float EffectiveHealthMagic { get; set; }
        public int GoldOnDeath { get; set; }
        public int DamageOnFinishPath { get; set; }
        public bool IsBoss { get; set; }
        public bool IsFlying { get; set; }
        public EnemyKinds Kind { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public EnemyAbilityReference Ability { get; set; }
    }

    // Виды способностей врагов — по классам конфигов EnemyAbilityConfig.
    public enum EnemyAbilityTypes
    {
        Summon,
        WarCry,
        SnowballThrow,
    }

    // Способность врага: каст Duration секунд на месте, перезарядка Cooldown — с появления и после каждого каста.
    public class EnemyAbilityReference
    {
        public EnemyAbilityTypes Type { get; set; }
        public string Asset { get; set; }
        public string Name { get; set; }
        public float Cooldown { get; set; }
        public float Duration { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SummonedEnemy { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Count { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? Radius { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? SpeedBoostIndex { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? SpeedBoostDuration { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<EnemyKinds> TargetKinds { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? ThrowRange { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? FreezeDuration { get; set; }
    }

    // Уровень башни — как его видит TowersListConfig; null, если до конфига нельзя дойти улучшениями.
    // Cost — цена покупки или улучшения до этого уровня, TotalCost — сумма цен всех уровней до него.
    // Урон в секунду — без способностей и урона по площади; DamageAreaRadius — радиус урона ядра.
    public class TowerReference
    {
        public string Asset { get; set; }
        public TowerTypes TowerType { get; set; }
        public int? Level { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public int? TotalCost { get; set; }
        public float AttackDamage { get; set; }
        public DamageTypes DamageType { get; set; }
        public float AttackPerSecond { get; set; }
        public float DamagePerSecond { get; set; }
        public float? DamagePerSecondPer100Gold { get; set; }
        public float AttackRange { get; set; }
        public bool HitsFlying { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? DamageAreaRadius { get; set; }
    }

    // Способность башни IV уровня: слот 1 или 2 окна способностей, уровни по порядку покупки.
    public class TowerAbilityReference
    {
        public TowerTypes TowerType { get; set; }
        public int Slot { get; set; }
        public string Asset { get; set; }
        public string Name { get; set; }
        public List<TowerAbilityLevelReference> Levels { get; set; } = new();
    }

    // Уровень способности башни: цена, сумма цен до него и числа своего вида способности.
    public class TowerAbilityLevelReference
    {
        public int Level { get; set; }
        public int Cost { get; set; }
        public int TotalCost { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? AdditionalTargets { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? AdditionalArrows { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? PoisonDamage { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? PoisonDuration { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? PoisonInterval { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? Cooldown { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? StunTime { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? Radius { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? FragmentDamage { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? FragmentsCount { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? FragmentAreaRadius { get; set; }
    }
}
