using System.Threading;
using Cysharp.Threading.Tasks;
using Viewigation.Routes;

namespace Viewigation.Views
{
  public interface IWidget
  {
    internal void Initialize();
    internal void Dispose();
    internal void Suspend();
    internal void Resume();
  }

  public interface IView : IWidget
  {
    internal UniTask Show(bool animated, CancellationToken cancellation = default);
    internal UniTask Hide(bool animated, CancellationToken cancellation = default);
  }

  public interface IViewWithRoute : IView
  {
    public IRoute? Route { get; internal set; }
  }

  public interface IView<TParameters> : IView, ISetParameters<TParameters> { }
}
