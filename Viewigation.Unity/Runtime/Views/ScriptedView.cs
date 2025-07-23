using System.Threading;
using Cysharp.Threading.Tasks;
using Viewigation.ReactiveObjects;
using Viewigation.Routes;

namespace Viewigation.Views
{
  public abstract class ScriptedView : IViewWithRoute
  {
    protected ReactiveBinder LifetimeBinder => _lifetimeBinder ??= new ReactiveBinder();
    private ReactiveBinder? _lifetimeBinder;

    protected ReactiveBinder ShowBinder => _showBinder ??= new ReactiveBinder();
    private ReactiveBinder? _showBinder;

    IRoute? IViewWithRoute.Route
    {
      get => _route;
      set => _route = value;
    }

    protected IRoute? _route;

    void IWidget.Initialize()
    {
      OnInitialized();
    }

    void IWidget.Dispose()
    {
      OnDisposed();
      _lifetimeBinder?.Flush();
    }

    UniTask IView.Show(bool animated, CancellationToken cancellation)
    {
      return OnShown(animated, cancellation);
    }

    async UniTask IView.Hide(bool animated, CancellationToken cancellation)
    {
      await OnHidden(animated, cancellation);
      _showBinder?.Flush();
    }

    void IWidget.Suspend()
    {
      OnSuspended(true);
    }

    void IWidget.Resume()
    {
      OnSuspended(false);
    }

    protected virtual void OnInitialized() { }
    protected virtual void OnDisposed() { }
    protected virtual UniTask OnShown(bool animated, CancellationToken cancellation = default) => UniTask.CompletedTask;

    protected virtual UniTask OnHidden(bool animated, CancellationToken cancellation = default) =>
      UniTask.CompletedTask;

    protected virtual void OnSuspended(bool value) { }
  }
}
