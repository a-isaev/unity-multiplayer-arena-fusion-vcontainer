using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CoinRush.Infrastructure.SceneManagement
{
    public interface ISceneLoader
    {
        /// <summary>Loads a Build Settings scene in Single mode (replaces the current one).</summary>
        UniTask LoadAsync(string sceneName, IProgress<float> progress = null, CancellationToken cancellation = default);
    }
}
