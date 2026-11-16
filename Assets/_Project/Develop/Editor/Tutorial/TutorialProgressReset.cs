using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using YG;

namespace Assets._Project.Develop.Editor.Tutorial
{
    // Сброс показанного обучения в сохранении редактора, чтобы пройти его снова. В редакторе с целью WebGL данные игрока
    // лежат в файле сохранений PluginYG2: JSON PlayerData строкой под ключом Data.PlayerData. Из него убирается список
    // ShownTutorials — без списка TutorialProgressService считает, что ничего ещё не показано. Остальное сохранение не
    // меняется. В Play Mode пункт недоступен: игра держит прогресс в памяти и перезапишет файл.
    public static class TutorialProgressReset
    {
        private const string MenuPath = "Tools/Tutorial/Reset Shown Tutorials";
        private const string SavesFileName = "SavesEditorYG2.json";
        private const string DataKey = "Data";
        private const string PlayerDataKey = "PlayerData";
        private const string ShownTutorialsKey = "ShownTutorials";

        [MenuItem(MenuPath)]
        private static void ResetShownTutorials()
        {
            string path = Path.Combine(InfoYG.PATCH_PC_EDITOR, SavesFileName);

            if (File.Exists(path) == false)
            {
                Debug.Log("Сохранения редактора нет — обучение и так покажется с начала");
                return;
            }

            JObject saves = JObject.Parse(File.ReadAllText(path));
            string playerDataJson = (string)saves[DataKey]?[PlayerDataKey];

            if (string.IsNullOrEmpty(playerDataJson))
            {
                Debug.Log("В сохранении редактора нет данных игрока — обучение и так покажется с начала");
                return;
            }

            JObject playerData = JObject.Parse(playerDataJson);
            int shownCount = playerData[ShownTutorialsKey] is JArray shownTutorials ? shownTutorials.Count : 0;

            playerData.Remove(ShownTutorialsKey);
            saves[DataKey][PlayerDataKey] = playerData.ToString(Formatting.Indented);

            File.WriteAllText(path, saves.ToString(Formatting.Indented));

            Debug.Log($"Обучение сброшено, снято отметок о показе: {shownCount}");
        }

        [MenuItem(MenuPath, true)]
        private static bool CanResetShownTutorials() => EditorApplication.isPlayingOrWillChangePlaymode == false;
    }
}
