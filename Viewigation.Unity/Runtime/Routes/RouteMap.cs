using System;
using System.Collections.Generic;

namespace Viewigation.Routes
{
  public class RouteMap
  {
    private readonly Dictionary<RouteKey, string> _map = new();

    public void Register<TRoute>(string layer, string? id = null) where TRoute : IRoute
    {
      var key = RouteKey.Create<TRoute>(id);

      _map[key] = layer;
    }

    public void Register(Type routeType, string layer, string? id = null)
    {
      var key = RouteKey.Create(routeType, id);

      _map[key] = layer;
    }

    public string? Layer<TRoute>(string? id = null) where TRoute : IRoute
    {
      var key = RouteKey.Create<TRoute>(id);

      return _map.GetValueOrDefault(key);
    }
  }

  public readonly struct RouteKey : IEquatable<RouteKey>
  {
    public readonly Type RouteType;
    public readonly string? RouteId;

    internal RouteKey(Type routeType, string? routeId = null)
    {
      RouteType = routeType;
      RouteId = routeId;
    }

    internal static RouteKey Create(Type routeType, string? routeId = null) => new(routeType, routeId);
    public static RouteKey Create<TRoute>(string? routeId = null) where TRoute : IRoute => new(typeof(TRoute), null);

    public bool Equals(RouteKey other)
    {
      return RouteType == other.RouteType && RouteId == other.RouteId;
    }

    public override bool Equals(object? obj)
    {
      return obj is RouteKey other && Equals(other);
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(RouteType, RouteId ?? string.Empty);
    }
  }
}
