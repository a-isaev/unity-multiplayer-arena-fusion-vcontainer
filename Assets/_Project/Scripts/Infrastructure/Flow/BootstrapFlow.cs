using System.Threading;
using CoinRush.Infrastructure.Config;
using CoinRush.Infrastructure.SceneManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace CoinRush.Infrastructure.Flow
{
    /// <summary>
    /// App start: global settings, then a check of the local config, then the menu.
    /// IAsyncStartable is a VContainer entry point: plain C# code that runs at Start time without a MonoBehaviour.
    /// </summary>
    public sealed class BootstrapFlow : IAsyncStartable
    {
        private readonly GameConfig _gameConfig;
        private readonly IPhotonConfigProvider _photonConfig;
        private readonly ISceneLoader _sceneLoader;
        private readonly ArenaAssets _arenaAssets;

        public BootstrapFlow(GameConfig gameConfig, IPhotonConfigProvider photonConfig, ISceneLoader sceneLoader, ArenaAssets arenaAssets)
        {
            _gameConfig = gameConfig;
            _photonConfig = photonConfig;
            _sceneLoader = sceneLoader;
            _arenaAssets = arenaAssets;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            Application.targetFrameRate = _gameConfig.TargetFrameRate;

            if (!_photonConfig.TryGet(out _))
                Debug.LogWarning("[Bootstrap] Photon config is missing. Online play will not work. " +
                                 "Run 'Coin Rush > Create Local Photon Config' and paste your Fusion App ID.");

            var overlay = new GameObject("Loading Overlay").AddComponent<LoadingOverlay>();
            await UniTask.Yield(cancellation);
            try
            {
                await _arenaAssets.LoadSurfaceAsync(cancellation);
            }
            catch (System.Exception exception) when (!cancellation.IsCancellationRequested)
            {
                overlay.Message = "Arena could not load. Restart Coin Rush to retry.";
                Debug.LogException(exception);
                return;
            }

            await _sceneLoader.LoadAsync(SceneNames.Menu);
        }
    }
}
