using VContainer;
using Viewigation.Navigation;
using Viewigation.Routes;

namespace Viewigation.VContainer
{
  public static class VContainerExtensions
  {
    public static void RegisterNavigationLayer(this IContainerBuilder builder, NavigationLayerConfig config)
    {
      builder.Register<INavigationLayer, NavigationLayer>(Lifetime.Scoped)
        .WithParameter(config.Self);
    }
  }
}
