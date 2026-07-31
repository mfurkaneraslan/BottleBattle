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
        private const string WebOutputDirectory = "Builds/WebGL";
        private const string WebPublishDirectory = "docs";

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

        [MenuItem("Bottle Battle/Build WebGL Preview")]
        public static void BuildWebGLPreview()
        {
            Directory.CreateDirectory(WebOutputDirectory);

            // GitHub Pages cannot attach Unity's Brotli/Gzip response headers when
            // serving ordinary repository files, so publish an uncompressed build.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = WebOutputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"WebGL preview build failed: {report.summary.result}");
            }

            File.WriteAllText(Path.Combine(WebOutputDirectory, ".nojekyll"), string.Empty);
            PrepareWebTemplate();
            CopyDirectory(WebOutputDirectory, WebPublishDirectory);
            Debug.Log(
                $"WebGL preview is ready and copied to {WebPublishDirectory}: " +
                $"({report.summary.totalSize / 1024f / 1024f:0.0} MB)");
        }

        private static void PrepareWebTemplate()
        {
            string indexPath = Path.Combine(WebOutputDirectory, "index.html");
            string index = File.ReadAllText(indexPath);
            string cacheKey = System.DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            index = index.Replace(
                "<title>Unity Web Player | Bottle Battle</title>",
                "<meta name=\"viewport\" content=\"width=device-width, height=device-height, initial-scale=1.0, user-scalable=no\">\n" +
                "    <meta name=\"theme-color\" content=\"#fff8e8\">\n" +
                "    <meta name=\"description\" content=\"Swap colorful bottles, match the hidden order, and earn three stars.\">\n" +
                "    <meta property=\"og:title\" content=\"Bottle Battle\">\n" +
                "    <meta property=\"og:description\" content=\"Can you solve every bottle order in the minimum number of moves?\">\n" +
                "    <meta property=\"og:type\" content=\"website\">\n" +
                "    <meta property=\"og:image\" content=\"https://mfurkaneraslan.github.io/BottleBattle/og.png\">\n" +
                "    <meta name=\"twitter:card\" content=\"summary_large_image\">\n" +
                "    <title>Bottle Battle</title>");
            index = index.Replace(
                "var loaderUrl = buildUrl + \"/WebGL.loader.js\";",
                $"var loaderUrl = buildUrl + \"/WebGL.loader.js?v={cacheKey}\";");
            index = index.Replace(
                "dataUrl: buildUrl + \"/WebGL.data\",",
                $"dataUrl: buildUrl + \"/WebGL.data?v={cacheKey}\",");
            index = index.Replace(
                "frameworkUrl: buildUrl + \"/WebGL.framework.js\",",
                $"frameworkUrl: buildUrl + \"/WebGL.framework.js?v={cacheKey}\",");
            index = index.Replace(
                "codeUrl: buildUrl + \"/WebGL.wasm\",",
                $"codeUrl: buildUrl + \"/WebGL.wasm?v={cacheKey}\",");
            File.WriteAllText(indexPath, index);

            string stylePath = Path.Combine(WebOutputDirectory, "TemplateData", "style.css");
            string style = File.ReadAllText(stylePath);
            style = style.Replace(
                "#unity-container.unity-desktop { left: 50%; top: 50%; transform: translate(-50%, -50%) }",
                "#unity-container.unity-desktop { position: fixed; inset: 0; display: grid; place-items: center; background: #fff8e8 }");
            style = style.Replace(
                "#unity-canvas { background: #231F20 }",
                "#unity-canvas { background: #fff8e8 }\n" +
                ".unity-desktop #unity-canvas { width: min(100vw, 56.25vh) !important; height: min(100vh, 177.7778vw) !important }");
            style = style.Replace("#unity-footer { position: relative }", "#unity-footer { display: none }");
            File.WriteAllText(stylePath, style);
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);
            foreach (string sourceFile in Directory.GetFiles(sourceDirectory))
            {
                File.Copy(
                    sourceFile,
                    Path.Combine(destinationDirectory, Path.GetFileName(sourceFile)),
                    true);
            }

            foreach (string sourceSubdirectory in Directory.GetDirectories(sourceDirectory))
            {
                CopyDirectory(
                    sourceSubdirectory,
                    Path.Combine(destinationDirectory, Path.GetFileName(sourceSubdirectory)));
            }
        }
    }
}
