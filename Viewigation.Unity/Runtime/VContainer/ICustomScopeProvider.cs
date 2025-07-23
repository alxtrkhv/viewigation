using UnityEngine;
using VContainer.Unity;

namespace Viewigation.VContainer
{
  public interface ICustomScopeProvider : ICustomScopeConsumer
  {
    LifetimeScope? Scope { get; }
  }

  public interface ICustomScopeProvider<TScope> : ICustomScopeConsumer<TScope>, ICustomScopeProvider
    where TScope : LifetimeScope
  {
    LifetimeScope? ICustomScopeProvider.Scope
    {
      get
      {
        var monoBehaviour = this as MonoBehaviour;
        if (monoBehaviour == null) {
          return null;
        }

        return monoBehaviour.GetComponent<TScope>();
      }
    }
  }
}
