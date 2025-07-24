using System.Threading;
using Cysharp.Threading.Tasks;
using Viewigation.Views;

namespace Viewigation.Routes
{
  public interface IRouter
  {
    public TRoute? LoadedRoute<TRoute>(string? id = null) where TRoute : IRoute<IView>;

    public UniTask<TRoute?> Load<TRoute>(string? id = null, bool tryFindLooseView = false,
      CancellationToken cancellation = default) where TRoute : IRoute<IView>, new();

    public void Unload<TRoute>(string? id = null, bool force = false) where TRoute : IRoute<IView>;

    public void Unload<TRoute>(TRoute? route, bool force = false) where TRoute : IRoute;

    public UniTask<TRoute?> Open<TRoute>(string? id = null, bool animated = true,
      CancellationToken cancellation = default) where TRoute : IRoute<IView>, new();

    public UniTask Open<TRoute>(TRoute? route, bool animated = true, CancellationToken cancellation = default) where TRoute : IRoute;

    public UniTask<TRoute?> Open<TRoute, TParameters>(string? id = null, TParameters? parameters = default,
      bool animated = true, CancellationToken cancellation = default) where TRoute : IRoute<IView<TParameters>>, new();

    public UniTask Open<TRoute, TParameters>(TRoute? route, TParameters? parameters = default,
      bool animated = true, CancellationToken cancellation = default) where TRoute : IRoute<IView<TParameters>>;

    public UniTask<TRoute?> Close<TRoute>(string? id = null, bool animated = true,
      bool? unload = null,
      CancellationToken cancellation = default) where TRoute : IRoute<IView>;

    public UniTask Close<TRoute>(IRoute? route, bool animated = true, bool? unload = null,
      CancellationToken cancellation = default) where TRoute : IRoute;
  }
}
