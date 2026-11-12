using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Abilities;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Entities.Towers;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels;
using Assets._Project.Develop.Runtime.Gameplay.EntitiesCore;
using Assets._Project.Develop.Runtime.Gameplay.Features.AbilitiesFeature;
using Assets._Project.Develop.Runtime.Gameplay.Features.AbilitiesFeature.Abilities;
using Assets._Project.Develop.Runtime.Gameplay.Features.Enemies;
using Assets._Project.Develop.Runtime.Gameplay.Features.Level;
using Assets._Project.Develop.Runtime.Gameplay.Features.LevelStages;
using Assets._Project.Develop.Runtime.Gameplay.Features.Towers;
using Assets._Project.Develop.Runtime.Gameplay.Infrastructure;
using Assets._Project.Develop.Runtime.Infrastructure.DI;
using Assets._Project.Develop.Runtime.Utilities.ConfigsManagment;
using Assets._Project.Develop.Runtime.Utilities.Reactive;
using Assets._Project.Develop.Runtime.Utilities.Wallet;
using UnityEditor;
using UnityEngine;

namespace Assets._Project.Develop.Editor.LevelEditor.Playtest
{
    // Бот плейтеста для итераций баланса: играет уровень по плану покупок из JSON вместо игрока. Покупает по очереди,
    // как только хватает золота; первый стейдж запускает, когда на стартовое золото больше нечего купить; вызывает
    // стейдж досрочно, когда врагов на поле не больше порога; ускоряет время и по итогу прогона выходит из Play Mode.
    // Ускорение подбирается на лету так, чтобы шаг игры за кадр не превышал MaxGameStep: перезарядка башен сбрасывается
    // без переноса остатка, и на крупном шаге башни теряют до 20 % урона (при ×4 и 0,1 с за кадр бот проигрывал уровень 3,
    // который игрок проходил без потерь).
    // Запуск — Launch; прогон помечается automated и в сводку логов не идёт.
    // Только редактор: тикает от EditorApplication.update, а не от бутстрапа сцены.
    //
    // План: { "TimeScale": 4, "MaxGameStep": 0.025, "DisableRendering": false, "SkipMaxEnemies": 3, "SkipStages": [],
    // "Build": ["12A", "12A", "1M", "1M:1"] }. TimeScale — потолок ускорения; DisableRendering — выключить камеры ради кадров.
    // Шаг «<место><A|M|C>» — постройка башни на месте (с единицы, как в срезе) или улучшение стоящей там на уровень;
    // «<место>S» — продать башню на месте; «<место>:<1|2>» — следующий уровень первой или второй способности башни IV
    // на месте. SkipMaxEnemies < 0 — без досрочных вызовов; непустой SkipStages — вызывать только эти стейджи.
    // HandOver — только расставить застройку и отдать игру человеку: бот выполняет шаги плана до старта и отключается,
    // не запуская стейдж, не ускоряя время и не выходя из Play Mode; «В бой» нажимает игрок. Лог остаётся automated.
    public class PlaytestBot : IInitializable, IDisposable
    {
        private const string PlanPathKey = "KingdomStand.PlaytestBot.PlanPath";
        private const int MaxTowerLevel = 4;
        private const int TowersFirstLevel = 1;
        private const float SlotMatchDistance = 0.5f;
        private const char AbilitySeparator = ':';
        private const char SellLetter = 'S';
        private const int FirstAbilityGroup = 1;
        private const int SecondAbilityGroup = 2;
        private const int UnlimitedFrameRate = -1;
        private const float FrameTimeSmoothing = 0.1f;

        private readonly PlanData _plan;
        private readonly List<Step> _steps;
        private readonly PlaytestLogService _log;
        private readonly StagesCycle _stagesCycle;
        private readonly EntitiesLifeContext _entitiesLifeContext;
        private readonly Level _level;
        private readonly TowersPurchaseService _towersPurchaseService;
        private readonly TowersFactory _towersFactory;
        private readonly TowersPlaceholdersService _towersPlaceholdersService;
        private readonly AbilitiesFactory _abilitiesFactory;
        private readonly WalletService _walletService;
        private readonly TowersListConfig _towersListConfig;
        private readonly TowersAbilitiesListConfig _towersAbilitiesListConfig;

        private EnemiesOnFieldCounter _enemiesCounter;
        private int _nextStepIndex;
        private bool _wasRunInBackground;
        private bool _isRunning;
        private bool _isCycleLaunched;
        private int _originalTargetFrameRate;
        private float _smoothedFrameTime;
        private bool _isExitRequested;

