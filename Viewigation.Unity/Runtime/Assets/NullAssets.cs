using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Viewigation.Assets
{
  public class NullAssets : IAssets
  {
    public static NullAssets Instance => _instance ??= new();
    private static NullAssets? _instance;

    public UniTask Initialize(CancellationToken cancellation = default)
    {
      return UniTask.CompletedTask;
    }

    public UniTask<TAsset?> LoadAsync<TAsset>(string key, CancellationToken cancellation = default)
      where TAsset : Object
    {
      return UniTask.FromResult<TAsset?>(null);
    }

    public UniTask Refresh(CancellationToken cancellation = default)
    {
      return UniTask.CompletedTask;
    }

    public void ReleaseAsset(Object asset) { }
  }
}
