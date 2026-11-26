using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CoinRush.Editor
{
    internal static class CoinRushFirstOpen
    {
        private const string BootstrapPath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string MarkerPath = "Library/CoinRushFirstOpen";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (!UnityEngine.Application.isBatchMode)
                EditorApplication.delayCall += OpenBootstrapOnce;
        }

        private static void OpenBootstrapOnce()
        {
            // Remove the override used by the earlier Play Mode setup.
            var bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapPath);
            if (bootstrap != null && EditorSceneManager.playModeStartScene == bootstrap)
                EditorSceneManager.playModeStartScene = null;

            if (bootstrap == null || File.Exists(MarkerPath) || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                    return;
            }

            if (SceneManager.GetActiveScene().path != BootstrapPath)
                EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);

            File.WriteAllText(MarkerPath, "");
        }
    }
}
