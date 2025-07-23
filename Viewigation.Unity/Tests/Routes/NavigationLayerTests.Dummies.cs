using System.Threading;
using Cysharp.Threading.Tasks;
using Viewigation.Routes;
using Viewigation.Views;

namespace Viewigation.Tests.Tests.Routes
{
  public partial class NavigationLayerTests
  {
    public class NonSuspensiveUnityRoute : UnityRoute<IView>
    {
      public NonSuspensiveUnityRoute() : base(string.Empty, false) { }
    }

    public class SuspensiveUnityRoute : UnityRoute<IView>
    {
      public SuspensiveUnityRoute() : base(string.Empty, true) { }
    }

    public class DummyUnityRouteWithParameters : UnityRoute<IView<string>>
    {
      public DummyUnityRouteWithParameters() : base(string.Empty, true) { }
    }

    public class RouteWithoutValidView : UnityRoute<NonExistingView>
    {
      public RouteWithoutValidView() : base(string.Empty, false) { }
    }

    public class NonExistingView : IView
    {
      void IWidget.Initialize() { }

      void IWidget.Dispose() { }

      UniTask IView.Show(bool animated, CancellationToken cancellation)
      {
        return UniTask.CompletedTask;
      }

      UniTask IView.Hide(bool animated, CancellationToken cancellation)
      {
        return UniTask.CompletedTask;
      }

      void IWidget.Suspend() { }

      void IWidget.Resume() { }
    }

    public class SlowRoute : UnityRoute<SlowView>
    {
      public SlowRoute() : base(string.Empty, false) { }
    }

    public class SlowView : IView<SlowViewParameters>
    {
      bool IResetParameters.ParametersSet { get; set; }

      SlowViewParameters ISetParameters<SlowViewParameters>.Parameters
      {
        get => _parameters;
        set => _parameters = value;
      }

      private SlowViewParameters _parameters;

      void IWidget.Initialize() { }

      void IWidget.Dispose() { }

      UniTask IView.Show(bool animated, CancellationToken cancellation)
      {
        return _parameters.ShowTcs.Task;
      }

      UniTask IView.Hide(bool animated, CancellationToken cancellation)
      {
        return _parameters.HideTcs.Task;
      }

      void IWidget.Suspend() { }

      void IWidget.Resume() { }
    }

    public class SlowViewParameters
    {
      public readonly UniTaskCompletionSource ShowTcs = new();
      public readonly UniTaskCompletionSource HideTcs = new();
      public readonly UniTaskCompletionSource SuspendTcs = new();
      public readonly UniTaskCompletionSource ResumeTcs = new();
    }

    public class ViewWithParameters<TParameters> : IView<TParameters>
    {
      void IWidget.Initialize() { }

      void IWidget.Dispose() { }

      UniTask IView.Show(bool animated, CancellationToken cancellation)
      {
        return UniTask.CompletedTask;
      }

      UniTask IView.Hide(bool animated, CancellationToken cancellation)
      {
        return UniTask.CompletedTask;
      }

      void IWidget.Suspend() { }

      void IWidget.Resume() { }

      bool IResetParameters.ParametersSet { get; set; }

      TParameters ISetParameters<TParameters>.Parameters { get; set; }
    }
  }
}
