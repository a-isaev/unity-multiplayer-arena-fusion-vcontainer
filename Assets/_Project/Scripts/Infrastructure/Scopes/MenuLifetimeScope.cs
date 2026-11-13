using CoinRush.Infrastructure.Flow;
using VContainer;
using VContainer.Unity;

namespace CoinRush.Infrastructure.Scopes
{
    /// <summary>
    /// Scope of the Menu scene. In stage 4 it gets the MVP registrations (views, presenters).
    /// For now it only checks that the parent (project) dependencies resolve.
    /// </summary>
    public sealed class MenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<MenuFlow>();
        }
    }
}
