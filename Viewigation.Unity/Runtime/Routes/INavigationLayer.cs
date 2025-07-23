using System;
using System.Collections.Generic;

namespace Viewigation.Routes
{
  public interface INavigationLayer : IRouter, IDisposable
  {
    string Id { get; }
    IReadOnlyList<IRoute> Stack { get; }

    bool IsValid => true;

    public void Initialize(INavigation? navigation = null);

    public void Suspend(object? actor = null);
    public void Resume(object? actor = null);


  }
}
