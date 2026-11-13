using CoinRush.Infrastructure.Config;
using UnityEngine;
using VContainer.Unity;

namespace CoinRush.Infrastructure.Flow
{
    /// <summary>Temporary stub. Stage 4 replaces it with MainMenuPresenter.</summary>
    public sealed class MenuFlow : IStartable
    {
        private readonly GameConfig _gameConfig;

        public MenuFlow(GameConfig gameConfig) => _gameConfig = gameConfig;

        public void Start() =>
            Debug.Log($"[Menu] Ready. Parent scope resolved GameConfig: round {_gameConfig.RoundDurationSeconds}s, " +
                      $"max {_gameConfig.MaxPlayers} players.");
    }
}
