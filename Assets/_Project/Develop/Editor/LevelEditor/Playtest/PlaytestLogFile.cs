using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Assets._Project.Develop.Editor.LevelEditor.Playtest
{
    // Файлы логов плейтеста — Logs/Playtests в корне проекта: папка Logs в .gitignore, в git логи не попадают.
    // Формат JSON тот же, что у срезов.
    public static class PlaytestLogFile
    {
        private const string LOGS_FOLDER = "Logs/Playtests";
        private const string FILE_TIME_FORMAT = "yyyy-MM-dd_HH-mm-ss";
        private const string FILE_EXTENSION = ".json";
        private const string FILE_NAME_SEPARATOR = "_";

        private static readonly Encoding _utf8WithoutBom = new UTF8Encoding(false);

        public static string CreatePath(string levelName, DateTime startedAt)
        {
            string fileTime = startedAt.ToString(FILE_TIME_FORMAT, CultureInfo.InvariantCulture);

            return LevelSliceExporter.ToFullPath($"{LOGS_FOLDER}/{fileTime}{FILE_NAME_SEPARATOR}{levelName}{FILE_EXTENSION}");
        }

        public static void Write(string path, PlaytestLog log)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, LevelSliceSerializer.ToJson(log), _utf8WithoutBom);
        }

        // Логи уровня по имени файла, от старых к новым: время в начале имени сортируется как текст.
        public static List<string> FindPaths(string levelName)
        {
            string folder = LevelSliceExporter.ToFullPath(LOGS_FOLDER);

            if (Directory.Exists(folder) == false)
                return new List<string>();

            string pattern = $"{FILE_NAME_SEPARATOR}{levelName}{FILE_EXTENSION}";

            return Directory.GetFiles(folder, $"*{pattern}")
                .Where(path => Path.GetFileName(path).EndsWith(pattern, StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
        }

        // false — файл не прочитан или это не лог в нынешнем формате
        public static bool TryRead(string path, out PlaytestLog log)
        {
            log = null;

            try
            {
                string json = File.ReadAllText(path, _utf8WithoutBom);
                log = LevelSliceSerializer.FromToken<PlaytestLog>(LevelSliceSerializer.ParseToken(json));

                return log != null;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is JsonException)
            {
                return false;
            }
        }
    }
}
