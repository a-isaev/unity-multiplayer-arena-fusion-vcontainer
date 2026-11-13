using UnityEngine;

namespace CoinRush.Infrastructure.Config
{
    /// <summary>
    /// Reads Settings/Photon/Resources/photon_config.json, which is listed in .gitignore.
    /// Resources is used on purpose: the file must be optional (a fresh clone has none),
    /// and a missing serialized reference would be harder to report clearly.
    /// Note: the App ID still ships inside the build. That is normal for Photon, because the App ID
    /// is a public client identifier. We keep it out of git so the repo cannot be used against our quota.
    /// </summary>
    public sealed class PhotonConfigProvider : IPhotonConfigProvider
    {
        public const string ResourcePath = "photon_config";

        private PhotonConfig _cached;
        private bool _loaded;

        public bool TryGet(out PhotonConfig config)
        {
            if (!_loaded)
            {
                _loaded = true;
                var asset = Resources.Load<TextAsset>(ResourcePath);
                if (asset != null)
                    _cached = JsonUtility.FromJson<PhotonConfig>(asset.text);
            }

            config = _cached;
            return config != null && config.IsValid;
        }
    }
}
