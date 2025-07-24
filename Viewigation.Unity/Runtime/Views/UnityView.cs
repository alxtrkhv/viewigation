using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Viewigation.Animations;
using Viewigation.Navigation;
using Viewigation.ReactiveObjects;
using Viewigation.Routes;

namespace Viewigation.Views
{
  public abstract class UnityView : MonoBehaviour, IViewWithRoute
  {
    [Header("Unity View")]
    [SerializeField]
    private BaseUnityViewAnimation? _animation;

    protected ReactiveBinder UntilDisposed => _lifetimeBinder ??= new();
    private ReactiveBinder? _lifetimeBinder;

    protected ReactiveBinder UntilHidden => _showBinder ??= new();
    private ReactiveBinder? _showBinder;

    protected virtual AnimationOrder ShownAnimationOrder => AnimationOrder.After;
    protected virtual AnimationOrder HiddenAnimationOrder => AnimationOrder.After;
    protected virtual bool PropagateToWidgets => true;

    protected IUnityViewAnimation? Animation { get; set; } = null!;

    private Transform? _parent = null!;
    private Transform? _pool = null!;

    protected IReadOnlyList<UnityWidget> Widgets => _widgets ?? (IReadOnlyList<UnityWidget>)Array.Empty<UnityWidget>();
    private List<UnityWidget>? _widgets;

    IRoute? IViewWithRoute.Route
    {
      get => _route;
      set => _route = value;
    }

    protected IRoute? _route;

    public void SetNavigationRoot(INavigationLayerConfig navigationLayerConfig)
    {
      _parent = navigationLayerConfig.Root;
      _pool = navigationLayerConfig.Pool;
    }

    void IWidget.Initialize()
    {
      OnInitialize();

      if (PropagateToWidgets) {
        GetComponentsInChildren(includeInactive: true, _widgets ??= new());
      }

      foreach (var widget in Widgets) {
        (widget as IWidget)?.Initialize();
      }

      gameObject.SetActive(false);

      Animation ??= _animation;
      Animation?.Initialize();

      transform.SetParent(_pool, false);
      transform.localPosition = Vector3.zero;
    }

    void IWidget.Dispose()
    {
      foreach (var widget in Widgets) {
        (widget as IWidget)?.Dispose();
      }

      OnDispose();

      _showBinder?.Flush();
      _lifetimeBinder?.Flush();

      if (this != null) {
        Destroy(gameObject);
      }
    }

    async UniTask IView.Show(bool animated, CancellationToken cancellation)
    {
      transform.SetParent(_parent, false);
      transform.SetAsLastSibling();

      transform.localPosition = Vector3.zero;

      if (ShownAnimationOrder == AnimationOrder.Before) {
        await PlayShowAnimation(animated, cancellation);
      }

      await OnShow(animated, cancellation);

      if (ShownAnimationOrder == AnimationOrder.After) {
        await PlayShowAnimation(animated, cancellation);
      }

      if (!gameObject.activeSelf) {
        Log.Warning($"{GetType().Name} game object is not active when shown.");
      }
    }

    async UniTask IView.Hide(bool animated, CancellationToken cancellation)
    {
      if (HiddenAnimationOrder == AnimationOrder.Before) {
        await PlayHideAnimation(animated, cancellation);
      }

      await OnHide(animated, cancellation);

      if (HiddenAnimationOrder == AnimationOrder.After) {
        await PlayHideAnimation(animated, cancellation);
      }

      transform.SetParent(_pool);

      if (gameObject.activeSelf) {
        Log.Warning($"{GetType().Name} game object is active when hidden.");
      }

      _showBinder?.Flush();
    }

    protected UniTask PlayShowAnimation(bool animated = true, CancellationToken cancellation = default)
    {
      if (animated) {
        return Animation?.PlayShown(cancellation) ?? SetActiveUnityViewAnimation.ShowTask(gameObject);
      }

      return SetActiveUnityViewAnimation.ShowTask(gameObject);
    }

    protected UniTask PlayHideAnimation(bool animated = true, CancellationToken cancellation = default)
    {
      if (animated) {
        return Animation?.PlayHidden(cancellation) ?? SetActiveUnityViewAnimation.HideTask(gameObject);
      }

      return SetActiveUnityViewAnimation.HideTask(gameObject);
    }

    void IWidget.Suspend()
    {
      OnSuspend(true);

      foreach (var widget in Widgets) {
        (widget as IWidget)?.Suspend();
      }
    }

    void IWidget.Resume()
    {
      foreach (var widget in Widgets) {
        (widget as IWidget)?.Resume();
      }

      OnSuspend(false);
    }

    protected virtual void OnInitialize() { }
    protected virtual void OnDispose() { }
    protected virtual UniTask OnShow(bool animated, CancellationToken cancellation = default) => UniTask.CompletedTask;
    protected virtual UniTask OnHide(bool animated, CancellationToken cancellation = default) => UniTask.CompletedTask;

    protected virtual void OnSuspend(bool value) { }

    public void Close()
    {
      _route?.Close();
    }

    public void CloseAndForceUnload()
    {
      _route?.Close(unload: true);
    }

    public void CloseAndForceSkipUnload()
    {
      _route?.Close(unload: false);
    }
  }

  public abstract class UnityView<TParameters> : UnityView, IView<TParameters>
  {
    TParameters? ISetParameters<TParameters>.Parameters
    {
      get => CurrentParameters;
      set => CurrentParameters = value;
    }

    protected TParameters? CurrentParameters { get; private set; }

    bool IResetParameters.ParametersSet { get; set; }
  }
}
