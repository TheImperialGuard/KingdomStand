using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Assets._Project.Develop.Runtime.Configs.Gameplay.Levels;
using Assets._Project.Develop.Runtime.Gameplay.GameMode;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Assets._Project.Develop.Editor.LevelEditor.Playtest
{
    // Сводка логов плейтеста одного уровня для разбора баланса — Markdown-таблицы прогонов и стейджей.
    // Берутся прогоны текущей версии уровня: данные стейджей в снимке лога совпадают с конфигом, заметки не в счёт.
    // У бесконечного уровня версии не сверяются: его волны задают настройки режима, а не стейджи. Прогоны под управлением
    // Claude (automated) пропускаются. Оборванный прогон даёт в таблицу стейджей только сыгранные целиком.
    public static class PlaytestLogSummary
    {
        private const string NO_VALUE = "—";
        private const string CELL_SEPARATOR = " | ";
        private const string ROW_START = "| ";
        private const string ROW_END = " |";
        private const string HEADER_SEPARATOR_CELL = "---";
        private const string LIST_SEPARATOR = ", ";
        private const string TOWERS_SEPARATOR = " ";
        private const string SUMMONED_BY_MARK = "←";
        private const string STAR = "★";
        private const string DAMAGE_MATCHED = "✓";
        private const string NUMBER_FORMAT = "0.#";
        private const string PERCENT_FORMAT = "0";
        private const string SECONDS_FORMAT = "00";
        private const int SECONDS_PER_MINUTE = 60;
        private const float PERCENT_SCALE = 100f;
        private const int FIRST_STAGE_NUMBER = 1;
        private const int PERFECT_STARS = 3;
        private const int AVERAGE_STARS = 2;
        private const int BAD_STARS = 1;
        private const int DEFEAT_STARS = 0;

        private static readonly CultureInfo _culture = CultureInfo.InvariantCulture;

        private static readonly string[] _runsHeader =
        {
            "Начат", "Со стейджа", "Итог", "Дошёл", "Время", "Здоровье", "Урон = потеряно", "Пик", "Досрочно", "Пушка",
            "Башни в конце",
        };

        private static readonly string[] _stagesHeader =
        {
            "Стейдж", "Прогонов", "Длительность", "Золото на старте", "Здоровья потеряно", "Прорвались", "Пик", "Досрочно",
            "Пушка", "Призывы",
        };

        // includeOldVersions — взять и прогоны прошлых версий уровня
        public static string Create(LevelConfig level, bool includeOldVersions)
        {
            bool isEndless = level.GameMode == GameModes.Infinity;
            JToken currentVersion = CreateVersionToken(LevelSliceSerializer.CreateDataSlice(level));

            List<PlaytestLog> runs = new();
            int oldVersionsCount = 0;
            int automatedCount = 0;
            int unreadableCount = 0;

            foreach (string path in PlaytestLogFile.FindPaths(level.name))
            {
                if (PlaytestLogFile.TryRead(path, out PlaytestLog log) == false)
                {
                    unreadableCount++;
                    continue;
                }

                if (log.Level != level.name)
                    continue;

                if (log.Automated)
                {
                    automatedCount++;
                    continue;
                }

                bool isCurrentVersion = isEndless || IsVersion(log, currentVersion);

                if (isCurrentVersion == false)
                {
                    oldVersionsCount++;

                    if (includeOldVersions == false)
                        continue;
                }

                runs.Add(log);
            }

            StringBuilder text = new();

            AppendHeader(text, level.name, runs, oldVersionsCount, automatedCount, unreadableCount, includeOldVersions, isEndless);

            if (runs.Count == 0)
                return text.ToString();

            AppendRuns(text, runs);
            AppendStages(text, runs);

            return text.ToString();
        }

        private static void AppendHeader(
            StringBuilder text,
            string levelName,
            IReadOnlyList<PlaytestLog> runs,
            int oldVersionsCount,
            int automatedCount,
            int unreadableCount,
            bool includeOldVersions,
            bool isEndless)
        {
            int interruptedCount = runs.Count(run => IsFinished(run) == false);
            string oldVersionsState = includeOldVersions ? "в сводке" : "не в сводке";
            string versionsNote = isEndless ? "у бесконечного уровня версии не сверяются" : $"прошлых версий уровня: {oldVersionsCount} — {oldVersionsState}";

            text.AppendLine($"# Сводка плейтестов — {levelName}");
            text.AppendLine();
            text.AppendLine($"Прогонов: {runs.Count}, из них оборвано: {interruptedCount}; {versionsNote}.");
            text.AppendLine($"Под управлением Claude, пропущено: {automatedCount}. Не прочитано: {unreadableCount}.");

            List<PlaytestLog> fullWins = runs
                .Where(run => run.StartStage == FIRST_STAGE_NUMBER && IsWin(run.Result))
                .ToList();

            if (fullWins.Count > 0)
            {
                string averageTime = FormatTime(fullWins.Average(run => run.GameTime));
                string minTime = FormatTime(fullWins.Min(run => run.GameTime));
                string maxTime = FormatTime(fullWins.Max(run => run.GameTime));

                text.AppendLine($"Побед со стейджа 1: {fullWins.Count}, время в среднем {averageTime} ({minTime}–{maxTime}).");
            }

            if (runs.Count == 0)
                text.AppendLine("Прогонов нет.");
        }

        private static void AppendRuns(StringBuilder text, IReadOnlyList<PlaytestLog> runs)
        {
            text.AppendLine();
            text.AppendLine("## Прогоны");
            text.AppendLine();
            AppendTableHeader(text, _runsHeader);

            foreach (PlaytestLog run in runs)
            {
                int damage = run.Stages.Sum(stage => stage.Enemies.Sum(enemy => enemy.Damage));
                int healthLost = run.Stages.Sum(stage => stage.Health.Lost);
                int skippedCount = run.Stages.Count(stage => stage.IsSkipped);
                string damageCheck = damage == healthLost ? DAMAGE_MATCHED : $"{damage} ≠ {healthLost}";
                string reached = run.LevelStagesCount.HasValue ? $"{run.ReachedStage}/{run.LevelStagesCount}" : $"{run.ReachedStage}";
                string health = $"{run.FinalPlayerHealth}/{run.StartPlayerHealth}";

                AppendRow(
                    text,
                    run.StartedAt,
                    $"{run.StartStage}",
                    FormatResult(run.Result),
                    reached,
                    FormatTime(run.GameTime),
                    health,
                    damageCheck,
                    $"{run.PeakEnemies}",
                    $"{skippedCount}",
                    FormatCannon(run.Stages),
                    FormatTowers(run.TowersAtEnd));
            }

            text.AppendLine();
            text.AppendLine("Время — игровое, без пауз. «Урон = потеряно»: сумма урона прорвавшихся против потерянного здоровья;");
            text.AppendLine("при поражении урон может быть больше — здоровье не уходит ниже нуля.");
        }

        private static void AppendStages(StringBuilder text, IReadOnlyList<PlaytestLog> runs)
        {
            IEnumerable<IGrouping<int, PlaytestStageLog>> stageGroups = runs
                .SelectMany(GetPlayedStages)
                .GroupBy(stage => stage.Stage)
                .OrderBy(group => group.Key);

            text.AppendLine();
            text.AppendLine("## Стейджи");
            text.AppendLine();
            AppendTableHeader(text, _stagesHeader);

            foreach (IGrouping<int, PlaytestStageLog> group in stageGroups)
            {
                List<PlaytestStageLog> stages = group.ToList();
                string duration = FormatTime(stages.Average(stage => stage.Duration));
                string gold = FormatRange(stages.Min(stage => stage.Gold.AtStart), stages.Max(stage => stage.Gold.AtStart));
                int healthLostSum = stages.Sum(stage => stage.Health.Lost);
                int healthLostMax = stages.Max(stage => stage.Health.Lost);
                string healthLost = $"{healthLostSum} (макс. {healthLostMax})";
                int peak = stages.Max(stage => stage.PeakEnemies);
                int skippedCount = stages.Count(stage => stage.IsSkipped);
                string skipped = $"{skippedCount}/{stages.Count}";

                AppendRow(
                    text,
                    $"{group.Key}",
                    $"{stages.Count}",
                    duration,
                    gold,
                    healthLost,
                    FormatLeaks(stages),
                    $"{peak}",
                    skipped,
                    FormatCannon(stages),
                    FormatCasts(stages));
            }

            text.AppendLine();
            text.AppendLine("Стейджи — только сыгранные целиком. Длительность — в среднем по прогонам.");
            text.AppendLine("«Прорвались»: враг ×прорывов (урон), сумма по прогонам; призванные — враг←призвавший.");
            text.AppendLine("«Пушка»: попаданий от выстрелов, (попадания/выстрелы), врагов на попадание.");
            text.AppendLine("«Призывы»: призывающий — касты на одного вышедшего (кастов/вышло), сумма по прогонам.");
        }

        private static void AppendTableHeader(StringBuilder text, IReadOnlyList<string> header)
        {
            AppendRow(text, header.ToArray());
            AppendRow(text, Enumerable.Repeat(HEADER_SEPARATOR_CELL, header.Count).ToArray());
        }

        private static void AppendRow(StringBuilder text, params string[] cells)
        {
            text.Append(ROW_START);
            text.Append(string.Join(CELL_SEPARATOR, cells));
            text.AppendLine(ROW_END);
        }

        private static bool IsFinished(PlaytestLog run)
            => run.Result != PlaytestResult.Interrupted && run.Result != PlaytestResult.InProgress;

        private static bool IsWin(PlaytestResult result)
            => result == PlaytestResult.Perfect || result == PlaytestResult.Average || result == PlaytestResult.Bad;

        // У оборванного прогона последний стейдж недоигран
        private static IEnumerable<PlaytestStageLog> GetPlayedStages(PlaytestLog run)
        {
            bool isFinished = IsFinished(run);

            return run.Stages.Where(stage => stage.Stage < run.ReachedStage || (isFinished && stage.Stage == run.ReachedStage));
        }

        private static bool IsVersion(PlaytestLog run, JToken version)
        {
            if (run.LevelSnapshot == null || run.LevelSnapshot.Type == JTokenType.Null)
                return false;

            try
            {
                LevelSlice snapshot = LevelSliceSerializer.FromToken<LevelSlice>(run.LevelSnapshot);

                return JToken.DeepEquals(CreateVersionToken(snapshot), version);
            }
            catch (JsonException)
            {
                // Снимок в прежнем формате среза — уровень того времени с нынешним не сравнить
                return false;
            }
        }

        // Данные уровня, от которых зависит игра: золото и здоровье на старте, стейджи и волны — без заметок и анализа
        private static JToken CreateVersionToken(LevelSlice slice)
        {
            LevelSlice data = new()
            {
                Level = slice.Level,
                StartGold = slice.StartGold,
                StartPlayerHealth = slice.StartPlayerHealth,
            };

            foreach (StageSlice stage in slice.Stages)
            {
                StageSlice stageData = new()
                {
                    StageTime = stage.StageTime,
                    TimeToSkipStage = stage.TimeToSkipStage,
                };

                foreach (WaveSlice wave in stage.Waves)
                {
                    stageData.Waves.Add(new WaveSlice
                    {
                        Groups = wave.Groups,
                        PatternRepeats = wave.PatternRepeats,
                        PathNumber = wave.PathNumber,
                        Lane = wave.Lane,
                        StartDelay = wave.StartDelay,
                        DelayBetweenSpawns = wave.DelayBetweenSpawns,
                    });
                }

                data.Stages.Add(stageData);
            }

            return LevelSliceSerializer.ToToken(data);
        }

        private static string FormatResult(PlaytestResult result)
        {
            switch (result)
            {
                case PlaytestResult.Perfect:
                    return FormatStars(result, PERFECT_STARS);

                case PlaytestResult.Average:
                    return FormatStars(result, AVERAGE_STARS);

                case PlaytestResult.Bad:
                    return FormatStars(result, BAD_STARS);

                case PlaytestResult.Defeat:
                    return FormatStars(result, DEFEAT_STARS);

                default:
                    return result.ToString();
            }
        }

        private static string FormatStars(PlaytestResult result, int stars) => $"{result} {stars}{STAR}";

        private static string FormatTime(float seconds)
        {
            int totalSeconds = (int)Math.Round(seconds, MidpointRounding.AwayFromZero);
            int minutes = totalSeconds / SECONDS_PER_MINUTE;
            string restSeconds = (totalSeconds % SECONDS_PER_MINUTE).ToString(SECONDS_FORMAT, _culture);

            return $"{minutes}:{restSeconds}";
        }

        private static string FormatRange(int min, int max) => min == max ? $"{min}" : $"{min}–{max}";

        private static string FormatNumber(float value) => value.ToString(NUMBER_FORMAT, _culture);

        private static string FormatCannon(IEnumerable<PlaytestStageLog> stages)
        {
            List<PlaytestCannonLog> cannons = stages
                .Select(stage => stage.Cannon)
                .Where(cannon => cannon != null)
                .ToList();

            int shots = cannons.Sum(cannon => cannon.Shots);

            if (shots == 0)
                return NO_VALUE;

            int hits = cannons.Sum(cannon => cannon.Hits);
            int enemiesHit = cannons.Sum(cannon => cannon.EnemiesHit);
            string text = FormatHits(hits, shots, enemiesHit);

            int fragments = cannons.Sum(cannon => cannon.Fragments);

            if (fragments == 0)
                return text;

            int fragmentHits = cannons.Sum(cannon => cannon.FragmentHits);
            int fragmentEnemiesHit = cannons.Sum(cannon => cannon.FragmentEnemiesHit);
            string fragmentsText = FormatHits(fragmentHits, fragments, fragmentEnemiesHit);

            return $"{text}; осколки {fragmentsText}";
        }

        private static string FormatHits(int hits, int shots, int enemiesHit)
        {
            string percent = (hits * PERCENT_SCALE / shots).ToString(PERCENT_FORMAT, _culture);
            string enemiesPerHit = hits > 0 ? FormatNumber((float)enemiesHit / hits) : NO_VALUE;

            return $"{percent} % ({hits}/{shots}), {enemiesPerHit}";
        }

        private static string FormatTowers(IReadOnlyList<PlaytestTowerCount> towers)
        {
            if (towers == null || towers.Count == 0)
                return NO_VALUE;

            return string.Join(TOWERS_SEPARATOR, towers.Select(tower => $"{tower.Tower}{tower.Level}×{tower.Count}"));
        }

        private static string FormatLeaks(IEnumerable<PlaytestStageLog> stages)
        {
            List<string> leaks = stages
                .SelectMany(stage => stage.Enemies)
                .GroupBy(GetEnemyKey)
                .Select(group => (Enemy: group.Key, Leaked: group.Sum(enemy => enemy.Leaked), Damage: group.Sum(enemy => enemy.Damage)))
                .Where(leak => leak.Leaked > 0)
                .OrderByDescending(leak => leak.Damage)
                .Select(leak => $"{leak.Enemy} ×{leak.Leaked} ({leak.Damage})")
                .ToList();

            return leaks.Count > 0 ? string.Join(LIST_SEPARATOR, leaks) : NO_VALUE;
        }

        private static string FormatCasts(IEnumerable<PlaytestStageLog> stages)
        {
            List<string> casts = stages
                .SelectMany(stage => stage.Enemies)
                .Where(enemy => enemy.Casts.HasValue)
                .GroupBy(GetEnemyKey)
                .Select(group => (Enemy: group.Key, Casts: group.Sum(enemy => enemy.Casts.Value), Spawned: group.Sum(enemy => enemy.Spawned)))
                .Select(cast => $"{cast.Enemy} {FormatNumber((float)cast.Casts / cast.Spawned)} ({cast.Casts}/{cast.Spawned})")
                .ToList();

            return casts.Count > 0 ? string.Join(LIST_SEPARATOR, casts) : NO_VALUE;
        }

        private static string GetEnemyKey(PlaytestEnemyLog enemy)
            => enemy.SummonedBy == null ? enemy.Enemy : $"{enemy.Enemy}{SUMMONED_BY_MARK}{enemy.SummonedBy}";
    }
}
