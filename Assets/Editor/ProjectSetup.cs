using System.IO;
using BottleBattle;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BottleBattleEditor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string SceneDirectory = "Assets/Scenes";
        private const string ScenePath = SceneDirectory + "/MainMenu.unity";
        private const string SetupKey = "BottleBattle.ProjectSetup.v1";

        static ProjectSetup()
        {
            EditorApplication.delayCall += SetupIfNeeded;
        }

        [MenuItem("Bottle Battle/Set Up Project Again")]
        public static void ForceSetup()
        {
            SessionState.EraseBool(SetupKey);
            SetupIfNeeded();
        }

        private static void SetupIfNeeded()
        {
            if (SessionState.GetBool(SetupKey, false))
            {
                return;
            }

            SessionState.SetBool(SetupKey, true);
            ConfigurePlayer();
            EnsureMainMenuScene();
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Bottle Battle Studio";
            PlayerSettings.productName = "Bottle Battle";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android,
                "com.bottlebattle.game");
        }

        private static void EnsureMainMenuScene()
        {
            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            if (!File.Exists(ScenePath))
            {
                Scene scene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);

                var root = new GameObject("Main Menu");
                root.AddComponent<MainMenuController>();

                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Debug.Log("Bottle Battle main menu scene is ready.");
        }
    }
}
