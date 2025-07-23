using UnityEngine;

namespace Viewigation.Views
{
  public abstract class UnityWidget : MonoBehaviour, IWidget
  {
    void IWidget.Initialize()
    {
      OnInitialize();
    }

    void IWidget.Dispose()
    {
      OnDispose();
    }

    void IWidget.Suspend()
    {
      OnSuspend(true);
    }

    void IWidget.Resume()
    {
      OnSuspend(false);
    }

    protected virtual void OnInitialize() { }
    protected virtual void OnDispose() { }
    protected virtual void OnSuspend(bool value) { }
  }
}
