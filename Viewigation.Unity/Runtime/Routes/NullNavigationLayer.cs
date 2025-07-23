using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Viewigation.Views;

namespace Viewigation.Routes
{
  public class NullNavigationLayer : INavigationLayer
  {
    public string Id { get; }
    public IReadOnlyList<IRoute> Stack { get; } = new List<IRoute>(0);

    public bool IsValid => false;

    public NullNavigationLayer(string id)
    {
      Id = id;
    }

    public void Initialize(INavigation? navigation = null)
    {
      LogOutput();
    }

    public void Dispose() { }

    public void Suspend(object? actor = null)
    {
      LogOutput();
    }

    public void SuspendAndForget()
    {
      LogOutput();
    }

    public void ResumeAndForget()
    {
      LogOutput();
    }

    public TRoute? LoadedRoute<TRoute>(string? id = null) where TRoute : IRoute<IView>
    {
      LogOutput();
      return default;
    }

    public void Resume(object? actor = null)
    {
      LogOutput();
    }

    public UniTask<TRoute?> Load<TRoute>(string? id = null, bool tryPreload = false,
      CancellationToken cancellation = default)
      where TRoute : IRoute<IView>, new()
    {
      return LogOutput<TRoute>();
    }

    public void Unload<TRoute>(string? id = null, bool force = false) where TRoute : IRoute<IView>
    {
      LogOutput();
    }

    public void Unload<TRoute>(TRoute? route, bool force = false) where TRoute : IRoute
    {
      LogOutput();
    }

    public UniTask<TRoute?> Open<TRoute>(string? id, bool animated = true, CancellationToken cancellation = default)
      where TRoute : IRoute<IView>, new()
    {
      return LogOutput<TRoute>();
    }

    public UniTask Open<TRoute>(TRoute? route, bool animated = true, CancellationToken cancellation = default)
      where TRoute : IRoute
    {
      return LogOutput();
    }

    public UniTask<TRoute?> Open<TRoute, TParameters>(string? id, TParameters? parameters = default,
      bool animated = true, CancellationToken cancellation = default)
      where TRoute : IRoute<IView<TParameters>>, new()
    {
      return LogOutput<TRoute, TParameters>();
    }

    public UniTask Open<TRoute, TParameters>(TRoute? route, TParameters? parameters = default, bool animated = true,
      CancellationToken cancellation = default) where TRoute : IRoute<IView<TParameters>>
    {
      return LogOutput();
    }

    public UniTask<TRoute?> Close<TRoute>(string? id, bool animated = true,
      bool? unload = false,
      CancellationToken cancellation = default) where TRoute : IRoute<IView>
    {
      return LogOutput<TRoute>();
    }

    public UniTask Close<TRoute>(IRoute? route, bool animated = true, bool? unload = false,
      CancellationToken cancellation = default) where TRoute : IRoute
    {
      return LogOutput();
    }

    private UniTask<TRoute?> LogOutput<TRoute, TParameters>() where TRoute : IRoute<IView<TParameters>>
    {
      Log.Debug($"{nameof(NavigationLayer)} with id '{Id}' has not been registered.");

      return new(default);
    }

    private UniTask<TRoute?> LogOutput<TRoute>() where TRoute : IRoute<IView>
    {
      Log.Debug($"{nameof(NavigationLayer)} with id '{Id}' has not been registered.");

      return new(default);
    }

    private UniTask LogOutput()
    {
      Log.Debug($"{nameof(NavigationLayer)} with id '{Id}' has not been registered.");

      return UniTask.CompletedTask;
    }
  }
}
