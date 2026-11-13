using CoinRush.Infrastructure.Flow;
using VContainer;
using VContainer.Unity;

namespace CoinRush.Infrastructure.Scopes
{
    /// <summary>Scope of the Bootstrap scene. Its only job is to run BootstrapFlow.</summary>
    public sealed class BootstrapLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BootstrapFlow>();
        }
    }
}