        public PlaytestBot(
            PlanData plan,
            PlaytestLogService log,
            StagesCycle stagesCycle,
            EntitiesLifeContext entitiesLifeContext,
            Level level,
            TowersPurchaseService towersPurchaseService,
            TowersFactory towersFactory,
            TowersPlaceholdersService towersPlaceholdersService,
            AbilitiesFactory abilitiesFactory,
            WalletService walletService,
            ConfigsProviderService configsProviderService)
        {
            _plan = plan;
            _log = log;
            _stagesCycle = stagesCycle;
            _entitiesLifeContext = entitiesLifeContext;
            _level = level;
            _towersPurchaseService = towersPurchaseService;
            _towersFactory = towersFactory;
            _towersPlaceholdersService = towersPlaceholdersService;
            _abilitiesFactory = abilitiesFactory;
            _walletService = walletService;
            _towersListConfig = configsProviderService.GetConfig<TowersListConfig>();
            _towersAbilitiesListConfig = configsProviderService.GetConfig<TowersAbilitiesListConfig>();

            _steps = ParseSteps(plan.Build);
        }

        // Прогон уровня ботом; planPath — абсолютный путь к JSON плана. Стейдж — с нуля, как у PlaytestLaunch.
        public static void Launch(LevelConfig level, string planPath, int startStageIndex = 0, int? startGold = null)
        {
            if (File.Exists(planPath) == false)
                throw new FileNotFoundException("Plan of playtest bot not found", planPath);

            SessionState.SetString(PlanPathKey, planPath);
            PlaytestLauncher.Launch(new PlaytestLaunch(level, startStageIndex, startGold, true));
        }

        // Регистрация из сессии плейтеста: только для прогонов Claude и только если план задан через Launch.
        public static void TryRegister(DIContainer container)
        {
            string planPath = SessionState.GetString(PlanPathKey, string.Empty);

            if (string.IsNullOrEmpty(planPath) || File.Exists(planPath) == false)
                return;

            PlanData plan = JsonUtility.FromJson<PlanData>(File.ReadAllText(planPath));

            container.RegisterAsSingle(c => new PlaytestBot(
                plan,
                c.Resolve<PlaytestLogService>(),
                c.Resolve<StagesCycle>(),
                c.Resolve<EntitiesLifeContext>(),
                c.Resolve<Level>(),
                c.Resolve<TowersPurchaseService>(),
                c.Resolve<TowersFactory>(),
                c.Resolve<TowersPlaceholdersService>(),
                c.Resolve<AbilitiesFactory>(),
                c.Resolve<WalletService>(),
                c.Resolve<ConfigsProviderService>())).NonLazy();
        }

        public void Initialize()
        {
            _enemiesCounter = new EnemiesOnFieldCounter(_entitiesLifeContext);

            // Без фокуса редактора Play Mode стоит; в редакторе это поле пишет и в настройки проекта — вернуть по выходу
            _wasRunInBackground = PlayerSettings.runInBackground;
            Application.runInBackground = true;

            _originalTargetFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = UnlimitedFrameRate;

            if (_plan.DisableRendering)
                foreach (Camera camera in Camera.allCameras)
                    camera.enabled = false;

            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            _isRunning = true;

            Debug.Log($"Бот плейтеста: шагов плана {_steps.Count}, скорость до ×{_plan.TimeScale}, шаг до {_plan.MaxGameStep} с");
        }

        public void Dispose() => Stop();

        private void Tick()
        {
            if (_isRunning == false || EditorApplication.isPlaying == false)
                return;

            if (_plan.HandOver)
            {
                HandOverWhenBuilt();
                return;
            }

            if (_log.IsCompleted)
            {
                RequestExit();
                return;
            }

            UpdateTimeScale();

            bool hasProgress = TryPerformNextStep();

            // Как кнопка «В бой» попапа старта: подготовка кончается, когда на стартовое золото больше ничего не купить
            if (_isCycleLaunched == false && hasProgress == false)
            {
                _isCycleLaunched = true;
                _stagesCycle.Launch();
            }

            TrySkipStage();
        }

        // Шаг, на который не хватает золота, застройку не завершит: бот отключается и сообщает, сколько шагов не сделано
        private void HandOverWhenBuilt()
        {
            if (TryPerformNextStep())
                return;

            int notPerformedCount = _steps.Count - _nextStepIndex;

            if (notPerformedCount > 0)
                Debug.LogWarning($"Бот плейтеста: не хватило золота, шагов не сделано {notPerformedCount}");

            Debug.Log("Бот плейтеста: застройка расставлена, игра передана игроку");
            Stop();
        }

