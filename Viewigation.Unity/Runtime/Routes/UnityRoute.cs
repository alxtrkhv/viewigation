using Viewigation.Views;

namespace Viewigation.Routes
{
  public abstract class UnityRoute<TViewType> : BaseRoute<TViewType> where TViewType : IView
  {
    protected UnityRoute(string? viewKey, bool isSuspensive) : base(
      viewKey,
      RouteType.Unity,
      isSuspensive
    ) { }
  }
}
