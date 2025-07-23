using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Viewigation.Animations
{
  public class SetActiveUnityViewAnimation : IUnityViewAnimation
  {
    private readonly GameObject _gameObject;

    public SetActiveUnityViewAnimation(GameObject gameObject)
    {
      _gameObject = gameObject;
    }

    public void Initialize() { }

    public UniTask PlayShown(CancellationToken cancellation = default)
    {
      return ShowTask(_gameObject);
    }

    public UniTask PlayHidden(CancellationToken cancellation = default)
    {
      return HideTask(_gameObject);
    }

    public static UniTask ShowTask(GameObject gameObject)
    {
      gameObject.SetActive(true);

      return UniTask.CompletedTask;
    }

    public static UniTask HideTask(GameObject gameObject)
    {
      gameObject.SetActive(false);

      return UniTask.CompletedTask;
    }
  }
}
