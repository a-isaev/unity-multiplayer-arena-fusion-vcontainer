using System;
using System.IO;
using CoinRush.Infrastructure.Config;
using UnityEditor;
using UnityEngine;

namespace CoinRush.Editor
{
    public sealed class PhotonSettingsWindow : EditorWindow
    {
        private const string ConfigPath =
            "Assets/_Project/Settings/Photon/Resources/" + PhotonConfigProvider.ResourcePath + ".json";

        [SerializeField] private string _appId = "";
        private string _message;
        private MessageType _messageType;

        [MenuItem("CoinRush/Photon Settings")]
        private static void Open()
        {
            GetWindow<PhotonSettingsWindow>("CoinRush Photon");
        }

        private void OnEnable()
        {
            minSize = new Vector2(340f, 130f);
            try
            {
                _appId = ReadConfig().AppIdFusion ?? "";
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                ShowError(exception.Message);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Photon Fusion", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                _appId = EditorGUILayout.TextField("Fusion App ID", _appId);
                using (new EditorGUI.DisabledScope(!TryParseAppId(_appId, out _)))
                {
                    if (GUILayout.Button("Update App ID"))
                        Save();
                }
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorGUILayout.HelpBox("Stop Play Mode to update the App ID.", MessageType.Info);
            else if (!string.IsNullOrEmpty(_message))
                EditorGUILayout.HelpBox(_message, _messageType);
            else
                EditorGUILayout.HelpBox("Enter a Fusion App ID from the Photon Dashboard.", MessageType.Info);
        }

        private void Save()
        {
            if (!TryParseAppId(_appId, out var appId))
                return;

            try
            {
                var exists = File.Exists(ConfigPath);
                var config = ReadConfig();
                _appId = appId.ToString("D");
                config.AppIdFusion = _appId;

                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                File.WriteAllText(ConfigPath, JsonUtility.ToJson(config, prettyPrint: true) + "\n");
                AssetDatabase.ImportAsset(ConfigPath, ImportAssetOptions.ForceSynchronousImport);

                _message = exists ? "App ID updated." : "Local Photon config created.";
                _messageType = MessageType.Info;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                ShowError(exception.Message);
            }
        }

        private static PhotonConfig ReadConfig()
        {
            if (!File.Exists(ConfigPath))
                return new PhotonConfig { FixedRegion = "" };

            return JsonUtility.FromJson<PhotonConfig>(File.ReadAllText(ConfigPath))
                ?? throw new ArgumentException("The existing Photon config is not valid JSON.");
        }

        private static bool TryParseAppId(string input, out Guid appId) =>
            Guid.TryParse(input, out appId) && appId != Guid.Empty;

        private void ShowError(string message)
        {
            _message = message;
            _messageType = MessageType.Error;
        }
    }
}
