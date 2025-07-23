#if VIEWIGATION_ADDRESSABLES
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Viewigation.Assets
{
  public class AddressableAssets : IAssets
  {
    public UniTask Initialize(CancellationToken cancellation = default)
    {
      return Addressables.InitializeAsync().ToUniTask(cancellationToken: cancellation);
    }

    public async UniTask<TObject?> LoadAsync<TObject>(string key, CancellationToken cancellation = default)
      where TObject : Object
    {
      try {
        var asset = await Addressables.LoadAssetAsync<TObject>(key).ToUniTask(cancellationToken: cancellation);
        return asset;
      } catch {
        return null;
      }
    }

    public async UniTask Refresh(CancellationToken cancellation = default)
    {
      await Addressables.UpdateCatalogs().ToUniTask(cancellationToken: cancellation);
    }

    public void ReleaseAsset(Object asset)
    {
      Addressables.Release(asset);
    }
  }
}
#endif
