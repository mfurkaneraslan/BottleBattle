using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BottleBattleEditor
{
    public static class BuildPreview
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string OutputDirectory = "Builds/WindowsPreview";
        private const string OutputPath = OutputDirectory + "/BottleBattlePreview.exe";

        [MenuItem("Bottle Battle/Build Windows Preview")]
        public static void BuildWindowsPreview()
        {
            Directory.CreateDirectory(OutputDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Windows preview build failed: {report.summary.result}");
            }

            Debug.Log(
                $"Windows preview is ready: {OutputPath} " +
                $"({report.summary.totalSize / 1024f / 1024f:0.0} MB)");
        }
    }
}
