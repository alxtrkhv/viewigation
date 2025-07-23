using System.Threading;
using Cysharp.Threading.Tasks;
using Viewigation.Views;

namespace Viewigation.Routes
{
  public static class NavigationExtensions
  {
    public static UniTask Open<TRoute>(this TRoute route, bool animated = true,
      CancellationToken cancellation = default) where TRoute : IRoute
    {
      return route.Layer?.Open(route, animated, cancellation) ?? UniTask.CompletedTask;
    }

    public static UniTask Open<TRoute, TParameters>(this TRoute route, TParameters? parameters = default,
      bool animated = true, CancellationToken cancellation = default) where TRoute : IRoute<IView<TParameters>>
    {
      return route.Layer?.Open(route, parameters, animated, cancellation) ?? UniTask.CompletedTask;
    }

    public static UniTask Close<TRoute>(this TRoute route, bool animated = true, bool unload = false,
      CancellationToken cancellation = default) where TRoute : IRoute
    {
      return route.Layer?.Close<TRoute>(route, animated, unload, cancellation) ?? UniTask.CompletedTask;
    }

    public static void Unload<TRoute>(this TRoute route) where TRoute : IRoute
    {
      route.Layer?.Unload(route);
    }

    public static UniTask Close<TRoute>(this IViewWithRoute view) where TRoute : IRoute
    {
      return view.Route?.Close() ?? UniTask.CompletedTask;
    }
  }
}
