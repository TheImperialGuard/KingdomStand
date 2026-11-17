using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using YG;
using YG.EditorScr;
using YG.Insides;

namespace Assets._Project.Develop.Editor.Build
{
    // Веб-сборка на площадку: площадка — это платформа PluginYG2 и папка сборки. Если платформа площадки в плагине не
    // выбрана, команда выбирает её тем же путём, что окно плагина, ждёт перекомпиляции под символ платформы, собирает
    // и возвращает прежнюю платформу со строками символов как были (плагин дописал бы символ в конец списка).
    // Состояние между перезагрузками домена лежит в файле в Library: если редактор упадёт во время сборки, платформа
    // вернётся при следующем запуске.
    //
    // Первая перезагрузка домена после смены платформы иногда возвращает .asmdef в состояние прежней платформы (это
    // внутри PluginYG2: так же бывает и при смене в окне плагина). Сборка, начатая в таком состоянии, стартует
    // с незакомпилированными изменениями, Unity записывает ассеты URP в раскладке редактора, и сборка падает при
    // загрузке. Поэтому перед сборкой и после возврата состояние сверяется, при расхождении выправляется процедурой
    // самого плагина и проверяется ещё раз после новой перезагрузки домена.
    [InitializeOnLoad]
    public static class StoreBuild
    {
        private const string MenuRoot = "Tools/Build/";
        private const string DialogTitle = "Сборка на площадку";
        private const string StateFilePath = "Library/StoreBuildState.json";
        private const string PlatformsFolder = "Assets/PluginYourGames/Platforms";
        private const string PlatformAssetExtension = ".asset";
        private const string PlatformCodeFolderName = "SDK";
        private const string PlatformNameSuffix = "Platform";
        private const string PlatformSymbolSuffix = "_yg";
        private const string AsmdefExtension = ".asmdef";
        private const char DefinesSeparator = ';';
        private const int MaxStabilizeAttempts = 3;

        private static readonly Store YandexGames = new Store("Yandex Games", "YandexGames", null);
        private static readonly Store Itch = new Store("itch.io", "EmptyWebGL", "Itch");

        // Продолжение — по EditorApplication.update, а не delayCall: delayCall не вызывается, пока окно редактора
        // не в фокусе, и сборка стояла бы до возвращения в редактор.
        static StoreBuild()
        {
            if (File.Exists(StateFilePath))
                EditorApplication.update += ContinueAfterReload;
        }

        [MenuItem(MenuRoot + "Yandex Games")]
        private static void BuildYandexGames()
        {
            StartBuild(YandexGames);
        }

        [MenuItem(MenuRoot + "itch.io")]
        private static void BuildItch()
        {
            StartBuild(Itch);
        }

        private static void StartBuild(Store store)
        {
            if (File.Exists(StateFilePath))
            {
                EditorUtility.DisplayDialog(DialogTitle, $"Предыдущая сборка не завершила возврат платформы ({StateFilePath}). Дождитесь перекомпиляции.", "OK");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                EditorUtility.DisplayDialog(DialogTitle, "Активная платформа — не Web. Переключите её в Build Profiles.", "OK");
                return;
            }

            string buildLocation = EditorUserBuildSettings.GetBuildLocation(BuildTarget.WebGL);

            if (string.IsNullOrEmpty(buildLocation))
            {
                EditorUtility.DisplayDialog(DialogTitle, "Папка веб-сборки не задана. Задайте её обычной сборкой в Build Profiles.", "OK");
                return;
            }

            PlatformSettings currentPlatform = InfoYG.Inst().Basic.platform;
            PlatformSettings storePlatform = LoadPlatform(store.PlatformFolderName);

            if (currentPlatform == null || storePlatform == null)
            {
                EditorUtility.DisplayDialog(DialogTitle, $"Не найдена платформа PluginYG2: текущая или {store.PlatformFolderName}.", "OK");
                return;
            }

            if (IsPlatformStateValid() == false)
            {
                EditorUtility.DisplayDialog(DialogTitle, $"Платформа PluginYG2 {currentPlatform.name} не в согласованном состоянии: символ платформы или файлы .asmdef в {PlatformsFolder}. Выберите платформу заново в окне плагина и дождитесь перекомпиляции.", "OK");
                return;
            }

            string outputPath = store.GetOutputPath(buildLocation);
            bool mustSwitchPlatform = currentPlatform != storePlatform;
            string switchNote = mustSwitchPlatform
                ? $"Плагин переключится на {storePlatform.name}, после сборки вернётся {currentPlatform.name}.\n\n"
                : string.Empty;
            string message = $"{store.Title}: две сборки подряд — ETC2, затем DXT — в папку:\n{outputPath}\n\n{switchNote}Это займёт несколько десятков минут.";

            if (EditorUtility.DisplayDialog(DialogTitle, message, "Собрать", "Отмена") == false)
                return;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
                return;

            int buildNumber = InfoYG.Inst().Basic.buildNumber;

            if (mustSwitchPlatform == false)
            {
                try
                {
                    WebDualTextureBuild.Build(outputPath);
                }
                finally
                {
                    RestoreBuildNumber(buildNumber);
                }

                return;
            }

            BuildState state = new BuildState
            {
                Phase = BuildPhase.Build,
                StoreTitle = store.Title,
                OutputPath = outputPath,
                OriginalPlatformPath = AssetDatabase.GetAssetPath(currentPlatform),
                OriginalBuildNumber = buildNumber,
                Defines = CaptureDefines(),
            };

            WriteState(state);
            Debug.Log($"[{DialogTitle}] {store.Title}: переключение на {storePlatform.name}, сборка начнётся после перекомпиляции.");
            SelectPlatform(storePlatform);
        }