        // true — шаг сделан или пропущен; false — шагов нет или не хватает золота
        private bool TryPerformNextStep()
        {
            if (_nextStepIndex >= _steps.Count)
                return false;

            int stepIndexBefore = _nextStepIndex;

            Step step = _steps[_nextStepIndex];

            if (step.SlotIndex < 0 || step.SlotIndex >= _level.TowersPositions.Count)
            {
                SkipStep(step, "нет такого места");
                return true;
            }

            Vector3 slotPosition = _level.TowersPositions[step.SlotIndex].position;

            bool isDone;

            if (step.IsSell)
                isDone = TrySellTower(step, slotPosition);
            else if (step.AbilityGroup > 0)
                isDone = TryBuyAbility(step, slotPosition);
            else
                isDone = TryBuyTower(step, slotPosition);

            if (isDone)
                _nextStepIndex++;

            return _nextStepIndex != stepIndexBefore;
        }

        private bool TryBuyTower(Step step, Vector3 slotPosition)
        {
            Entity tower = FindTower(slotPosition);

            if (tower == null)
                return TryBuildTower(step, slotPosition);

            if (tower.TowerType.Value != step.TowerType)
                return SkipStep(step, $"на месте стоит {tower.TowerType.Value}");

            int nextLevel = tower.TowerLevel.Value + 1;

            if (nextLevel > MaxTowerLevel)
                return SkipStep(step, "башня уже IV");

            if (_towersPurchaseService.TryBuyTower(step.TowerType, nextLevel) == false)
                return false;

            ShootingTowerConfig config = _towersListConfig.GetBy(step.TowerType, nextLevel);

            PlaytestHooks.NotifyTowerBought(step.TowerType, nextLevel, config.Cost, slotPosition);
            _towersFactory.Create(tower.Transform.position, config, nextLevel);
            tower.SelfReleaseRequested.Value = true;

            return true;
        }

        private bool TryBuildTower(Step step, Vector3 slotPosition)
        {
            Entity placeholder = FindPlaceholder(slotPosition);

            if (placeholder == null)
                return SkipStep(step, "место занято");

            if (_towersPurchaseService.TryBuyTower(step.TowerType, TowersFirstLevel) == false)
                return false;

            ShootingTowerConfig config = _towersListConfig.GetBy(step.TowerType, TowersFirstLevel);

            PlaytestHooks.NotifyTowerBought(step.TowerType, TowersFirstLevel, config.Cost, slotPosition);
            _towersPlaceholdersService.ReleasePlaceholder(placeholder);
            _towersFactory.Create(placeholder.Transform.position, config, TowersFirstLevel);

            return true;
        }

        // Как кнопка продажи в попапе башни: золото, место под башню вместо неё
        private bool TrySellTower(Step step, Vector3 slotPosition)
        {
            Entity tower = FindTower(slotPosition);

            if (tower == null)
                return SkipStep(step, "на месте нет башни");

            _towersPurchaseService.Sell(tower);
            _towersPlaceholdersService.CreatePlaceholder(tower.Transform.position);
            tower.SelfReleaseRequested.Value = true;

            return true;
        }

        // Как попап способностей: в группе — первая способность, которая ещё не на максимуме
        private bool TryBuyAbility(Step step, Vector3 slotPosition)
        {
            Entity tower = FindTower(slotPosition);

            if (tower == null || tower.TowerLevel.Value < MaxTowerLevel)
                return SkipStep(step, "на месте нет башни IV");

            TowersAbilitiesConfig abilitiesConfig = _towersAbilitiesListConfig.GetBy(tower.TowerType.Value);
            IReadOnlyList<AbilityConfig> group = step.AbilityGroup == FirstAbilityGroup
                ? abilitiesConfig.FirstAbilityGroup
                : abilitiesConfig.SecondAbilityGroup;

            AbilityConfig config = null;
            Ability ability = null;

            foreach (AbilityConfig candidate in group)
            {
                Ability owned = tower.Abilities.Elements.FirstOrDefault(element => element.ID == candidate.ID);

                if (owned == null || owned.IsMaxLevel == false)
                {
                    config = candidate;
                    ability = owned;
                    break;
                }
            }

            if (config == null)
                return SkipStep(step, "способность уже на максимуме");

            int currentLevel = ability == null ? 0 : ability.CurrentLevel.Value;
            int cost = config.LevelsCosts[currentLevel];

            if (_walletService.Enough(CurrencyTypes.Gold, cost) == false)
                return false;

            _walletService.Spend(CurrencyTypes.Gold, cost);
            PlaytestHooks.NotifyAbilityBought(tower.TowerType.Value, config, cost, slotPosition);

            if (ability == null)
                tower.Abilities.Add(_abilitiesFactory.CreateAbilityFor(tower, config, 1));
            else
                ability.AddLevel(1);

            return true;
        }

