using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Viewigation.Assets
{
  public interface IAssets
  {
    public UniTask Initialize(CancellationToken cancellation = default);
    public UniTask<TAsset?> LoadAsync<TAsset>(string key, CancellationToken cancellation = default) where TAsset : Object;
    public UniTask Refresh(CancellationToken cancellation = default);
    public void ReleaseAsset(Object asset);
  }
}
