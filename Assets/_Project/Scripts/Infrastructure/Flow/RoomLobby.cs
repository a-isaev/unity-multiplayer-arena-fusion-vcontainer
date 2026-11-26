using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoinRush.Gameplay;
using CoinRush.Infrastructure.Config;
using Fusion;
using Fusion.Photon.Realtime;
using Photon.Client;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinRush.Infrastructure.Flow
{
    public sealed class RoomLobby : MonoBehaviour
    {
        private static byte[] _participantToken;

        private IPhotonConfigProvider _config;
        private NetworkRunner _runner;
        private CancellationToken _lifetime;
        private int _maxPlayers;
        private float _roundDuration;
        private Material _surface;
        private bool _busy;
        private string _code = "";
        private string _status = "";

        public void Initialize(IPhotonConfigProvider config, int maxPlayers, float roundDuration, Material surface)
        {
            _participantToken ??= CreateParticipantToken();
            _config = config;
            _maxPlayers = maxPlayers;
            _roundDuration = roundDuration;
            _surface = surface;
            _lifetime = destroyCancellationToken;
        }

        private GUIStyle _heading;
        private GUIStyle _caption;
        private GUIStyle _entry;
        private GUIStyle _button;

        private void OnGUI()
        {
            if (_heading == null)
            {
                _heading = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
                _caption = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
                _caption.normal.textColor = new Color(0.73f, 0.83f, 0.89f);
                _entry = new GUIStyle(GUI.skin.textField)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter
                };
                _button = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
            }

            var connected = !_busy && _runner != null && _runner.IsRunning;
            var safe = Screen.safeArea;
            Rect panel;
            float scale;
            if (connected)
            {
                ArenaScreenLayout.GetRects(out _, out var room, out _, out scale);
                panel = new Rect(room.position / scale, room.size / scale);
            }
            else
            {
                const float height = 252f;
                scale = 1f;
                if (Application.isMobilePlatform)
                {
                    var preferred = Mathf.Clamp(Mathf.Min(safe.width, safe.height) / 480f, 1.25f, 2.5f);
                    scale = Mathf.Min(preferred, safe.width / 340f, safe.height / (height + 24f));
                }

                var width = Mathf.Min(400f, safe.width / scale - 24f);
                var top = (Screen.height - safe.yMax) / scale;
                var y = top + Mathf.Max(12f, (safe.height / scale - height) / 2f);
                panel = new Rect(safe.center.x / scale - width / 2f, y, width, height);
            }
            var previousMatrix = GUI.matrix;
            GUI.matrix = previousMatrix * Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var previousColor = GUI.color;
            GUI.color = new Color(0.07f, 0.12f, 0.16f, 0.97f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previousColor;

            GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 12f, panel.width - 32f, panel.height - 24f));
            if (connected)
                DrawRoom();
            else
                DrawMenu();

            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }

        private void DrawMenu()
        {
            GUILayout.Label("Coin Rush", _heading);
            if (_busy)
            {
                GUILayout.Label(_status, _caption);
                return;
            }

            GUILayout.Space(6f);
#if UNITY_WEBGL && !UNITY_EDITOR
            GUILayout.Label("Join a room hosted on desktop or Android.", _caption);
#else
            if (GUILayout.Button("Create room", _button, GUILayout.Height(38f)))
            {
                _code = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
                _ = ConnectAsync(GameMode.Host);
                return;
            }

#endif

            GUILayout.Space(8f);
            GUILayout.Label("Room code", _caption);
            _code = GUILayout.TextField(_code, 8, _entry, GUILayout.Height(34f)).ToUpperInvariant();
            using (new GuiEnabledScope(IsRoomCode(_code)))
            {
                if (GUILayout.Button("Join room", _button, GUILayout.Height(38f)))
                    _ = ConnectAsync(GameMode.Client);
            }

            GUILayout.Label(_status, _caption);
        }

        private void DrawRoom()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(_code, _heading);
            if (GUILayout.Button("Copy code", _button, GUILayout.Height(30f)))
                GUIUtility.systemCopyBuffer = _code;
            GUILayout.EndHorizontal();

            GUILayout.Label($"Players: {_runner.CommittedPlayers.Count()}/{_runner.SessionInfo.MaxPlayers}", _caption);
            if (!Application.isMobilePlatform)
                GUILayout.Label("WASD or arrows to move · Space to dash", _caption);

            if (_runner.TryGetPlayerObject(_runner.LocalPlayer, out var avatar))
            {
                var cooldown = avatar.GetComponent<ArenaPlayer>().DashCooldownLeft;
                GUILayout.Label(cooldown > 0f ? $"Dash ready in {cooldown:0.0}s" : "Dash ready", _caption);
            }

            if (GUILayout.Button("Leave room", _button, GUILayout.Height(30f)))
                _ = LeaveAsync();
        }

        private readonly struct GuiEnabledScope : IDisposable
        {
            private readonly bool _previous;

            public GuiEnabledScope(bool enabled)
            {
                _previous = GUI.enabled;
                GUI.enabled = _previous && enabled;
            }

            public void Dispose() => GUI.enabled = _previous;
        }

        private async Task ConnectAsync(GameMode mode)
        {
            if (!_config.TryGet(out var config))
            {
                _status = "Photon App ID is missing.";
                return;
            }

            _busy = true;
            _status = "Connecting…";
            var runnerObject = new GameObject("Room Network Runner");
            var runner = runnerObject.AddComponent<NetworkRunner>();
            _runner = runner;

            try
            {
                var arena = runnerObject.AddComponent<ArenaSession>();
                var scene = new NetworkSceneInfo();
                scene.AddSceneRef(SceneRef.FromIndex(gameObject.scene.buildIndex), LoadSceneMode.Additive);
                var result = await runner.StartGame(new StartGameArgs
                {
                    GameMode = mode,
                    Config = arena.Configure(runner, _roundDuration, _surface),
                    Scene = scene,
                    SceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>(),
                    SessionName = _code,
                    ConnectionToken = _participantToken,
                    PlayerCount = _maxPlayers,
                    IsVisible = false,
                    EnableClientSessionCreation = false,
                    StartGameCancellationToken = _lifetime,
                    CustomPhotonAppSettings = new FusionAppSettings
                    {
                        Protocol = ConnectionProtocol.WebSocketSecure,
                        AppVersion = "arena-4",
                        AppIdFusion = config.AppIdFusion.Trim(),
                        FixedRegion = string.IsNullOrWhiteSpace(config.FixedRegion) ? "eu" : config.FixedRegion.Trim()
                    }
                });

                if (_lifetime.IsCancellationRequested)
                    return;

                if (result.Ok)
                {
                    arena.ShowArena();
                    _status = "";
                }
                else
                {
                    _status = result.ShutdownReason switch
                    {
                        ShutdownReason.GameNotFound => "Room not found. Check the code and game version.",
                        ShutdownReason.ConnectionRefused => "Join refused. If you refreshed, wait for your old connection to close and try again.",
                        _ => $"Connection failed: {result.ShutdownReason}"
                    };
                    _runner = null;
                    Destroy(runnerObject);
                }
            }
            catch (Exception exception)
            {
                if (!_lifetime.IsCancellationRequested)
                {
                    _status = "Could not connect to Photon.";
                    Debug.LogException(exception);
                    _runner = null;
                    Destroy(runnerObject);
                }
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task LeaveAsync()
        {
            _busy = true;
            _status = "Leaving room…";
            var runner = _runner;
            _runner = null;
            try
            {
                await runner.Shutdown();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (runner != null)
                    Destroy(runner.gameObject);

                _status = "";
                _busy = false;
            }
        }

        private void OnDestroy()
        {
            if (_runner != null)
                Destroy(_runner.gameObject);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int CoinRush_GetOrStoreToken(string candidate, [System.Runtime.InteropServices.Out] byte[] output, int capacity);
#endif

        private static byte[] CreateParticipantToken()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // sessionStorage writes synchronously and survives refreshes of this tab.
            var buffer = new byte[33];
            if (CoinRush_GetOrStoreToken(Guid.NewGuid().ToString("N"), buffer, buffer.Length) != 0 &&
                Guid.TryParseExact(System.Text.Encoding.ASCII.GetString(buffer, 0, 32), "N", out var tabToken))
                return tabToken.ToByteArray();
#endif
            var args = Environment.GetCommandLineArgs();
            var profileArg = Array.IndexOf(args, "-coinrush-profile");
            var profile = profileArg >= 0 && profileArg + 1 < args.Length ? args[profileArg + 1] : "";
            if (string.IsNullOrWhiteSpace(profile) && !Application.isMobilePlatform &&
                Application.platform != RuntimePlatform.WebGLPlayer)
                return Guid.NewGuid().ToByteArray();

            var key = "coinrush-participant-" + profile;
            if (!Guid.TryParse(PlayerPrefs.GetString(key), out var token))
            {
                token = Guid.NewGuid();
                PlayerPrefs.SetString(key, token.ToString("N"));
                PlayerPrefs.Save();
            }

            return token.ToByteArray();
        }

        private static bool IsRoomCode(string code)
        {
            if (code.Length != 8)
                return false;

            foreach (var character in code)
            {
                if (character is not (>= '0' and <= '9' or >= 'A' and <= 'F'))
                    return false;
            }

            return true;
        }
    }
}
