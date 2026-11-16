using UnityEditor;
using UnityEngine;

namespace Assets._Project.Develop.Editor.Localization
{
    // Язык игры в редакторе (Р-05): пункты меню пишут Simulation.language настроек PluginYG2, откуда язык берёт
    // YGLanguageSource. Язык читается один раз при старте игры, поэтому в Play Mode пункты недоступны.
    public static class EditorLanguageMenu
    {
        private const string RussianMenuPath = "Tools/Localization/Language/Русский";
        private const string EnglishMenuPath = "Tools/Localization/Language/English";
        private const string SettingsPath = "Assets/PluginYourGames/Resources/SettingsYG2.asset";
        private const string LanguageProperty = "Simulation.language";
        private const string RussianCode = "ru";
        private const string EnglishCode = "en";

        [MenuItem(RussianMenuPath)]
        private static void SelectRussian() => SetLanguage(RussianCode);

        [MenuItem(EnglishMenuPath)]
        private static void SelectEnglish() => SetLanguage(EnglishCode);

        [MenuItem(RussianMenuPath, true)]
        private static bool CanSelectRussian() => UpdateMenu();

        [MenuItem(EnglishMenuPath, true)]
        private static bool CanSelectEnglish() => UpdateMenu();

        private static bool UpdateMenu()
        {
            string language = GetLanguageProperty().stringValue;

            Menu.SetChecked(RussianMenuPath, language == RussianCode);
            Menu.SetChecked(EnglishMenuPath, language == EnglishCode);

            return EditorApplication.isPlayingOrWillChangePlaymode == false;
        }

        private static void SetLanguage(string languageCode)
        {
            SerializedProperty language = GetLanguageProperty();

            language.stringValue = languageCode;
            language.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(language.serializedObject.targetObject);

            Debug.Log($"Язык игры в редакторе: {languageCode}");
        }

        private static SerializedProperty GetLanguageProperty()
        {
            Object settings = AssetDatabase.LoadMainAssetAtPath(SettingsPath);

            return new SerializedObject(settings).FindProperty(LanguageProperty);
        }
    }
}
