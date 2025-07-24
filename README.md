# Viewigation

After working with Unity in different companies for quite a while, I noticed a pattern, where every company developed their internal router/navigation system or MV* framework to maintain app architecture. Some of those, in my opinion, were unsophisticated, some - overly complex and dogmatic in the MV* way.
This library is my two cents. I have used it in multiple personal projects and find it capable but not too bounding.
It does not try to enforce any architectural style since it only manages V of MV\*. Yet it provides you with an ability to implement mechanism of architectural enforcement.

## Installation

### Unity Package Manager

You can install Viewigation directly in Unity using the Package Manager with a Git URL:

1. Open Unity and go to **Window > Package Manager**
2. Click the **+** button in the top-left corner
3. Select **Add package from git URL...**
4. Enter the following URL:

   ```
   https://github.com/alxtrkhv/viewigation.git?path=/Viewigation.Unity#release/v0.1.0
   ```

5. Click **Add**

## Dependencies

### Mandatory Dependencies

- **UniTask** - All async code in Viewigation is built using UniTask. This is a required dependency for the library to function properly.

### Optional Dependencies

- **VContainer** - While `VContainerRouteFactory` is the only available implementation of `IRouteFactory` provided with Viewigation, users can write custom implementations if required.
- **Addressables** - While `AddressableAssets` is the only available implementation of `IAssets` provided with Viewigation, users can write custom implementations if required.

## Concepts

### UnityView(IView)

This type represents a view object with following lifecycle callbacks and handlers:

1. Initialize (OnInitialized)
2. Show (OnShown)
3. Suspend (OnSuspended(true))
4. Resume (OnSuspended(false))
5. Hide (OnHidden)
6. Dispose (OnDisposed)

As you may notice, every callback has a counterpart. I believe improper lifecycle management is one of the main bug sources and this is why I'd like to elaborate on this part.

After a view is initialized and before it's disposed - it can be shown, hidden, suspended, and resumed multiple times. Both OnInitialized and OnDisposed are sync handlers.

OnShown and OnHidden both are async and both support transitional animations.

Suspension is controlled by four factors:

1. Route containing the view is overlapped by a suspensive route on the same layer (more on this later)
2. Layer is overlapped by a suspensive route on a dependee layer (more on this later)
3. Layer is suspended and resumed manually
4. With MonoLifecycle suspension follows OnApplicationFocus and OnApplicationPause.

### UnityRoute(IRoute)

In a nutshell, routes are wrappers for views to allow a view to be used in multiple contexts with different settings. Routes manage the lifecycle of their associated views and provide navigation operations like opening, closing, loading, and unloading.

Routes can be suspensive, meaning they will suspend underlying routes when opened on top of them in the same layer.

### NavigationLayer(INavigationLayer)

This is the part that does all the heavy lifting. Navigation layers manage collections of routes and handle their lifecycle, stacking, and suspension logic.

### Navigation(INavigation)

The main navigation system that orchestrates multiple layers. It provides a unified API for route operations across all layers and manages layer dependencies.

### IRouteFactory

The factory interface responsible for creating and managing routes and their associated views. The route factory handles:

- Creating new route instances
- Instantiating views for routes

With custom implemenation of the factory and view it's possible to enforce some architectural rules - mandatory models, view models, etc

### IAssets

The asset management interface that abstracts asset loading and unloading operations. This allows Viewigation to work with different asset systems.

### ReactiveObject(IReadOnlyObject/IMutableObject)

A reactive programming primitive that provides observable value changes with event-driven notifications. ReactiveObjects allow you to:

- Store and observe value changes with automatic equality checking
- Subscribe to value updates with `Read(handler)` methods that support both single-value and old/new-value handlers
- Update values using `Value` setter or `Write(newValue)`
- Force updates even when values are equal using `ForceWrite(sameValue)`
- Bind reactive objects to UI components using `ReactiveBinder` for simplified cleanup

### Animations

Viewigation provides two approaches for implementing view animations:

#### IUnityViewAnimation

An interface for implementing custom transition animations. This interface provides:

- `Initialize()` - Called once when the animation is set up
- `PlayShown(CancellationToken)` - Async method for show animations
- `PlayHidden(CancellationToken)` - Async method for hide animations

This approach is ideal for code-driven animations or when you need full control over the animation logic.

#### BaseUnityViewAnimation

Abstract monobehaviour version for component based approach

### UnityWidget(IWidget)

A lightweight component within views. Widgets follow the same lifecycle pattern as views but are managed by their parent view.

Widgets support:

- Initialize/Dispose lifecycle
- Suspend/Resume states
- Automatic discovery and management by parent UnityView

## Usage

Here's an example showing how to use Viewigation in your Unity project:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Viewigation.Animations;
using Viewigation.Navigation;
using Viewigation.ReactiveObjects;
using Viewigation.Routes;
using Viewigation.Views;