        private static void ContinueAfterReload()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            EditorApplication.update -= ContinueAfterReload;
            BuildState state = ReadState();

            switch (state.Phase)
            {
                case BuildPhase.Build:
                    ContinueBuild(state);
                    break;

                case BuildPhase.Restore:
                    Debug.LogWarning($"[{DialogTitle}] {state.StoreTitle}: сборка прервалась, возвращаю платформу.");
                    RestorePlatform(state);
                    break;

                case BuildPhase.Verify:
                    VerifyRestore(state);
                    break;
            }
        }

        private static void ContinueBuild(BuildState state)
        {
            if (IsPlatformStateValid())
            {
                BuildAndRestore(state);
                return;
            }

            if (state.StabilizeAttempts >= MaxStabilizeAttempts)
            {
                Debug.LogError($"[{DialogTitle}] {state.StoreTitle}: платформа {PlatformSettings.currentPlatformFullName} не пришла в согласованное состояние, сборка отменена.");
                RestorePlatform(state);
                return;
            }

            Debug.LogWarning($"[{DialogTitle}] {state.StoreTitle}: после перекомпиляции символ или .asmdef платформы не сходятся, выправляю и жду перезагрузки.");
            StabilizePlatform(state);
        }

        private static void BuildAndRestore(BuildState state)
        {
            // Если сборка уронит редактор, при следующем запуске фаза Restore вернёт платформу.
            state.Phase = BuildPhase.Restore;
            WriteState(state);
            bool isBuilt = false;

            try
            {
                if (EditorUtility.scriptCompilationFailed)
                    Debug.LogError($"[{DialogTitle}] {state.StoreTitle}: ошибки компиляции после переключения платформы, сборка не запускалась.");
                else
                    isBuilt = WebDualTextureBuild.Build(state.OutputPath);

                if (isBuilt && IsPlatformStateValid() == false)
                {
                    Debug.LogError($"[{DialogTitle}] {state.StoreTitle}: во время сборки символ или .asmdef платформы изменились — сборка могла записаться с незакомпилированными изменениями и падать при загрузке. Соберите заново.");
                    isBuilt = false;
                }
            }
            finally
            {
                RestorePlatform(state);
            }

            if (isBuilt)
                Debug.Log($"[{DialogTitle}] {state.StoreTitle}: готово, {state.OutputPath}");
        }

        private static void RestorePlatform(BuildState state)
        {
            PlatformSettings originalPlatform = AssetDatabase.LoadAssetAtPath<PlatformSettings>(state.OriginalPlatformPath);
            state.Phase = BuildPhase.Verify;
            state.StabilizeAttempts = 0;
            WriteState(state);
            InfoYG.Inst().Basic.buildNumber = state.OriginalBuildNumber;
            SelectPlatform(originalPlatform);
            RestoreDefines(state.Defines);
            AssetDatabase.SaveAssets();
        }

        private static void VerifyRestore(BuildState state)
        {
            bool isDefinesChanged = RestoreDefines(state.Defines);

            if (isDefinesChanged == false && IsPlatformStateValid())
            {
                File.Delete(StateFilePath);
                Debug.Log($"[{DialogTitle}] {state.StoreTitle}: платформа {PlatformSettings.currentPlatformFullName} возвращена.");
                return;
            }

            if (state.StabilizeAttempts >= MaxStabilizeAttempts)
            {
                File.Delete(StateFilePath);
                Debug.LogError($"[{DialogTitle}] {state.StoreTitle}: платформа {PlatformSettings.currentPlatformFullName} не пришла в согласованное состояние. Проверьте символы и файлы .asmdef в {PlatformsFolder}.");
                return;
            }

            Debug.LogWarning($"[{DialogTitle}] {state.StoreTitle}: после возврата символы или .asmdef платформы не сходятся, выправляю и жду перезагрузки.");
            StabilizePlatform(state);
        }

        // Процедура самого плагина: символ выбранной платформы и .asmdef всех платформ по выбранной. Проверка — после
        // следующей перезагрузки домена; перезагрузка запрашивается явно, даже если компилировать нечего.
        private static void StabilizePlatform(BuildState state)
        {
            state.StabilizeAttempts++;
            WriteState(state);
            DefineSymbols.PlatformDefineSymbols();

            if (state.Phase == BuildPhase.Verify)
                RestoreDefines(state.Defines);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.RequestScriptReload();
        }

