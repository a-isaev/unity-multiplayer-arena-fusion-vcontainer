using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace CoinRush.Infrastructure.SceneManagement
{
    /// <summary>
    /// Plain C# service that wraps SceneManager. It lives in ProjectLifetimeScope,
    /// so it survives scene changes and any scene scope can get it injected.
    /// </summary>
    public sealed class SceneLoader : ISceneLoader
    {
        public async UniTask LoadAsync(string sceneName, IProgress<float> progress = null, CancellationToken cancellation = default)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
                throw new InvalidOperationException($"Scene '{sceneName}' is not in Build Settings.");

            // The cancellation token only stops our await. Unity cannot abort a scene load that has started.
            await operation.ToUniTask(progress: progress, cancellationToken: cancellation);
        }
    }
}
