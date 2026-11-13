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

        public BootstrapFlow(GameConfig gameConfig, IPhotonConfigProvider photonConfig, ISceneLoader sceneLoader)
        {
            _gameConfig = gameConfig;
            _photonConfig = photonConfig;
            _sceneLoader = sceneLoader;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            Application.targetFrameRate = _gameConfig.TargetFrameRate;

            if (!_photonConfig.TryGet(out _))
                Debug.LogWarning("[Bootstrap] Photon config is missing. Online play will not work. " +
                                 "Run 'Coin Rush > Create Local Photon Config' and paste your Fusion App ID.");

            // Stage 4 will add async preloading here (Addressables), still before the menu.
            await UniTask.Yield(cancellation);

            // We do NOT pass `cancellation` here. Loading Menu unloads Bootstrap, which disposes this scope
            // and cancels its token. The await would then throw OperationCanceledException
            // even though the load itself succeeded.
            await _sceneLoader.LoadAsync(SceneNames.Menu);
        }
    }
}
