using System;
using Cysharp.Threading.Tasks;
using Viewigation.Views;

namespace Viewigation.Routes
{
  public interface IRoute : IView
  {
    public RouteState State { get; }
    public bool IsSuspended { get; }

    public Type ViewType { get; }
    public RouteType RouteType { get; }
    public string? ViewKey { get; }

    public bool IsSuspensive { get; }

    public string? Id { get; internal set; }
    public INavigationLayer? Layer { get; internal set; }

    public UniTask HiddenTask { get; }
  }

  public interface IRoute<out TView> : IRoute where TView : IView
  {
    public TView? View { get; }
    internal void SetView(IView view);
  }
}
