using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Viewigation.Routes;
using Viewigation.Unity;
using Viewigation.Views;

namespace Viewigation.Navigation
{
  public class Navigation : INavigation
  {
    private const string PauseSuspensionToken = nameof(PauseSuspensionToken);
    private const string FocusSuspensionToken = nameof(FocusSuspensionToken);

    private readonly Dictionary<string, INavigationLayer> _layers;
    private readonly RouteMap _routeMap = new();
    private readonly ILifecycle _lifecycle;

    public int Count => _layers.Count;

    public INavigationLayer this[string layer] => Layer(layer);

    public Navigation(IEnumerable<INavigationLayer> layers, ILifecycle lifecycle)
    {
      _layers = layers
        .ToDictionary(
          k => k.Id,
          v => v
        );

      _lifecycle = lifecycle;
    }

    public void Initialize()
    {
      InitializeAllLayers();

      _lifecycle.Focus += OnFocus;
      _lifecycle.Pause += OnPause;
    }

    private void InitializeAllLayers()
    {
      foreach (var layer in _layers.Values) {
        layer.Initialize(this);
      }
    }

    public bool AddLayer(INavigationLayer layer)
    {
      if (!_layers.TryGetValue(layer.Id, out var existingLayer) || existingLayer is NullNavigationLayer) {
        _layers[layer.Id] = layer;
        InitializeAllLayers();

        return true;
      }

      Log.Warning($"Layer with id '{layer.Id}' already loaded.");
      return false;
    }

    public bool RemoveLayer(string key)
    {
      if (_layers.ContainsKey(key)) {
        _layers.Remove(key, out var layer);
        layer.Dispose();
        InitializeAllLayers();
        return true;
      }

      Log.Warning($"There is no layer with id '{key} to unload.");
      return false;
    }

    public void Dispose()
    {
      _lifecycle.Focus -= OnFocus;
      _lifecycle.Pause -= OnPause;

      foreach (var layer in this) {
        layer.Dispose();
      }

      _layers.Clear();
    }

    void INavigation.RegisterOnLayer(Type routeType, string layer, string? id)
    {
      _routeMap.Register(routeType, layer, id);
    }

    public INavigationLayer Layer(string layerName)
    {
      if (_layers.TryGetValue(layerName, out var layer)) {
        return layer;
      }

      return _layers[layerName] = new NullNavigationLayer(layerName);
    }

    public IEnumerator<INavigationLayer> GetEnumerator()
    {
      return _layers.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    private void OnPause(bool pause)
    {
      foreach (var layer in this) {
        if (pause) {
          layer.Suspend(PauseSuspensionToken);
        } else {
          layer.Resume(PauseSuspensionToken);
        }
      }
    }

    private void OnFocus(bool hasFocus)
    {
      foreach (var layer in this) {
        if (hasFocus) {
          layer.Resume(FocusSuspensionToken);
        } else {
          layer.Suspend(FocusSuspensionToken);
        }
      }
    }

    public TRoute? LoadedRoute<TRoute>(string? id = null) where TRoute : IRoute<IView>
    {
      var layerKey = _routeMap.Layer<TRoute>(id);
      if (layerKey == null) {
        return default;
      }

      var layer = Layer(layerKey);

      return layer.LoadedRoute<TRoute>();
    }

    public UniTask<TRoute?> Load<TRoute>(string? id = null, bool tryPreload = false,
      CancellationToken cancellation = default) where TRoute : IRoute<IView>, new()
    {
      var layerKey = _routeMap.Layer<TRoute>(id);
      if (layerKey == null) {
        return UniTask.FromResult<TRoute?>(default);
      }

      var layer = Layer(layerKey);

      return layer.Load<TRoute>(id, tryPreload, cancellation);
    }

    public void Unload<TRoute>(string? id = null, bool force = false) where TRoute : IRoute<IView>
    {
      var layerKey = _routeMap.Layer<TRoute>(id);
      if (layerKey == null) {
        return;
      }

      var layer = Layer(layerKey);
      var route = layer.LoadedRoute<TRoute>(id);

      Unload(route, force);
    }

    public void Unload<TRoute>(TRoute? route, bool force = false) where TRoute : IRoute
    {
      if (route == null) {
        return;
      }

      var layerKey = _routeMap.Layer<TRoute>();
      if (layerKey == null) {
        return;
      }

      var layer = Layer(layerKey);
      layer.Unload(route, force);
    }

    public UniTask<TRoute?> Open<TRoute>(string? id = null, bool animated = true,
      CancellationToken cancellation = default) where TRoute : IRoute<IView>, new()
    {
      var layerKey = _routeMap.Layer<TRoute>(id);
      if (layerKey == null) {
        return UniTask.FromResult<TRoute?>(default);
      }

      var layer = Layer(layerKey);
      return layer.Open<TRoute>(id, animated, cancellation);
    }

    public UniTask Open<TRoute>(TRoute? route, bool animated = true, CancellationToken cancellation = default)
      where TRoute : IRoute
    {
      var layerKey = _routeMap.Layer<TRoute>();
      if (layerKey == null) {
        return UniTask.CompletedTask;
      }

      var layer = Layer(layerKey);
      return layer.Open(route, animated, cancellation);
    }

    public UniTask<TRoute?> Open<TRoute, TParameters>(string? id = null, TParameters? parameters = default,
      bool animated = true,
      CancellationToken cancellation = default) where TRoute : IRoute<IView<TParameters>>, new()
    {
      var layerKey = _routeMap.Layer<TRoute>(id);
      if (layerKey == null) {
        return UniTask.FromResult<TRoute?>(default);
      }

      var layer = Layer(layerKey);
      return layer.Open<TRoute, TParameters>(id, parameters, animated, cancellation);
    }

    public UniTask Open<TRoute, TParameters>(TRoute? route, TParameters? parameters = default, bool animated = true,
      CancellationToken cancellation = default) where TRoute : IRoute<IView<TParameters>>
    {
      var layerKey = _routeMap.Layer<TRoute>();
      if (layerKey == null) {
        return UniTask.CompletedTask;
      }

      var layer = Layer(layerKey);
      return layer.Open(route, parameters, animated, cancellation);
    }

    public UniTask<TRoute?> Close<TRoute>(string? id = null, bool animated = true, bool? unload = null,
      CancellationToken cancellation = default) where TRoute : IRoute<IView>
    {
      var layerKey = _routeMap.Layer<TRoute>(id);
      if (layerKey == null) {
        return UniTask.FromResult<TRoute?>(default);
      }

      var layer = Layer(layerKey);
      return layer.Close<TRoute>(id, animated, unload, cancellation);
    }

    public UniTask Close<TRoute>(IRoute? route, bool animated = true, bool? unload = null,
      CancellationToken cancellation = default) where TRoute : IRoute
    {
      var layerKey = _routeMap.Layer<TRoute>();
      if (layerKey == null) {
        return UniTask.CompletedTask;
      }

      var layer = Layer(layerKey);
      return layer.Close<TRoute>(route, animated, unload, cancellation);
    }
  }
}
