using CoinRush.Infrastructure.Config;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace CoinRush.Infrastructure.Flow
{
    public sealed class MenuFlow : IAsyncStartable
    {
        private readonly GameConfig _gameConfig;
        private readonly IPhotonConfigProvider _photonConfig;

        private readonly ArenaAssets _arenaAssets;

        public MenuFlow(GameConfig gameConfig, IPhotonConfigProvider photonConfig, ArenaAssets arenaAssets)
        {
            _gameConfig = gameConfig;
            _photonConfig = photonConfig;
            _arenaAssets = arenaAssets;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            var surface = await _arenaAssets.LoadSurfaceAsync(cancellation);
            var root = new GameObject("Room Lobby");
            root.AddComponent<RoomLobby>().Initialize(_photonConfig, _gameConfig.MaxPlayers, _gameConfig.RoundDurationSeconds, surface);
        }
    }
}
