using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using Viewigation.Assets;
using Viewigation.Routes;
using Viewigation.Unity;
using Viewigation.VContainer;

namespace Viewigation.Navigation
{
  public struct NavigationBuildingContext
  {
    private IAssets? _assets;
    private IRouteFactory? _routeFactory;
    private ILifecycle? _lifecycle;
    private List<INavigationLayerConfig>? _navigationLayersConfigs;
    private List<INavigationLayer>? _navigationLayers;

#if VIEWIGATION_ADDRESSABLES
    public NavigationBuildingContext WithAddressables()
    {
      _assets = new AddressableAssets();
      return this;
    }
#endif

#if VIEWIGATION_VCONTAINER
    public NavigationBuildingContext WithVContainer(IContainerBuilder containerBuilder)
    {
      if (_assets == null) {
        Log.Error($"You should select implementation of {nameof(IAssets)}.");
        return this;
      }

      var factory = new VContainerRouteFactory(_assets);
      _routeFactory = factory;

      containerBuilder.RegisterBuildCallback(container => {
          factory.SetContainer(container.Resolve<IObjectResolver>());

          var navigation = container.Resolve<INavigation>();
          var layers = container.Resolve<IReadOnlyList<INavigationLayer>>();

          foreach (var layer in layers) {
            navigation.AddLayer(layer);
          }
        }
      );

      return this;
    }
#endif

    public NavigationBuildingContext WithCustomRouteFactory(IRouteFactory routeFactory)
    {
      _routeFactory = routeFactory;

      return this;
    }

    public NavigationBuildingContext WithMonoLifecycle(GameObject? carrier = null, Transform? parent = null)
    {
      if (carrier == null) {
        carrier = new($"{nameof(Viewigation)}.Lifecycle");
      }

      _lifecycle = carrier.GetComponent<ILifecycle>();
      if (_lifecycle == null) {
        _lifecycle = carrier.AddComponent<MonoLifecycle>();
      }

      if (parent != null) {
        carrier.transform.SetParent(parent, false);
        carrier.transform.localPosition = Vector3.zero;
      }

      return this;
    }

    public NavigationBuildingContext WithCustomLifecycle(ILifecycle lifecycle)
    {
      _lifecycle = lifecycle;

      return this;
    }

    public NavigationBuildingContext AddLayerConfig(INavigationLayerConfig layerConfig)
    {
      _navigationLayersConfigs ??= new();
      _navigationLayersConfigs.Add(layerConfig);

      return this;
    }

    public NavigationBuildingContext AddLayerConfigs(params INavigationLayerConfig[] layerConfigs)
    {
      foreach (var config in layerConfigs) {
        AddLayerConfig(config);
      }

      return this;
    }

    public NavigationBuildingContext AddLayerConfigs(IEnumerable<INavigationLayerConfig> layerConfigs)
    {
      foreach (var config in layerConfigs) {
        AddLayerConfig(config);
      }

      return this;
    }

    public NavigationBuildingContext AddLayer(INavigationLayer layer)
    {
      _navigationLayers ??= new();
      _navigationLayers.Add(layer);

      return this;
    }

    public NavigationBuildingContext AddLayers(params INavigationLayer[] layers)
    {
      foreach (var config in layers) {
        AddLayer(config);
      }

      return this;
    }

    public NavigationBuildingContext AddLayers(IEnumerable<INavigationLayer> layers)
    {
      foreach (var config in layers) {
        AddLayer(config);
      }

      return this;
    }

    public INavigation? Create()
    {
      if (_routeFactory == null) {
        Log.Error($"You should select implementation of {nameof(IRouteFactory)}.");
        return null;
      }

      if (_lifecycle == null) {
        Log.Error($"You should select implementation of {nameof(ILifecycle)}.");
        return null;
      }

      var routeFactory = _routeFactory;
      var newLayers = _navigationLayersConfigs?
        .Select(x => new NavigationLayer(x.Self, routeFactory))
        .ToArray() ?? Array.Empty<INavigationLayer>();

      var layers = _navigationLayers?.Concat(newLayers).ToArray();

      return new Navigation(layers ?? newLayers, _lifecycle);
    }
  }
}
