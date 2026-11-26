using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoinRush.Gameplay
{
    public sealed class ArenaSession : MonoBehaviour, INetworkRunnerCallbacks
    {
        private static readonly NetworkObjectGuid PlayerGuid =
            NetworkObjectGuid.Parse("5595e1e8-55fc-45e8-bc18-a0f82086d454");

        private static readonly NetworkObjectGuid RoundGuid =
            NetworkObjectGuid.Parse("4aa1bb55-fc59-4ebc-b9d4-e329068d3443");

        private NetworkPrefabId _playerPrefab;
        private NetworkPrefabId _roundPrefab;
        private ArenaRound _round;
        private float _roundDuration;
        private Material _surfaceMaterial;
        private Camera _arenaCamera;
        private VolumeProfile _bloomProfile;
        private bool _dashPressed;
        private bool _arenaVisible;
        private readonly ArenaTouchControls _touch = new();

        public NetworkProjectConfig Configure(NetworkRunner runner, float roundDuration, Material surface)
        {
            _roundDuration = roundDuration;
            _surfaceMaterial = surface;

            var templateRoot = new GameObject("Player Template");
            templateRoot.transform.SetParent(transform, false);

            var avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            avatar.name = "Arena Player";
            avatar.GetComponent<Renderer>().sharedMaterial = _surfaceMaterial;
            avatar.transform.SetParent(templateRoot.transform, false);
            avatar.transform.localScale = Vector3.one * 0.75f;
            var networkObject = avatar.AddComponent<NetworkObject>();
            avatar.AddComponent<NetworkTransform>();
            avatar.AddComponent<ArenaPlayer>();
            new NetworkObjectBaker().Bake(avatar);

            var roundObject = new GameObject("Arena Round");
            roundObject.transform.SetParent(templateRoot.transform, false);
            var networkRound = roundObject.AddComponent<NetworkObject>();
            roundObject.AddComponent<ArenaRound>().SetSurface(surface);
            new NetworkObjectBaker().Bake(roundObject);
            templateRoot.SetActive(false);

            var config = NetworkProjectConfig.Deserialize(NetworkProjectConfig.Serialize(NetworkProjectConfig.Global));
            config.PrefabTable = new NetworkPrefabTable();
            foreach (var source in NetworkProjectConfig.Global.PrefabTable.Prefabs)
                config.PrefabTable.AddSource(source);

            _playerPrefab = config.PrefabTable.AddSource(new NetworkPrefabSourceStatic
            {
                AssetGuid = PlayerGuid,
                Object = networkObject
            });

            _roundPrefab = config.PrefabTable.AddSource(new NetworkPrefabSourceStatic
            {
                AssetGuid = RoundGuid,
                Object = networkRound
            });

            runner.AddCallbacks(this);
            runner.ProvideInput = true;
            return config;
        }

        public void ShowArena()
        {
            _arenaVisible = true;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Arena Floor";
            floor.transform.SetParent(transform, false);
            floor.transform.localPosition = Vector3.down * 0.2f;
            floor.transform.localScale = new Vector3(15f, 0.4f, 15f);
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", new Color(0.24f, 0.32f, 0.38f));
            var floorRenderer = floor.GetComponent<Renderer>();
            floorRenderer.sharedMaterial = _surfaceMaterial;
            floorRenderer.SetPropertyBlock(block);

            var cameraObject = new GameObject("Arena Camera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 22f, 0f), Quaternion.Euler(90f, 0f, 0f));
            var camera = cameraObject.AddComponent<Camera>();
            _arenaCamera = camera;
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.depth = 10f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.13f, 0.16f);
            FitArenaCamera();
            if (Application.isMobilePlatform)
            {
                camera.allowHDR = false;
                return;
            }

            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            _bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = _bloomProfile.Add<Bloom>();
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0.45f);
            bloom.scatter.Override(0.55f);
            var volume = cameraObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = _bloomProfile;
        }

        private void LateUpdate()
        {
            if (_arenaCamera != null)
                FitArenaCamera();
        }

        private void FitArenaCamera()
        {
            ArenaScreenLayout.GetRects(out var field, out _, out _, out _);
            // Keep the camera full-screen and place its field inside the unobstructed area.
            var fieldSize = Mathf.Max(1f, Mathf.Min(field.width, field.height));
            var worldPerPixel = 16f / fieldSize;
            _arenaCamera.orthographicSize = Screen.height * worldPerPixel * 0.5f;
            var offset = field.center - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            _arenaCamera.transform.position = new Vector3(-offset.x * worldPerPixel, 22f, offset.y * worldPerPixel);
        }

        private void OnDestroy()
        {
            if (_bloomProfile == null)
                return;

            foreach (var component in _bloomProfile.components)
                Destroy(component);

            Destroy(_bloomProfile);
        }

        private void OnGUI()
        {
            if (_arenaVisible && Application.isMobilePlatform)
                _touch.Draw();
        }

        private void Update()
        {
            if (_arenaVisible && Application.isMobilePlatform)
                _touch.Update();

            if (Application.isFocused && Keyboard.current?.spaceKey.wasPressedThisFrame == true)
                _dashPressed = true;
            else if (!Application.isFocused)
                _dashPressed = false;
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var move = Vector2.zero;
            var buttons = new NetworkButtons();
            var keyboard = Keyboard.current;
            if (Application.isFocused && keyboard != null)
            {
                move.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                move.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            }

            var touchDash = _touch.ConsumeDash();
            if (Application.isFocused)
            {
                if (_touch.Move != Vector2.zero)
                    move = _touch.Move;

                buttons.Set(ArenaInput.DashButton, touchDash || _dashPressed ||
                    keyboard?.spaceKey.isPressed == true);
            }

            _dashPressed = false;
            input.Set(new ArenaInput { Move = move, Buttons = buttons });
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer || runner.TryGetPlayerObject(player, out _))
                return;

            var token = runner.GetPlayerConnectionToken(player);
            if (_round != null && _round.IsRunning && !_round.CanReconnect(token))
            {
                runner.Disconnect(player);
                return;
            }

            var angle = player.PlayerId * (Mathf.PI / 2f);
            var position = new Vector3(4f * Mathf.Cos(angle), 0.75f, 4f * Mathf.Sin(angle));
            var avatar = runner.Spawn(_playerPrefab, position, Quaternion.identity, player);
            runner.SetPlayerObject(player, avatar);
            if (_round != null)
                _round.Reconnect(player, token);
            if (_round == null)
            {
                var match = runner.Spawn(_roundPrefab, Vector3.zero, Quaternion.identity,
                    onBeforeSpawned: (_, instance) => instance.GetComponent<ArenaRound>().SetDuration(_roundDuration));
                _round = match.GetComponent<ArenaRound>();
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer)
                return;

            _round?.Disconnect(player);
            if (runner.TryGetPlayerObject(player, out var avatar))
                runner.Despawn(avatar);
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            if (token?.Length != 16 || (_round != null && _round.IsRunning && !_round.CanReconnect(token)))
            {
                request.Refuse();
                return;
            }

            request.Accept();
        }
        public void OnConnectFailed(NetworkRunner runner, NetAddress address, NetConnectFailedReason reason) { }
        [Obsolete]
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessions) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    }
}
