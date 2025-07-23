using System;
using System.Collections.Generic;

namespace Viewigation.Routes
{
  public interface INavigation : IRouter, IReadOnlyCollection<INavigationLayer>, IDisposable
  {
    public void Initialize();

    public bool AddLayer(INavigationLayer layer);
    public bool RemoveLayer(string key);

    public INavigationLayer Layer(string layerName);
    public INavigationLayer this[string layer] { get; }

    public void RegisterOnLayer<TRoute>(string layer, string? id = null) where TRoute : IRoute =>
      RegisterOnLayer(typeof(TRoute), layer, id);

    public void RegisterOnLayer(params (RouteKey route, string layer)[] records)
    {
      foreach (var record in records) {
        RegisterOnLayer(record.route.RouteType, record.layer, record.route.RouteId);
      }
    }

    protected void RegisterOnLayer(Type routeType, string layer, string? id = null);

    public static NavigationBuildingContext Builder() => new();
  }
}
