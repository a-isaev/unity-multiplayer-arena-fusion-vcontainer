using UnityEngine;

namespace CoinRush.Infrastructure.Config
{
    /// <summary>
    /// Designer-tunable settings. It is registered in DI as an instance, so nothing reads it through a static field.
    /// Gameplay values (dash, coins) are added in stage 3.
    /// </summary>
    [CreateAssetMenu(menuName = "Coin Rush/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [field: SerializeField, Min(30)] public int TargetFrameRate { get; private set; } = 60;
        [field: SerializeField, Range(2, 4)] public int MaxPlayers { get; private set; } = 4;
        [field: SerializeField, Min(10)] public float RoundDurationSeconds { get; private set; } = 120f;
    }
}
