using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Viewigation.Animations
{
  public abstract class BaseUnityViewAnimation : MonoBehaviour, IUnityViewAnimation
  {
    public virtual void Initialize() { }
    public virtual UniTask PlayShown(CancellationToken cancellation = default) => UniTask.CompletedTask;
    public virtual UniTask PlayHidden(CancellationToken cancellation = default) => UniTask.CompletedTask;
  }
}