        private static void RestoreBuildNumber(int buildNumber)
        {
            InfoYG info = InfoYG.Inst();
            info.Basic.buildNumber = buildNumber;
            EditorUtility.SetDirty(info);
            AssetDatabase.SaveAssetIfDirty(info);
        }

        // Тот же порядок, что у окна плагина при смене платформы, плюс символы сразу, а не по изменению проекта.
        private static void SelectPlatform(PlatformSettings platform)
        {
            InfoYG info = InfoYG.Inst();
            info.Basic.platform = platform;
            EditorUtility.SetDirty(info);
            InfoYG.SetPlatform(PlatformSettings.currentPlatformFullName);

            if (info.Basic.autoApplySettings)
                platform.ApplyProjectSettings();

            PlatformSettings.SelectPlatform();
            DefineSymbols.PlatformDefineSymbols();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();
        }

        private static PlatformSettings LoadPlatform(string folderName)
        {
            string path = $"{PlatformsFolder}/{folderName}/{folderName}{PlatformAssetExtension}";
            return AssetDatabase.LoadAssetAtPath<PlatformSettings>(path);
        }

        // Согласованное состояние: символ выбранной платформы есть в символах веб-сборки, у выбранной платформы нет
        // .asmdef, а у остальных платформ с папкой кода он есть — через него плагин убирает их код из сборки.
        private static bool IsPlatformStateValid()
        {
            string currentPlatformName = PlatformSettings.currentPlatformFullName;
            string platformSymbol = currentPlatformName + PlatformSymbolSuffix;
            string[] webDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL).Split(DefinesSeparator);

            if (Array.Exists(webDefines, define => define == platformSymbol) == false)
                return false;

            foreach (string platformFolder in Directory.GetDirectories(PlatformsFolder))
            {
                string codeFolder = Path.Combine(platformFolder, PlatformCodeFolderName);

                if (Directory.Exists(codeFolder) == false)
                    continue;

                string platformName = Path.GetFileName(platformFolder) + PlatformNameSuffix;
                bool hasAsmdef = File.Exists(Path.Combine(codeFolder, platformName + AsmdefExtension));
                bool mustHaveAsmdef = platformName != currentPlatformName;

                if (hasAsmdef != mustHaveAsmdef)
                    return false;
            }

            return true;
        }

        private static List<GroupDefines> CaptureDefines()
        {
            List<GroupDefines> groupsDefines = new List<GroupDefines>();
            HashSet<string> targetNames = new HashSet<string>();

            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown || TryGetNamedTarget(group, out NamedBuildTarget target) == false)
                    continue;

                if (targetNames.Add(target.TargetName) == false)
                    continue;

                groupsDefines.Add(new GroupDefines
                {
                    Group = group,
                    Defines = PlayerSettings.GetScriptingDefineSymbols(target),
                });
            }

            return groupsDefines;
        }

        private static bool RestoreDefines(List<GroupDefines> groupsDefines)
        {
            bool isChanged = false;

            foreach (GroupDefines groupDefines in groupsDefines)
            {
                NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(groupDefines.Group);

                if (PlayerSettings.GetScriptingDefineSymbols(target) == groupDefines.Defines)
                    continue;

                PlayerSettings.SetScriptingDefineSymbols(target, groupDefines.Defines);
                isChanged = true;
            }

            return isChanged;
        }

        private static bool TryGetNamedTarget(BuildTargetGroup group, out NamedBuildTarget target)
        {
            try
            {
                target = NamedBuildTarget.FromBuildTargetGroup(group);
                return true;
            }
            catch (ArgumentException)
            {
                target = default;
                return false;
            }
        }

        private static void WriteState(BuildState state)
        {
            File.WriteAllText(StateFilePath, JsonUtility.ToJson(state, true));
        }

        private static BuildState ReadState()
        {
            return JsonUtility.FromJson<BuildState>(File.ReadAllText(StateFilePath));
        }

        private enum BuildPhase
        {
            Build,
            Restore,
            Verify,
        }

        [Serializable]
        private class BuildState
        {
            public BuildPhase Phase;
            public string StoreTitle;
            public string OutputPath;
            public string OriginalPlatformPath;
            public int OriginalBuildNumber;
            public int StabilizeAttempts;
            public List<GroupDefines> Defines;
        }

        [Serializable]
        private class GroupDefines
        {
            public BuildTargetGroup Group;
            public string Defines;
        }

        // BuildFolderName == null — папка веб-сборки из Build Profiles; иначе соседняя папка с этим именем.
        // Имена файлов сборки повторяют имя папки (Itch → Build/Itch.data.br).
        private class Store
        {
            public readonly string Title;
            public readonly string PlatformFolderName;

            private readonly string _buildFolderName;

            public Store(string title, string platformFolderName, string buildFolderName)
            {
                Title = title;
                PlatformFolderName = platformFolderName;
                _buildFolderName = buildFolderName;
            }

            public string GetOutputPath(string buildLocation)
            {
                if (_buildFolderName == null)
                    return buildLocation;

                return Path.Combine(Path.GetDirectoryName(buildLocation), _buildFolderName).Replace('\\', '/');
            }
        }
    }
}
