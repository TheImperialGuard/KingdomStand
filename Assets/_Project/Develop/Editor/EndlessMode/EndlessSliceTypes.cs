using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Weather;
using Assets._Project.Develop.Runtime.Gameplay.Features.ApplyDamage;
using Newtonsoft.Json;

namespace Assets._Project.Develop.Editor.EndlessMode
{
    // Блок endless среза бесконечного уровня — настройки EndlessModeConfig, по которым генератор собирает волны.
    // Импортируется целиком. Враги — имена ассетов MeleeConfig; null — пустой элемент списка. seed — зерно-образец:
    // по нему собраны волны среза, забег в игре берёт своё.
    public class EndlessSettingsSlice
    {
        [JsonProperty(Required = Required.Always)]
        public int Seed { get; set; }

        [JsonProperty(Required = Required.Always)]
        public WaveValueTableSlice WaveBudget { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<float> CycleStrength { get; set; } = new();

        [JsonProperty(Required = Required.Always)]
        public WaveValueTableSlice SpawnTime { get; set; }

        [JsonProperty(Required = Required.Always)]
        public WaveValueTableSlice RestTime { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<EndlessTierSlice> Tiers { get; set; } = new();

        [JsonProperty(Required = Required.Always)]
        public float ThemeWeight { get; set; }

        [JsonProperty(Required = Required.Always)]
        public float MinSpacing { get; set; }

        [JsonProperty(Required = Required.Always)]
        public float SafeCatchUpDistance { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int BossWavesStep { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<EndlessBossSlice> Bosses { get; set; } = new();

        [JsonProperty(Required = Required.Always)]
        public float BossWaveBudgetShare { get; set; }

        [JsonProperty(Required = Required.Always)]
        public WaveValueTableSlice HealthMultiplier { get; set; }

        [JsonProperty(Required = Required.Always)]
        public WaveValueTableSlice BossHealthMultiplier { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int MaxEnemiesToSkipWave { get; set; }
    }

    public class WaveValueTableSlice
    {
        [JsonProperty(Required = Required.Always)]
        public List<WaveValueKeySlice> Keys { get; set; } = new();

        [JsonProperty(Required = Required.Always)]
        public WaveValueTailModes TailMode { get; set; }

        [JsonProperty(Required = Required.Always)]
        public float TailStep { get; set; }
    }

    public class WaveValueKeySlice
    {
        [JsonProperty(Required = Required.Always)]
        public int Wave { get; set; }

        [JsonProperty(Required = Required.Always)]
        public float Value { get; set; }
    }

    public class EndlessTierSlice
    {
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public EndlessRoadRoles Road { get; set; }

        [JsonProperty(Required = Required.Always)]
        public WaveValueTableSlice Share { get; set; }

        [JsonProperty(Required = Required.Always)]
        public WaveValueTableSlice KindsPerWave { get; set; }

        [JsonProperty(Required = Required.Always)]
        public WaveValueTableSlice GroupSize { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<EndlessTierEnemySlice> Enemies { get; set; } = new();
    }

    // lastWave 0 — без конца; maxPerWave с пустыми keys — без потолка.
    public class EndlessTierEnemySlice
    {
        [JsonProperty(Required = Required.AllowNull)]
        public string Enemy { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int FirstWave { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int LastWave { get; set; }

        [JsonProperty(Required = Required.Always)]
        public float Price { get; set; }

        [JsonProperty(Required = Required.Always)]
        public WaveValueTableSlice MaxPerWave { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool SpawnsLast { get; set; }
    }

    public class EndlessBossSlice
    {
        [JsonProperty(Required = Required.AllowNull)]
        public string Boss { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<EndlessEscortGroupSlice> Escort { get; set; } = new();
    }

    public class EndlessEscortGroupSlice
    {
        [JsonProperty(Required = Required.AllowNull)]
        public string Enemy { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int GroupSize { get; set; }

        [JsonProperty(Required = Required.Always)]
        public float FirstDelay { get; set; }

        [JsonProperty(Required = Required.Always)]
        public float Period { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int GroupsCount { get; set; }
    }

    // Блок weather — справочно: правила погоды из WeatherConfig. Погода не зависит от зерна, поэтому в волны среза не
    // входит; правится она в инспекторе конфига погоды. transitions — какие погоды могут прийти на смену каждой.
    public class EndlessWeatherSlice
    {
        public WeatherKinds StartWeather { get; set; }
        public List<WeatherKindSlice> Weathers { get; set; } = new();
        public float ChangeChance { get; set; }
        public float ChangeChanceGrowth { get; set; }
        public SortedDictionary<WeatherKinds, List<WeatherKinds>> Transitions { get; set; } = new();
        public float TransitionDuration { get; set; }
        public LightningSlice Lightning { get; set; }
    }

    public class WeatherKindSlice
    {
        public WeatherKinds Kind { get; set; }
        public int WavesCount { get; set; }
        public bool HasRain { get; set; }
        public bool HasLightning { get; set; }
    }

    // Урон удара растёт по волнам по таблице множителя здоровья врагов.
    public class LightningSlice
    {
        public float MinInterval { get; set; }
        public float MaxInterval { get; set; }
        public float RoadStrikesShare { get; set; }
        public float Radius { get; set; }
        public float BaseDamage { get; set; }
        public DamageTypes DamageType { get; set; }
    }

    // Добавки бесконечного режима к analysis волны (стейджа среза). Здоровье — с множителем её волны, у босса — со своим.
    // Сила — сумма цен вышедших врагов, она сходится с бюджетом. heavyPath — тяжёлая дорога цикла; accent — акценты
    // лёгкой и тяжёлой части (null — без акцента, None — виды без защиты). pathsDelays — задержка первого звена дороги
    // (разрыв на стыке, не уместившийся в передышку), timeToSkip — с какой секунды открывается вызов следующей волны.
    public class EndlessStageAnalysis
    {
        public int Cycle { get; set; }
        public string HeavyPath { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DamageTypes? LightAccent { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DamageTypes? HeavyAccent { get; set; }

        public float Budget { get; set; }
        public float Strength { get; set; }
        public Dictionary<string, float> PathsDelays { get; set; } = new();
        public float TimeToSkip { get; set; }
        public float HealthMultiplier { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? BossHealthMultiplier { get; set; }

        public float HealthWithMultiplier { get; set; }
        public float EffectiveHealthPhysicWithMultiplier { get; set; }
        public float EffectiveHealthMagicWithMultiplier { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Boss { get; set; }

        // Ключ — имя тира, порядок — порядок тиров в конфиге. Сопровождение босса — отдельно
        public Dictionary<string, int> EnemiesByTier { get; set; } = new();

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? BossEscortCount { get; set; }
    }

    // Добавка к analysis потока (волны среза).
    public class EndlessWaveAnalysis
    {
        public float HealthWithMultiplier { get; set; }
    }

    // Добавки к analysis всего среза — суммы по выгруженным волнам.
    public class EndlessLevelAnalysis
    {
        public float HealthWithMultiplier { get; set; }
        public float EffectiveHealthPhysicWithMultiplier { get; set; }
        public float EffectiveHealthMagicWithMultiplier { get; set; }
    }
}
