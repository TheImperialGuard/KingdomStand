using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Assets._Project.Develop.Runtime.Utilities.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Assets._Project.Develop.Editor.Localization
{
    // Видимые игроку русские строки без перевода: тексты TMP в префабах проекта, строковые поля вью интерфейса
    // (подписи-заглушки книги) и строковые поля конфигов, кроме заметок баланса и имён ярусов бесконечного. Строка без
    // записи в strings таблицы и не из exceptions — непереведённая. Перед сборкой — только предупреждение: сборка идёт
    // дальше, непереведённая строка остаётся русской.
    public class UntranslatedStringsCheck : IPreprocessBuildWithReport
    {
        private const string MenuPath = "Tools/Localization/Check Untranslated";
        private const string TablePath = "Assets/_Project/Resources/Localization/English.json";
        private const string ProjectFolder = "Assets/_Project";
        private const string ConfigsFolder = "Assets/_Project/Resources/Configs";
        private const string PrefabFilter = "t:Prefab";
        private const string ScriptableObjectFilter = "t:ScriptableObject";
        private const char PropertyPathSeparator = '.';
        private const char HierarchySeparator = '/';

        private static readonly string[] _interfaceFolders = { "Assets/_Project/Resources/UI", "Assets/_Project/Prefabs/UI" };
        private static readonly HashSet<string> _skippedConfigFields = new() { "_note", "_tiers" };
        private static readonly Regex _russianLetter = new("[А-Яа-яЁё]");

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Report();

        [MenuItem(MenuPath)]
        private static void Report()
        {
            Dictionary<string, List<string>> untranslated = FindUntranslated();

            if (untranslated.Count == 0)
            {
                Debug.Log("Непереведённых строк нет");
                return;
            }

            StringBuilder message = new();
            message.Append("Строки без перевода: ").Append(untranslated.Count).AppendLine();

            foreach (KeyValuePair<string, List<string>> entry in untranslated)
            {
                message.Append("«").Append(entry.Key).Append("» — ").AppendJoin("; ", entry.Value).AppendLine();
            }

            Debug.LogWarning(message.ToString());
        }

        private static Dictionary<string, List<string>> FindUntranslated()
        {
            TranslationTable table = new(File.ReadAllText(TablePath));
            Dictionary<string, List<string>> untranslated = new();

            foreach (string path in FindAssetPaths(PrefabFilter, ProjectFolder))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
                    Check(table, untranslated, text.text, path + ": " + GetHierarchyPath(text.transform));
            }

            foreach (string path in FindAssetPaths(PrefabFilter, _interfaceFolders))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    // Тексты TMP уже проверены выше; null — компонент с потерянным скриптом
                    if (component == null || component is TMP_Text)
                        continue;

                    string location = path + ": " + GetHierarchyPath(component.transform) + HierarchySeparator;
                    CheckStringFields(table, untranslated, component, location, isConfig: false);
                }
            }

            foreach (string path in FindAssetPaths(ScriptableObjectFilter, ConfigsFolder))
            {
                ScriptableObject config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (config != null)
                    CheckStringFields(table, untranslated, config, path + ": ", isConfig: true);
            }

            return untranslated;
        }

        private static IEnumerable<string> FindAssetPaths(string filter, params string[] folders)
        {
            foreach (string guid in AssetDatabase.FindAssets(filter, folders))
                yield return AssetDatabase.GUIDToAssetPath(guid);
        }

        private static void CheckStringFields(
            TranslationTable table,
            Dictionary<string, List<string>> untranslated,
            Object target,
            string location,
            bool isConfig)
        {
            SerializedObject serializedObject = new(target);
            SerializedProperty property = serializedObject.GetIterator();

            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.String)
                    continue;

                if (isConfig && IsSkippedConfigField(property.propertyPath))
                    continue;

                Check(table, untranslated, property.stringValue, location + property.propertyPath);
            }
        }

        private static bool IsSkippedConfigField(string propertyPath)
        {
            foreach (string part in propertyPath.Split(PropertyPathSeparator))
            {
                if (_skippedConfigFields.Contains(part))
                    return true;
            }

            return false;
        }

        private static void Check(
            TranslationTable table,
            Dictionary<string, List<string>> untranslated,
            string text,
            string location)
        {
            if (string.IsNullOrEmpty(text) || _russianLetter.IsMatch(text) == false)
                return;

            if (table.HasTranslation(text) || table.IsException(text))
                return;

            if (untranslated.TryGetValue(text, out List<string> locations) == false)
            {
                locations = new List<string>();
                untranslated.Add(text, locations);
            }

            locations.Add(location);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;

            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
                path = parent.name + HierarchySeparator + path;

            return path;
        }
    }
}