        // Попап, который ставит игру на паузу, обнуляет масштаб — его не трогаем
        private void UpdateTimeScale()
        {
            _smoothedFrameTime = _smoothedFrameTime <= 0
                ? Time.unscaledDeltaTime
                : Mathf.Lerp(_smoothedFrameTime, Time.unscaledDeltaTime, FrameTimeSmoothing);

            if (Time.timeScale <= 0 || _smoothedFrameTime <= 0)
                return;

            Time.timeScale = Mathf.Clamp(_plan.MaxGameStep / _smoothedFrameTime, 1f, _plan.TimeScale);
        }

        private void TrySkipStage()
        {
            if (_plan.SkipMaxEnemies < 0 || _stagesCycle.StageCanBeSkiped.Value == false)
                return;

            if (_enemiesCounter.EnemiesCount.Value > _plan.SkipMaxEnemies)
                return;

            bool hasStagesList = _plan.SkipStages != null && _plan.SkipStages.Length > 0;

            if (hasStagesList && _plan.SkipStages.Contains(_log.CurrentStageNumber) == false)
                return;

            _stagesCycle.SkipStage();
        }

        private Entity FindTower(Vector3 slotPosition)
            => FindAt(slotPosition, entity => entity.TryGetTowerType(out _) && entity.TryGetTowerLevel(out _));

        private Entity FindPlaceholder(Vector3 slotPosition)
            => FindAt(slotPosition, entity => entity.TryGetInteractiveAction(out _) && entity.TryGetTowerType(out _) == false);

        private Entity FindAt(Vector3 slotPosition, Func<Entity, bool> isMatch)
        {
            foreach (Entity entity in _entitiesLifeContext.Entities)
            {
                if (entity.TryGetSelfReleaseRequested(out ReactiveVariable<bool> releaseRequested) && releaseRequested.Value)
                    continue;

                if (entity.TryGetTransform(out Transform transform) == false || transform == null)
                    continue;

                if (Vector3.Distance(transform.position, slotPosition) > SlotMatchDistance)
                    continue;

                if (isMatch(entity))
                    return entity;
            }

            return null;
        }

        private bool SkipStep(Step step, string reason)
        {
            Debug.LogWarning($"Бот плейтеста: шаг «{step.Token}» пропущен — {reason}");
            _nextStepIndex++;

            return false;
        }

        private void RequestExit()
        {
            if (_isExitRequested)
                return;

            _isExitRequested = true;
            EditorApplication.ExitPlaymode();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                Stop();
        }

        // Stop из Play Mode сцену не освобождает, поэтому бот отписывается и по выходу из Play Mode
        private void Stop()
        {
            if (_isRunning == false)
                return;

            _isRunning = false;

            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            _enemiesCounter?.Dispose();
            PlayerSettings.runInBackground = _wasRunInBackground;
            Application.targetFrameRate = _originalTargetFrameRate;
        }

        private static List<Step> ParseSteps(string[] tokens)
        {
            List<Step> steps = new();

            if (tokens == null)
                return steps;

            foreach (string token in tokens)
                steps.Add(ParseStep(token.Trim()));

            return steps;
        }

        private static Step ParseStep(string token)
        {
            int separatorIndex = token.IndexOf(AbilitySeparator);

            if (separatorIndex > 0)
            {
                int abilitySlot = int.Parse(token.Substring(0, separatorIndex));
                int group = int.Parse(token.Substring(separatorIndex + 1));

                if (group != FirstAbilityGroup && group != SecondAbilityGroup)
                    throw new FormatException($"Unknown ability group in step {token}");

                return new Step(token, abilitySlot - 1, default, group, false);
            }

            int slot = int.Parse(token.Substring(0, token.Length - 1));
            char letter = token[token.Length - 1];

            if (letter == SellLetter)
                return new Step(token, slot - 1, default, 0, true);

            TowerTypes type = letter switch
            {
                'A' => TowerTypes.Arrows,
                'M' => TowerTypes.Magic,
                'C' => TowerTypes.Cannon,
                _ => throw new FormatException($"Unknown tower letter in step {token}"),
            };

            return new Step(token, slot - 1, type, 0, false);
        }

        [Serializable]
        public class PlanData
        {
            public float TimeScale = 1f;
            public float MaxGameStep = 0.025f;
            public bool DisableRendering;
            public int SkipMaxEnemies = -1;
            public int[] SkipStages;
            public string[] Build;
            public bool HandOver;
        }

        private readonly struct Step
        {
            public Step(string token, int slotIndex, TowerTypes towerType, int abilityGroup, bool isSell)
            {
                Token = token;
                SlotIndex = slotIndex;
                TowerType = towerType;
                AbilityGroup = abilityGroup;
                IsSell = isSell;
            }

            public string Token { get; }
            public int SlotIndex { get; }
            public TowerTypes TowerType { get; }
            public int AbilityGroup { get; }
            public bool IsSell { get; }
        }
    }
}
