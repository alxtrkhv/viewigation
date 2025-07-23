using System;
using VContainer.Unity;

namespace Viewigation.VContainer
{
  public interface ICustomScopeConsumer
  {
    public Type ScopeType { get; }
  }

  public interface ICustomScopeConsumer<TScope> : ICustomScopeConsumer where TScope : LifetimeScope
  {
    Type ICustomScopeConsumer.ScopeType => typeof(TScope);
  }
}
