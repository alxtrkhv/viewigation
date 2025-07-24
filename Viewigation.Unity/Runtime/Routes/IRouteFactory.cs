using System.Threading;
using Cysharp.Threading.Tasks;
using Viewigation.Assets;
using Viewigation.Navigation;
using Viewigation.Views;

namespace Viewigation.Routes
{
  public interface IRouteFactory
  {
    public IAssets Assets { get; set; }

    public TRoute NewRoute<TRoute>(string? id) where TRoute : IRoute, new();

    public UniTask<IView?> NewView<TRoute>(TRoute route, INavigationLayerConfig layerConfig,
      CancellationToken cancellation = default)
      where TRoute : IRoute;

    public IView? ExistingView<TRoute>(TRoute route, INavigationLayerConfig layerConfig) where TRoute : IRoute;

    public void Release(string? viewKey);
  }
}
