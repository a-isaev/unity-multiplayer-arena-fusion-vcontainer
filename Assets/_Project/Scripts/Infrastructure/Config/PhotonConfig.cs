using System;

namespace CoinRush.Infrastructure.Config
{
    /// <summary>
    /// Photon settings loaded from a local JSON file that is not committed (see photon_config.example.json).
    /// Fields are public because JsonUtility only deserializes public or [SerializeField] fields.
    /// </summary>
    [Serializable]
    public sealed class PhotonConfig
    {
        public string AppIdFusion;
        public string FixedRegion;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(AppIdFusion) &&
            !AppIdFusion.StartsWith("PASTE-", StringComparison.Ordinal);
    }
}
