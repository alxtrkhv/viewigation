using System.Threading;
using Cysharp.Threading.Tasks;

namespace Viewigation.Animations
{
  public interface IUnityViewAnimation
  {
    public void Initialize();

    UniTask PlayShown(CancellationToken cancellation = default);
    UniTask PlayHidden(CancellationToken cancellation = default);
  }
}
