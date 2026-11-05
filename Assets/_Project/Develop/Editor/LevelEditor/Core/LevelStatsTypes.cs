using System;
using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Characters;
using Assets._Project.Develop.Runtime.Gameplay.Features.LevelNavigation;
using Newtonsoft.Json;

namespace Assets._Project.Develop.Editor.LevelEditor
{
    // Выход одного врага волны: время от начала стейджа и конфиг врага.
    public readonly struct WaveSpawn
    {
        public WaveSpawn(float time, CharacterConfig enemy)
        {
            Time = time;
            Enemy = enemy;
        }

        public float Time { get; }
        public CharacterConfig Enemy { get; }
    }

    // Статистика волны. Времена — в секундах от начала стейджа.
    // Приход на базу — оценка без убийств с остановками на каст, null — время в пути неизвестно.
    // Здоровье летающих — часть Health, которую не снимают пушки; пишется, только если летающие есть.
    public class WaveStats
    {
        public int EnemiesCount { get; set; }
        public float Health { get; set; }
        public int KillGold { get; set; }
        public int BreakthroughDamage { get; set; }
        public float SpawnEnd { get; set; }
        public float? FirstArrival { get; set; }
        public float? LastArrival { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? FlyingHealth { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<WaveAbilityStats> Abilities { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<EnemyUnderFireStats> UnderFire { get; set; }
    }

    // Сколько секунд босс волны идёт под огнём каждого места, которое достаёт до полосы волны; массив — по дальностям
    // LevelStats.CoverageRanges. Ход по покрытым отрезкам плюс остановки на каст внутри них; заморозка и оглушение
    // не входят.
    public class EnemyUnderFireStats
    {
        public string Enemy { get; set; }
        public List<SlotUnderFireStats> Slots { get; set; } = new();
    }

    public class SlotUnderFireStats
    {
        public int Slot { get; set; }
        public List<float> Seconds { get; set; } = new();
    }

    // Касты врагов одного типа в волне — оценка без убийств: каждый кастует, пока не дойдёт до базы.
    // CastsToBase и CastDelay — на одного кастующего. Расписание первого из них: касты идут через CastInterval секунд
    // и CastDistanceStep пути, от FirstCast до LastCast — каст k: FirstCast + (k − 1) × шаг. У остальных кастующих
    // расписание сдвинуто на разницу выходов (CasterSpawns).
    // Призванные — сколько скелетов и HP выйдет на волну, если все кастующие дойдут до базы: верхняя граница.
    // Касты до зоны башен — сколько кастов один кастующий начнёт, пока полоса волны ещё не под огнём; массив —
    // по дальностям LevelStats.CoverageRanges.
    public class WaveAbilityStats
    {
        public string Enemy { get; set; }
        public EnemyAbilityTypes Ability { get; set; }
        public int CastersCount { get; set; }
        public List<float> CasterSpawns { get; set; } = new();
        public int CastsToBase { get; set; }
        public float CastDelay { get; set; }
        public float CastInterval { get; set; }
        public float CastDistanceStep { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public AbilityCastStats FirstCast { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public AbilityCastStats LastCast { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<int> CastsBeforeTowers { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? SummonedToBase { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? SummonedHealthToBase { get; set; }
    }

    // Начало каста: время от начала стейджа и путь от начала дороги.
    public class AbilityCastStats
    {
        public float Time { get; set; }
        public float Distance { get; set; }
    }

    // Статистика стейджа. Золото к началу стейджа считается так, будто враги прошлых стейджей убиты.
    // Состав — число врагов по имени ассета с учётом повторов шаблона.
    public class StageStats
    {
        public int WavesCount { get; set; }
        public int EnemiesCount { get; set; }
        public SortedDictionary<string, int> EnemiesByType { get; set; } = new(StringComparer.Ordinal);
        public float Health { get; set; }
        public float EffectiveHealthPhysic { get; set; }
        public float EffectiveHealthMagic { get; set; }
        public float HealthPerSecond { get; set; }
        public int KillGold { get; set; }
        public int MaxSkipGold { get; set; }
        public int GoldAtStartWithoutSkips { get; set; }
        public int GoldAtStartWithMaxSkips { get; set; }
        public int BreakthroughDamage { get; set; }
        public float BreakthroughThreat { get; set; }
        public float SpawnEnd { get; set; }
        public float SpawnOverflow { get; set; }
        public float? LastArrival { get; set; }
        public int? PeakEnemies { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? FlyingHealth { get; set; }

        // Пик врагов и Health — без призванных; это верхняя граница призывов, если призывающие дойдут до базы.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? SummonedToBase { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? SummonedHealthToBase { get; set; }

        public List<RoadTrafficStats> Traffic { get; set; } = new();

        [JsonIgnore]
        public List<WaveStats> Waves { get; set; } = new();
    }

    public class LevelStats
    {
        public int StagesCount { get; set; }
        public int WavesCount { get; set; }
        public int EnemiesCount { get; set; }
        public SortedDictionary<string, int> EnemiesByType { get; set; } = new(StringComparer.Ordinal);
        public float Health { get; set; }
        public float EffectiveHealthPhysic { get; set; }
        public float EffectiveHealthMagic { get; set; }
        public int KillGold { get; set; }
        public int MaxSkipGold { get; set; }
        public int TotalGoldWithoutSkips { get; set; }
        public int TotalGoldWithMaxSkips { get; set; }
        public int BreakthroughDamage { get; set; }
        public float BreakthroughThreat { get; set; }
        public float DurationWithoutSkips { get; set; }
        public float DurationWithMaxSkips { get; set; }
        public int? PeakEnemies { get; set; }
        public List<RoadTrafficStats> Traffic { get; set; } = new();

        // Дальности башен по возрастанию — по ним идут все массивы покрытия; в срезе — coverageRanges.
        [JsonIgnore]
        public List<float> CoverageRanges { get; set; } = new();

        [JsonIgnore]
        public List<StageStats> Stages { get; set; } = new();

        [JsonIgnore]
        public List<RoadStats> Roads { get; set; } = new();
    }

    // Дорога префаба: длины по полосам и время до базы по центру дороги для врагов, которые по ней идут,
    // с подъёмом после появления и остановками на каст.
    public class RoadStats
    {
        public PathNumber PathNumber { get; set; }
        public string Name { get; set; }
        public int WaypointsCount { get; set; }
        public float? LengthCenter { get; set; }
        public float? LengthLeft { get; set; }
        public float? LengthRight { get; set; }
        public SortedDictionary<string, float> TimeToBase { get; set; } = new(StringComparer.Ordinal);

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public RoadCoverageStats Coverage { get; set; }
    }

    // Покрытие центра дороги местами под башни; массивы — по дальностям LevelStats.CoverageRanges.
    // FirstCovered — с какого расстояния от спавна дорога впервые под огнём, null — ни одно место не достаёт.
    // TotalLength — сумма покрытия по местам. Места — только те, что достают хотя бы на одной дальности; номер —
    // с единицы.
    public class RoadCoverageStats
    {
        public List<float?> FirstCovered { get; set; } = new();
        public List<float> TotalLength { get; set; } = new();
        public List<SlotCoverageStats> Slots { get; set; } = new();
    }

    public class SlotCoverageStats
    {
        public int Slot { get; set; }
        public List<float> Length { get; set; } = new();
    }

    // Сколько волн, врагов и здоровья идёт по одной полосе одной дороги.
    public class RoadTrafficStats
    {
        public PathNumber PathNumber { get; set; }
        public PathLane Lane { get; set; }
        public int WavesCount { get; set; }
        public int EnemiesCount { get; set; }
        public float Health { get; set; }
    }
}
