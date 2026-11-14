using System.Collections.Generic;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Weather;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Configs.Gameplay.Levels.Endless
{
    // Правила бесконечного режима: уровень, на котором он идёт, и всё, из чего генератор собирает волны. Числа по волнам —
    // таблицы ключей. Состав волны — бюджет силы, поделённый между тирами по долям; раскладка — роли дорог, звенья,
    // окно выхода и разрывы на стыке волн. Ещё — боссы и их сопровождение, рост здоровья врагов и боссов, порог толпы,
    // выше которого нельзя вызвать волну досрочно, и погода.
    [CreateAssetMenu(menuName = "Configs/Gameplay/Levels/NewEndlessModeConfig", fileName = "EndlessModeConfig")]
    public class EndlessModeConfig : ScriptableObject
    {
        [Tooltip("Уровень режима: карта, стартовое золото, здоровье замка, лимиты улучшений. Его стейджи не используются")]
        [SerializeField] private LevelConfig _level;

        [Tooltip("Первый абзац описания режима в главном меню — цель, рядом с ним череп волны")]
        [SerializeField, TextArea(2, 6)] private string _goal;

        [Tooltip("Второй абзац описания режима в главном меню — совет, рядом с ним знак «!»")]
        [SerializeField, TextArea(2, 6)] private string _advice;

        [Tooltip("Зерно-образец: по нему строятся таблица волн в инспекторе и срез. Забег берёт своё случайное зерно")]
        [SerializeField] private int _seed = 1;

        [Tooltip("Бюджет силы волны: делится между тирами по долям, врагов вида — его доля / цена")]
        [SerializeField] private WaveValueTable _waveBudget = new();

        [Tooltip("«Пила» цикла: множитель бюджета по месту волны после босса — первый элемент для первой волны цикла. " +
            "Места нет в списке — множитель 1")]
        [SerializeField] private List<float> _cycleStrength = new();

        [Tooltip("Окно выхода врагов волны, с: звенья каждой дороги стоят в нём равномерно")]
        [SerializeField] private WaveValueTable _spawnTime = new();

        [Tooltip("Передышка после выхода последнего врага волны до начала следующей, с")]
        [SerializeField] private WaveValueTable _restTime = new();

        [Tooltip("Тиры врагов, от дорогих к дешёвым: остаток бюджета переходит к следующему тиру")]
        [SerializeField] private List<EndlessTier> _tiers = new();

        [Tooltip("Во сколько раз чаще выпадают виды с сопротивлением акцента цикла")]
        [SerializeField, Min(1f)] private float _themeWeight = 3f;

        [Tooltip("Минимальная дистанция врагов на одной полосе, единицы дороги")]
        [SerializeField, Min(0.1f)] private float _minSpacing = 0.6f;

        [Tooltip("Ближе этого расстояния от спавна быстрый враг новой волны не должен догонять медленного прошлой, единицы")]
        [SerializeField, Min(0f)] private float _safeCatchUpDistance = 21f;

        [Tooltip("Боссовая — каждая волна с номером, кратным шагу. Она же открывает новый цикл ролей дорог")]
        [SerializeField, Min(1)] private int _bossWavesStep = 10;

        [Tooltip("Боссы по кругу: на первой боссовой волне — первый, на второй — второй, после последнего — снова первый")]
        [SerializeField] private List<EndlessBoss> _bosses = new();

        [Tooltip("Доля бюджета лёгких тиров в боссовой волне; тяжёлые тиры в ней не выходят — их место занимает босс")]
        [SerializeField, Range(0f, 1f)] private float _bossWaveBudgetShare = 0.5f;

        [Tooltip("Множитель здоровья врагов из волн, кроме боссов. Призванных врагов не касается")]
        [SerializeField] private WaveValueTable _healthMultiplier = new();

        [Tooltip("Множитель здоровья боссов по номеру волны. Предел удара по боссу считается от его базового здоровья")]
        [SerializeField] private WaveValueTable _bossHealthMultiplier = new();

        [Tooltip("Досрочный вызов волны закрыт, пока врагов на поле больше этого числа (призванные — в счёт)")]
        [SerializeField, Min(0)] private int _maxEnemiesToSkipWave = 35;

        [Tooltip("Погода режима. Пусто — погоды нет, весь забег горит обычный свет уровня")]
        [SerializeField] private WeatherConfig _weather;

        public LevelConfig Level => _level;
        public string Goal => _goal;
        public string Advice => _advice;
        public int Seed => _seed;
        public WaveValueTable WaveBudget => _waveBudget;
        public IReadOnlyList<float> CycleStrength => _cycleStrength;
        public WaveValueTable SpawnTime => _spawnTime;
        public WaveValueTable RestTime => _restTime;
        public IReadOnlyList<EndlessTier> Tiers => _tiers;
        public float ThemeWeight => _themeWeight;
        public float MinSpacing => _minSpacing;
        public float SafeCatchUpDistance => _safeCatchUpDistance;
        public int BossWavesStep => _bossWavesStep;
        public IReadOnlyList<EndlessBoss> Bosses => _bosses;
        public float BossWaveBudgetShare => _bossWaveBudgetShare;
        public WaveValueTable HealthMultiplier => _healthMultiplier;
        public WaveValueTable BossHealthMultiplier => _bossHealthMultiplier;
        public int MaxEnemiesToSkipWave => _maxEnemiesToSkipWave;
        public WeatherConfig Weather => _weather;
    }
}
