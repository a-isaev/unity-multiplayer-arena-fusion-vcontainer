using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CoinRush.Infrastructure.Flow
{
    public sealed class ArenaAssets : IDisposable
    {
        private AsyncOperationHandle<Material> _surface;

        public async UniTask<Material> LoadSurfaceAsync(CancellationToken cancellation)
        {
            if (!_surface.IsValid())
                _surface = Addressables.LoadAssetAsync<Material>("arena/surface");

            await UniTask.WaitUntil(() => _surface.IsDone, cancellationToken: cancellation);
            if (_surface.Status == AsyncOperationStatus.Succeeded)
                return _surface.Result;

            var cause = _surface.OperationException;
            Dispose();
            throw new InvalidOperationException("Could not load the arena material.", cause);
        }

        public void Dispose()
        {
            if (_surface.IsValid())
                Addressables.Release(_surface);

            _surface = default;
        }
    }
}
