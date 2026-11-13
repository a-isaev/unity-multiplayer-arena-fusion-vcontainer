using CoinRush.Infrastructure.Config;
using CoinRush.Infrastructure.SceneManagement;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CoinRush.Infrastructure.Scopes
{
    /// <summary>
    /// Root DI container for the whole application.
    /// VContainerSettings (in Preloaded Assets) creates it from a prefab before the first scene loads
    /// and marks it DontDestroyOnLoad. Every scene LifetimeScope without an explicit parent becomes its child,
    /// so it can resolve everything registered here.
    /// Lifetime.Singleton means "one instance per container", not the Singleton pattern:
    /// there is no static access, and consumers get the object through their constructor.
    /// </summary>
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameConfig _gameConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_gameConfig);
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
            builder.Register<IPhotonConfigProvider, PhotonConfigProvider>(Lifetime.Singleton);
        }
    }
}
