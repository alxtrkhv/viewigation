using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using Viewigation.Assets;
using Viewigation.Routes;
using Viewigation.Views;
using Object = UnityEngine.Object;

namespace Viewigation.VContainer
{
  public class VContainerRouteFactory : IRouteFactory
  {
    private readonly IAssets _assets;
    private readonly Dictionary<string, GameObject> _loadedPrefabs = new();
    private readonly Dictionary<Type, LifetimeScope> _customScopes = new();

    private IObjectResolver _container;

    public VContainerRouteFactory(IAssets assets, IObjectResolver container = null!)
    {
      _assets = assets;
      _container = container;
    }

    public void SetContainer(IObjectResolver container)
    {
      _container = container;
    }

    TRoute IRouteFactory.NewRoute<TRoute>(string? id)
    {
      var route = new TRoute {
        Id = id,
      };

      _container.Inject(route);

      return route;
    }

    UniTask<IView?> IRouteFactory.NewView<TRoute>(TRoute route, INavigationLayerConfig layerConfig,
      CancellationToken cancellation)
    {
      return route.RouteType switch {
        RouteType.Unity => NewUnityView(route, layerConfig, cancellation),
        RouteType.Scripted => NewScriptedView(route),
        _ => default,
      };
    }

    private UniTask<IView?> NewScriptedView<TRoute>(TRoute route) where TRoute : IRoute
    {
      if (_container.TryResolve(route.ViewType, out var newView)) {
        return new(newView as IView);
      }

      newView = Activator.CreateInstance(route.ViewType);
      _container.Inject(newView);

      return new(newView as IView);
    }

    IView? IRouteFactory.ExistingView<TRoute>(TRoute route, INavigationLayerConfig layerConfig)
    {
      return route.RouteType switch {
        RouteType.Unity => ExistingUnityView(route, layerConfig),
        _ => null,
      };
    }

    private async UniTask<IView?> NewUnityView<TRoute>(TRoute route, INavigationLayerConfig layerConfig,
      CancellationToken cancellation = default)
      where TRoute : IRoute
    {
      if (route.ViewKey is null) {
        return null;
      }

      var gameObject = await _assets.LoadAsync<GameObject>(route.ViewKey, cancellation);
      if (gameObject == null) {
        return null;
      }

      if (!gameObject.TryGetComponent<UnityView>(out var prefab) || prefab.GetType() != route.ViewType) {
        _assets.ReleaseAsset(gameObject);
        return null;
      }

      var parent = layerConfig.Pool;
      var operation = Object.InstantiateAsync(prefab, parent);
      await operation;

      if (operation.Result == null || operation.Result.Length == 0 || operation.Result[0] == null) {
        _assets.ReleaseAsset(gameObject);
        return null;
      }

      var view = operation.Result[0];
      SetupView(layerConfig, view);

      if (route.ViewKey != null) {
        _loadedPrefabs[route.ViewKey] = gameObject;
      }

      return view;
    }

    private IView? ExistingUnityView<TRoute>(TRoute route, INavigationLayerConfig layerConfig)
      where TRoute : IRoute
    {
      var views = layerConfig.Root != null
        ? layerConfig.Root.GetComponentsInChildren(route.ViewType, includeInactive: true)
        : SceneManager.GetActiveScene().GetRootGameObjects()
          .SelectMany(x => x.GetComponentsInChildren(route.ViewType, includeInactive: true))
          .Where(x => x != null).ToArray();

      var view = views.Length == 1 ? views[0] as UnityView : null;
      if (views.Length > 1) {
        Log.Warning($"There is {views.Length} views of {route.ViewType} type. Cannot decide which one to load.");
      }

      if (view != null) {
        SetupView(layerConfig, view);
      }

      return view;
    }

    private void SetupView(INavigationLayerConfig layerConfig, UnityView view)
    {
      view.SetNavigationRoot(layerConfig);

      if (view is ICustomScopeProvider scopeSource && scopeSource.Scope != null) {
        _customScopes[scopeSource.ScopeType] = scopeSource.Scope;

        if (!scopeSource.Scope.autoRun) {
          scopeSource.Scope.Build();
        }
      }

      var container = _container;

      if (view is ICustomScopeConsumer scopeConsumer) {
        if (_customScopes.TryGetValue(scopeConsumer.ScopeType, out var scope) && scope != null) {
          container = scope.Container;
        }
      }

      container.InjectGameObject(view.gameObject);
    }

    public void Release(string? viewKey)
    {
      if (viewKey is null) {
        return;
      }

      if (!_loadedPrefabs.TryGetValue(viewKey, out var prefab)) {
        return;
      }

      _assets.ReleaseAsset(prefab);
      _loadedPrefabs.Remove(viewKey);
    }
  }
}
