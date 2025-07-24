using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cysharp.Threading.Tasks;
using Viewigation.Blocks;
using Viewigation.Routes;
using Viewigation.Views;

namespace Viewigation.Navigation
{
  public class NavigationLayer : INavigationLayer
  {
    private readonly INavigationLayerConfig _layerConfig;
    private readonly IRouteFactory _routeFactory;

    private readonly List<IRoute> _loadedRoutes;
    private readonly List<IRoute> _activeStack;

    public IReadOnlyList<IRoute> Stack => _activeStack;
    public string Id => _layerConfig.Id;

    private readonly Block _suspensionBlock = new(true);
    private readonly List<INavigationLayer> _dependantLayers = new();

    public NavigationLayer(INavigationLayerConfig layerConfig, IRouteFactory routeFactory)
    {
      _layerConfig = layerConfig;
      _routeFactory = routeFactory;

      _loadedRoutes = _layerConfig.LoadedRoutes;
      _activeStack = _layerConfig.ActiveRoutes;
    }

    void INavigationLayer.Initialize(INavigation? navigation)
    {
      if (navigation is null) {
        Log.Debug($"Initializing navigation layer {Id} without parent Navigation.");
      }

      if (string.IsNullOrWhiteSpace(_layerConfig.Id)) {
        Log.Error($"{nameof(NavigationLayerConfig)}.{nameof(NavigationLayerConfig.Id)} is invalid on some layer.");
      }

      if (navigation is null) {
        return;
      }

      _dependantLayers.Clear();

      foreach (var dependentLayer in _layerConfig.DependentLayers) {
        var layer = navigation[dependentLayer];
        _dependantLayers.Add(layer);
      }
    }

    public void Dispose()
    {
      for (var i = _loadedRoutes.Count - 1; i >= 0; i--) {
        var route = _loadedRoutes[i];
        Unload(route, force: true);
      }

      _activeStack.Clear();
    }

    void INavigationLayer.Suspend(object? actor)
    {
      SuspendAndResume(true, actor);
    }

    void INavigationLayer.Resume(object? actor)
    {
      SuspendAndResume(false, actor);
    }

    public TRoute? LoadedRoute<TRoute>(string? id) where TRoute : IRoute<IView>
    {
      TryGetExistingRoute<TRoute>(id, out var route);

      return route;
    }

    public async UniTask<TRoute?> Load<TRoute>(string? id, bool tryFindLooseView = false,
      CancellationToken cancellation = default) where TRoute : IRoute<IView>, new()
    {
      if (TryGetExistingRoute<TRoute>(id, out var route)) {
        return route;
      }

      var newRoute = _routeFactory.NewRoute<TRoute>(id);

      var view = tryFindLooseView || newRoute.ViewKey is null
        ? _routeFactory.ExistingView(newRoute, _layerConfig)
        : null;

      if (view == null) {
        view = await _routeFactory.NewView(newRoute, _layerConfig, cancellation);
      }

      if (view == null) {
        Log.Error($"View for \"{typeof(TRoute).Name}\" route cannot be loaded or does not exist.");
        return default;
      }

      _loadedRoutes.Add(newRoute);
      newRoute.SetView(view);
      newRoute.Layer = this;
      newRoute.Initialize();

      route = newRoute;

      return route;
    }

    public void Unload<TRoute>(string? id, bool force = false) where TRoute : IRoute<IView>
    {
      if (!TryGetExistingRoute<TRoute>(id, out var route)) {
        Log.Warning($"There is no \"{typeof(TRoute).Name}\" route to unload.");
        return;
      }

      Unload(route, force);
    }

    public void Unload<TRoute>(TRoute? route, bool force = false) where TRoute : IRoute
    {
      if (route == null) {
        Log.Warning("There is no route to unload.");
        return;
      }

      if (route.Layer?.Id != Id) {
        Log.Warning("Route does not belong to this layer.");
        return;
      }

      if (!force && route.State != RouteState.Loaded && route.State != RouteState.Closed) {
        Log.Error("Route is not ready for unloading.");
        return;
      }

      _loadedRoutes.Remove(route);

      route.Layer = null;
      route.Dispose();
      _routeFactory.Release(route.ViewKey);
    }

    public async UniTask<TRoute?> Open<TRoute>(string? id, bool animated = true,
      CancellationToken cancellation = default)
      where TRoute : IRoute<IView>, new()
    {
      _layerConfig.InputBlocker?.Increment(null);

      var route = await Load<TRoute>(id, cancellation: cancellation);

      await Open(route, animated, cancellation);

      _layerConfig.InputBlocker?.Decrement(null);

      return route;
    }

    public async UniTask Open<TRoute>(TRoute? route, bool animated = true, CancellationToken cancellation = default)
      where TRoute : IRoute
    {
      _layerConfig.InputBlocker?.Increment(null);

      if (!CanRouteBeOpen(route)) {
        _layerConfig.InputBlocker?.Decrement(null);
        return;
      }

      _activeStack.Add(route);

      SuspendAndResume();
      await route.Show(animated, cancellation);

      _layerConfig.InputBlocker?.Decrement(null);
    }

