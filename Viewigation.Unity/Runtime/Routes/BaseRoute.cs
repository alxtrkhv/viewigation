using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Viewigation.Views;

namespace Viewigation.Routes
{
  public abstract class BaseRoute<TView> : IRoute<TView> where TView : IView
  {
    public RouteState State { get; private set; }
    public bool IsSuspended { get; private set; }

    public Type ViewType { get; } = typeof(TView);
    public RouteType RouteType { get; }
    public string? ViewKey { get; }

    public bool IsSuspensive { get; }

    INavigationLayer? IRoute.Layer { get; set; }

    public UniTask HiddenTask => _hiddenTcs?.Task ?? UniTask.CompletedTask;

    string? IRoute.Id { get; set; }

    private UniTaskCompletionSource? _hiddenTcs;

    internal BaseRoute(string? viewKey, RouteType routeType, bool isSuspensive = true)
    {
      ViewKey = viewKey;
      RouteType = routeType;
      IsSuspensive = isSuspensive;

      State = RouteState.Loading;
    }

    public TView? View { get; private set; }

    void IRoute<TView>.SetView(IView view)
    {
      View = view is TView validView ? validView : default;
    }

    void IWidget.Initialize()
    {
      if (View is IViewWithRoute viewWithRoute) {
        viewWithRoute.Route = this;
      }

      if (View != null) {
        try {
          View.Initialize();
        } catch (Exception e) {
          LogException(e);
        }
      }

      State = RouteState.Loaded;
    }

    void IWidget.Dispose()
    {
      State = RouteState.Disposed;

      if (View != null) {
        try {
          View.Dispose();
        } catch (Exception e) {
          LogException(e);
        }
      }

      if (View is IViewWithRoute viewWithRoute) {
        viewWithRoute.Route = null;
      }
    }

    async UniTask IView.Show(bool animated, CancellationToken cancellation)
    {
      State = RouteState.Opening;
      _hiddenTcs = new();

      if (View != null) {
        try {
          await View.Show(animated, cancellation);
        } catch (Exception e) {
          LogException(e);
        }
      }

      State = RouteState.Open;
    }

    async UniTask IView.Hide(bool animated, CancellationToken cancellation)
    {
      State = RouteState.Closing;
      if (View != null) {
        try {
          await View.Hide(animated, cancellation);
        } catch (Exception e) {
          LogException(e);
        }
      }

      State = RouteState.Closed;

      _hiddenTcs?.TrySetResult();
      _hiddenTcs = null;
    }

    void IWidget.Suspend()
    {
      if (View == null || IsSuspended) {
        return;
      }

      IsSuspended = true;
      try {
        View.Suspend();
      } catch (Exception e) {
        LogException(e);
      }
    }

    void IWidget.Resume()
    {
      if (View == null || !IsSuspended) {
        return;
      }

      IsSuspended = false;
      try {
        View.Resume();
      } catch (Exception e) {
        LogException(e);
      }
    }

    private static void LogException(Exception e)
    {
      Log.Error(e.Message);
    }
  }
}
