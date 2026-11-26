using CoinRush.Infrastructure.Flow;
using VContainer;
using VContainer.Unity;

namespace CoinRush.Infrastructure.Scopes
{
    public sealed class MenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<MenuFlow>();
        }
    }
}
