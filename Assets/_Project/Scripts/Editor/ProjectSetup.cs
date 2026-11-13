using System.IO;
using System.Linq;
using CoinRush.Infrastructure.Config;
using CoinRush.Infrastructure.SceneManagement;
using CoinRush.Infrastructure.Scopes;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using VContainer.Unity;

namespace CoinRush.Editor
{
    /// <summary>
    /// Builds the project scaffold from code instead of manual editor clicks.
    /// It is idempotent: anything that already exists is left alone, so it is safe to run again.
    /// Batch mode: Unity -batchmode -quit -projectPath . -executeMethod CoinRush.Editor.ProjectSetup.Run
    /// </summary>
    public static class ProjectSetup
    {
        private const string Root = "Assets/_Project";
        private const string SettingsDir = Root + "/Settings";
        private const string ScenesDir = Root + "/Scenes";
        private const string InfraPrefabsDir = Root + "/Prefabs/Infrastructure";

        private const string GameConfigPath = SettingsDir + "/GameConfig.asset";
        private const string VContainerSettingsPath = SettingsDir + "/VContainerSettings.asset";
        private const string ProjectScopePrefabPath = InfraPrefabsDir + "/ProjectLifetimeScope.prefab";

        private const string PhotonExamplePath = SettingsDir + "/Photon/photon_config.example.json";
        private const string PhotonLocalPath = SettingsDir + "/Photon/Resources/" + PhotonConfigProvider.ResourcePath + ".json";

        [MenuItem("Coin Rush/Setup/Run Project Setup")]
        public static void Run()
        {
            var gameConfig = EnsureGameConfig();
            var projectScope = EnsureProjectScopePrefab(gameConfig);
            EnsureVContainerSettings(projectScope);

            var bootstrap = EnsureScene(SceneNames.Bootstrap, withCamera: false, scene => CreateScope<BootstrapLifetimeScope>());
            var menu = EnsureScene(SceneNames.Menu, withCamera: true, scene => CreateScope<MenuLifetimeScope>());
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(bootstrap, true),
                new EditorBuildSettingsScene(menu, true),
            };

            AddressableAssetSettingsDefaultObject.GetSettings(create: true);
            ConfigurePlayerSettings();

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Done.");
        }

        [MenuItem("Coin Rush/Create Local Photon Config")]
        public static void CreateLocalPhotonConfig()
        {
            if (File.Exists(PhotonLocalPath))
            {
                Debug.Log($"[ProjectSetup] {PhotonLocalPath} already exists.");
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PhotonLocalPath)!);
                File.Copy(PhotonExamplePath, PhotonLocalPath);
                AssetDatabase.ImportAsset(PhotonLocalPath);
                Debug.Log($"[ProjectSetup] Created {PhotonLocalPath}. Paste your Fusion App ID there (the file is git-ignored).");
            }

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(PhotonLocalPath);
        }

        private static GameConfig EnsureGameConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            if (config != null) return config;

            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, GameConfigPath);
            return config;
        }

        private static ProjectLifetimeScope EnsureProjectScopePrefab(GameConfig gameConfig)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ProjectLifetimeScope>(ProjectScopePrefabPath);
            if (existing != null) return existing;

            Directory.CreateDirectory(InfraPrefabsDir);
            var go = new GameObject("ProjectLifetimeScope");
            var scope = go.AddComponent<ProjectLifetimeScope>();

            // The field is private [SerializeField], so we set it through SerializedObject, the same path the Inspector uses.
            var so = new SerializedObject(scope);
            so.FindProperty("_gameConfig").objectReferenceValue = gameConfig;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ProjectScopePrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ProjectLifetimeScope>();
        }

        private static void EnsureVContainerSettings(LifetimeScope rootScope)
        {
            var settings = AssetDatabase.LoadAssetAtPath<VContainerSettings>(VContainerSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<VContainerSettings>();
                AssetDatabase.CreateAsset(settings, VContainerSettingsPath);
            }

            settings.RootLifetimeScope = rootScope;
            settings.RemoveClonePostfix = true;
            EditorUtility.SetDirty(settings);

            // VContainer finds the settings through Preloaded Assets. That is how the root scope gets created at startup.
            var preloaded = PlayerSettings.GetPreloadedAssets().Where(a => a != null && a is not VContainerSettings).ToList();
            preloaded.Add(settings);
            PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
        }

        private static string EnsureScene(string name, bool withCamera, System.Action<UnityEngine.SceneManagement.Scene> populate)
        {
            var path = $"{ScenesDir}/{name}.unity";
            if (File.Exists(path)) return path;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (withCamera)
            {
                var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
                cam.transform.SetPositionAndRotation(new Vector3(0, 10, -6), Quaternion.Euler(60, 0, 0));
                var light = new GameObject("Directional Light").AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            populate(scene);
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static void CreateScope<T>() where T : LifetimeScope =>
            new GameObject(typeof(T).Name).AddComponent<T>();

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "a-isaev";
            PlayerSettings.productName = "Coin Rush";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.aisaev.coinrush");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.aisaev.coinrush");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        }
    }
}
