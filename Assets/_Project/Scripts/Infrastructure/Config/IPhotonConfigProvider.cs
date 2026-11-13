namespace CoinRush.Infrastructure.Config
{
    public interface IPhotonConfigProvider
    {
        /// <summary>False if the local config is missing or still holds the template value.</summary>
        bool TryGet(out PhotonConfig config);
    }
}
