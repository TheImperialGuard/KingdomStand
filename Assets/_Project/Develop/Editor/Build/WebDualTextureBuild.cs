using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using YG;

namespace Assets._Project.Develop.Editor.Build
{
    // Веб-сборка с двумя файлами данных: текстуры DXT для ПК и ETC2 для телефонов. Код у обеих сборок общий, отличается
    // только файл данных, поэтому файл ETC2 кладётся рядом с файлом DXT, а index.html выбирает нужный по поддержке
    // форматов видеокартой. Обработка после сборки идёт раньше PluginYG2: тот дорабатывает index.html, нумерует сборку
    // и упаковывает архив уже с обоими файлами. Запускается командами площадок — StoreBuild.
    public class WebDualTextureBuild : IPostprocessBuildWithReport
    {
        private const string DialogTitle = "Веб-сборка DXT + ETC2";
        private const string Etc2FolderSuffix = "_etc2";
        private const string BuildFolderName = "Build";
        private const string DataFileMarker = ".data";
        private const string IndexFileName = "index.html";
        private const string BuildLogFileName = "BuildLogYG2.txt";

        // У ProcessBuild из PluginYG2 порядок -1000: эта обработка должна пройти раньше.
        private const int BeforePluginYG2CallbackOrder = -1001;

        // Выбор файла данных: видеокарта понимает DXT (s3tc) — файл DXT; не понимает, но понимает ETC2 — файл ETC2;
        // иначе — DXT с программной распаковкой, как в обычной сборке. Выбранный файл пишется в консоль браузера.
        private const string DataFileSelectorTemplate = @"(function () {
                const gl = document.createElement(""canvas"").getContext(""webgl2"");
                let file = ""{dxt}"";
                if (gl != null && gl.getExtension(""WEBGL_compressed_texture_s3tc"") == null && gl.getExtension(""WEBGL_compressed_texture_etc"") != null)
                    file = ""{etc2}"";
                const loseContext = gl != null ? gl.getExtension(""WEBGL_lose_context"") : null;
                if (loseContext != null)
                    loseContext.loseContext();
                console.log(""Texture data file: "" + file);
                return file;
            })()";

        private static string _pendingEtc2DataPath;

        public int callbackOrder => BeforePluginYG2CallbackOrder;

        public static bool Build(string outputPath)
        {
            string etc2Path = outputPath + Etc2FolderSuffix;
            string[] scenes = GetEnabledScenes();
            bool archivingBuild = YG2.infoYG.Basic.archivingBuild;
            string buildLogPath = Path.Combine(InfoYG.PATCH_PC_EDITOR, BuildLogFileName);
            byte[] buildLog = File.Exists(buildLogPath) ? File.ReadAllBytes(buildLogPath) : null;

            try
            {
                // Сборка ETC2 — вспомогательная: из неё нужен только файл данных. Архив не нужен, номер сборки PluginYG2
                // возвращается, чтобы пара сборок считалась одной.
                YG2.infoYG.Basic.archivingBuild = false;
                BuildReport etc2Report = BuildPipeline.BuildPlayer(CreateOptions(scenes, etc2Path, WebGLTextureSubtarget.ETC2));
                RestoreBuildLog(buildLogPath, buildLog);

                if (etc2Report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[{DialogTitle}] Сборка ETC2 не удалась: {etc2Report.summary.result}. Сборка DXT не запускалась.");
                    return false;
                }

                _pendingEtc2DataPath = FindDataFile(etc2Path);
                YG2.infoYG.Basic.archivingBuild = archivingBuild;
                BuildReport dxtReport = BuildPipeline.BuildPlayer(CreateOptions(scenes, outputPath, WebGLTextureSubtarget.DXT));

                if (dxtReport.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[{DialogTitle}] Сборка DXT не удалась: {dxtReport.summary.result}.");
                    return false;
                }

                Debug.Log($"[{DialogTitle}] Готово: {outputPath}");
                return true;
            }
            finally
            {
                _pendingEtc2DataPath = null;
                YG2.infoYG.Basic.archivingBuild = archivingBuild;

                if (Directory.Exists(etc2Path))
                    FileUtil.DeleteFileOrDirectory(etc2Path);
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (string.IsNullOrEmpty(_pendingEtc2DataPath))
                return;

            string outputPath = report.summary.outputPath;
            string dxtDataName = Path.GetFileName(FindDataFile(outputPath));
            string etc2DataName = Path.GetFileName(_pendingEtc2DataPath);

            File.Copy(_pendingEtc2DataPath, Path.Combine(outputPath, BuildFolderName, etc2DataName), true);
            PatchIndex(Path.Combine(outputPath, IndexFileName), dxtDataName, etc2DataName);
        }

        private static string[] GetEnabledScenes()
        {
            List<string> scenes = new List<string>();

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    scenes.Add(scene.path);
            }

            return scenes.ToArray();
        }

        private static BuildPlayerOptions CreateOptions(string[] scenes, string path, WebGLTextureSubtarget subtarget)
        {
            return new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                subtarget = (int)subtarget,
                options = BuildOptions.None,
            };
        }

        private static void RestoreBuildLog(string path, byte[] content)
        {
            if (content != null)
            {
                File.WriteAllBytes(path, content);
                return;
            }

            if (File.Exists(path))
                File.Delete(path);
        }

        // Файл данных назван по папке сборки: YG → Build/YG.data.br (расширение после .data зависит от сжатия).
        private static string FindDataFile(string buildPath)
        {
            string buildName = Path.GetFileName(buildPath);
            string[] files = Directory.GetFiles(Path.Combine(buildPath, BuildFolderName), buildName + DataFileMarker + "*");

            if (files.Length != 1)
                throw new BuildFailedException($"[{DialogTitle}] В {buildPath} ожидался один файл данных {buildName}{DataFileMarker}*, найдено: {files.Length}.");

            return files[0];
        }

        private static void PatchIndex(string indexPath, string dxtDataName, string etc2DataName)
        {
            string index = File.ReadAllText(indexPath);
            string pattern = "dataUrl:\\s*buildUrl\\s*\\+\\s*\"/" + Regex.Escape(dxtDataName) + "\"";
            MatchCollection matches = Regex.Matches(index, pattern);

            if (matches.Count != 1)
                throw new BuildFailedException($"[{DialogTitle}] В {indexPath} ожидалась одна строка dataUrl с {dxtDataName}, найдено: {matches.Count}.");

            string selector = DataFileSelectorTemplate.Replace("{dxt}", dxtDataName).Replace("{etc2}", etc2DataName);
            index = index.Replace(matches[0].Value, "dataUrl: buildUrl + \"/\" + " + selector);
            File.WriteAllText(indexPath, index, new UTF8Encoding(false));
        }
    }
}