    public async UniTask<TRoute?> Open<TRoute, TParameters>(string? id, TParameters? parameters,
      bool animated, CancellationToken cancellation = default) where TRoute : IRoute<IView<TParameters>>, new()
    {
      _layerConfig.InputBlocker?.Increment(null);

      var route = await Load<TRoute>(id, cancellation: cancellation);

      await Open(route, parameters, animated, cancellation);

      _layerConfig.InputBlocker?.Decrement(null);

      return route;
    }

    public async UniTask Open<TRoute, TParameters>(TRoute? route, TParameters? parameters = default,
      bool animated = true,
      CancellationToken cancellation = default) where TRoute : IRoute<IView<TParameters>>
    {
      _layerConfig.InputBlocker?.Increment(null);

      route?.View?.SetParameters(parameters);
      await Open(route, animated, cancellation);

      _layerConfig.InputBlocker?.Decrement(null);
    }

    public async UniTask<TRoute?> Close<TRoute>(string? id, bool animated = true,
      bool? unload = null,
      CancellationToken cancellation = default) where TRoute : IRoute<IView>
    {
      _layerConfig.InputBlocker?.Increment(null);

      TryGetExistingRoute<TRoute>(id, out var route);

      await Close<TRoute>(route, animated: animated, unload: unload, cancellation: cancellation);

      _layerConfig.InputBlocker?.Decrement(null);

      return unload ?? _layerConfig.UnloadViewOnClose ? default : route;
    }

    public async UniTask Close<TRoute>(IRoute? route, bool animated = true, bool? unload = null,
      CancellationToken cancellation = default) where TRoute : IRoute
    {
      _layerConfig.InputBlocker?.Increment(null);

      if (!CanRouteBeClosed(route)) {
        _layerConfig.InputBlocker?.Decrement(null);
        return;
      }

      _activeStack.Remove(route);

      await route.Hide(animated, cancellation);

      ((route as IRoute<IView>)?.View as IResetParameters)?.ResetParameters();

      SuspendAndResume();

      _layerConfig.InputBlocker?.Decrement(null);

      if (unload ?? _layerConfig.UnloadViewOnClose) {
        Unload(route);
      }
    }

    private bool CanRouteBeOpen<TRoute>([NotNullWhen(true)] TRoute? route) where TRoute : IRoute
    {
      if (route == null) {
        Log.Warning("There is no route to open.");
        return false;
      }

      if (route.Layer?.Id != Id) {
        Log.Warning("Route does not belong to this layer.");
        return false;
      }

      switch (route.State) {
        case RouteState.None:
        case RouteState.Loaded:
        case RouteState.Closed:
          return true;
        default:
          Log.Warning($"\"{typeof(TRoute).Name}\" route can not be opened in state \"{route.State}\".");
          return false;
      }
    }

    private bool CanRouteBeClosed([NotNullWhen(true)] IRoute? route)
    {
      if (route == null) {
        Log.Warning("There is no route to close.");
        return false;
      }

      if (route.Layer?.Id != Id) {
        Log.Warning("Route does not belong to this layer.");
        return false;
      }

      switch (route.State) {
        case RouteState.Open:
          return true;
        default:
          Log.Warning($"\"{route.GetType().Name}\" route can not be closed in state \"{route.State}\".");
          return false;
      }
    }

    private bool TryGetExistingRoute<TRoute>(string? id, [NotNullWhen(true)] out TRoute? route)
      where TRoute : IRoute
    {
      foreach (var existingView in _loadedRoutes) {
        if (existingView is not TRoute existingRoute || existingRoute.Id != id) {
          continue;
        }

        route = existingRoute;
        return true;
      }

      route = default;
      return false;
    }

    private void SuspendAndResume(bool layerSuspended = false, object? actor = null)
    {
      actor ??= this;
      ProcessLayer(this, ref layerSuspended);

      foreach (var dependantLayer in _dependantLayers) {
        if (layerSuspended) {
          dependantLayer.Suspend(actor);
        } else {
          dependantLayer.Resume(actor);
        }
      }

      void ProcessLayer(NavigationLayer layer, ref bool isOverlapped)
      {
        if (isOverlapped) {
          layer._layerConfig.InputBlocker?.Increment(actor);
          layer._suspensionBlock.Increment(actor);
        } else {
          layer._suspensionBlock.Decrement(actor);
          layer._layerConfig.InputBlocker?.Decrement(actor);
        }

        for (var i = layer._activeStack.Count - 1; i >= 0; i--) {
          var route = layer._activeStack[i];

          if (i == layer._activeStack.Count - 1) {
            if (layer._suspensionBlock.IsActive) {
              route.Suspend();
            } else {
              route.Resume();
            }

            isOverlapped = isOverlapped || route.IsSuspensive || route.IsSuspended;

            continue;
          }

          var overlappingRoute = layer._activeStack[i + 1];
          isOverlapped = isOverlapped || overlappingRoute.IsSuspensive || overlappingRoute.IsSuspended;
          if (isOverlapped) {
            route.Suspend();
          } else {
            route.Resume();
          }
        }
      }
    }
  }
}