namespace Viewigation.Example
{
  public class SomeRoute : UnityRoute<View>
  {
    public SomeRoute() : base(viewKey: "SomeAddressableKey", isSuspensive: true) { }
  }

  public class View : UnityView
  {
    private Model _model = null!;

    // AnimationOrder.After/Before/Manual
    protected override AnimationOrder ShownAnimationOrder =>
      AnimationOrder.Manual; // Animation won't be triggered automatically (After is default)

    protected override AnimationOrder HiddenAnimationOrder =>
      AnimationOrder.Before; // Animation will be triggered before OnHide (After is default)

    protected override bool PropagateToWidgets =>
      false; // Widgets won't be initialized, suspended, resumed, and disposed automatically (true is default)

    [Inject]
    public void Inject(Model model)
    {
      _model = model;
    }

    protected override void OnInitialize()
    {
      // Unbind will be handled automatically in Dispose
      UntilDisposed.Bind(
        _model.Foo,
        input => { }
      );
    }

    protected override async UniTask OnShow(bool animated, CancellationToken cancellation = default)
    {
      // Unbind will be handled automatically in Hide
      UntilHidden.Bind(
        _model.Bar,
        input => {
          // Update some counter
        }
      );

      if (animated) {
        await PlayShowAnimation(cancellation: cancellation);
      }

      // You must call StopReading manually in OnHide
      _model.Foo.Read(
        FooHandler
      );
    }

    private void FooHandler(float old, float updated)
    {
      // Update some text field
    }

    protected override UniTask OnHide(bool animated, CancellationToken cancellation = default)
    {
      _model.Foo.StopReading(FooHandler);

      return base.OnHide(animated, cancellation);
    }
  }

  public class RouteWithParameters : UnityRoute<ViewWithParameters>
  {
    public RouteWithParameters() : base("SomeKey", isSuspensive: false) { }
  }

  public class ViewParameters
  {
    public string Foo { get; set; } = string.Empty;
  }

  public class ViewWithParameters : UnityView<ViewParameters>
  {
    protected override UniTask OnShow(bool animated, CancellationToken cancellation = default)
    {
      if (CurrentParameters != null) {
        Debug.Log(CurrentParameters.Foo);
      }

      return base.OnShow(animated, cancellation);
    }
  }

  public class Scope : LifetimeScope
  {
    [SerializeField]
    private List<NavigationLayerConfig> _navigationLayerConfigs = null!;

    protected override void Configure(IContainerBuilder builder)
    {
      builder.RegisterEntryPoint<EntryPoint>();

      var navigation = INavigation.Builder()
        .WithAddressables() // .WithCustomAssets(IAssets assets) .WithNullAssets()
        .WithVContainer(builder) // .WithCustomRouteFactory(IRouteFactory routeFactory)
        .WithMonoLifecycle() // .WithCustomLifecycle
        .AddLayerConfigs( // .AddLayerConfig(INavigationLayerConfig config) .AddLayer(Parent.Container.Resolve<INavigationLayer>())
          _navigationLayerConfigs
        )
        .Create();

      builder.RegisterInstance(navigation);
    }
  }

  public class EntryPoint : IAsyncStartable
  {
    private readonly INavigation _navigation;

    public EntryPoint(INavigation navigation)
    {
      _navigation = navigation;
    }

    public async UniTask StartAsync(CancellationToken cancellation = default)
    {
      _navigation.Initialize();

      await _navigation["SomeLayerTag"].Open<SomeRoute>(cancellation: cancellation); // Load will be call implicitly
      await _navigation["SomeLayerTag"].Close<SomeRoute>(cancellation: cancellation);
      _navigation["SomeLayerTag"].Unload<RouteWithParameters>();

      _navigation.RegisterOnLayer<SomeRoute>(layer: "SomeLayerTag", id: "RegisteredInstance"); // Type + id are unique

      await _navigation.Load<SomeRoute>(id: "RegisteredInstance", cancellation: cancellation); // Called explicitly
      await _navigation.Open<SomeRoute>(id: "RegisteredInstance", cancellation: cancellation);

      await _navigation.Close<SomeRoute>(id: "RegisteredInstance", cancellation: cancellation, unload: true); // Unload called implicitly

      var route = await _navigation["SomeLayerTag"].Open<SomeRoute>(cancellation: cancellation);
      if (route != null) {
        await route.Close(unload: true, cancellation: cancellation);
      }

      _navigation.RegisterOnLayer<RouteWithParameters>(layer: "Foo");

      await _navigation.Open<RouteWithParameters, ViewParameters>(
        parameters: new() { Foo = "Bar", },
        cancellation: cancellation
      );

      await _navigation.Open<RouteWithParameters>(cancellation: cancellation); // CurrentParameters will be null
    }
  }

  public class Model
  {
    public IMutableObject<float> Foo { get; } = new ReactiveObject<float>(0f);
    public IReadOnlyObject<int> Bar { get; } = new ReactiveObject<int>(1);
  }
}

```
