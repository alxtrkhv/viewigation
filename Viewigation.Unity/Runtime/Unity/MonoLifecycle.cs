using System;
using UnityEngine;

namespace Viewigation.Unity
{
  public class MonoLifecycle : MonoBehaviour, ILifecycle
  {
    public event Action<float>? Frame;
    public event Action<float>? Tick;
    public event Action<bool>? Pause;
    public event Action<bool>? Focus;

    public int FrameCount => _frameCount;
    private int _frameCount;

    public int TickCount => _tickCount;
    private int _tickCount;

    private void Update()
    {
      Frame?.Invoke(Time.deltaTime);
      _frameCount++;
    }

    private void FixedUpdate()
    {
      Tick?.Invoke(Time.fixedDeltaTime);
      _tickCount++;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
      Pause?.Invoke(pauseStatus);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
      Focus?.Invoke(hasFocus);
    }

    public void Dispose()
    {
      Frame = null;
      Tick = null;
      Pause = null;
      Focus = null;

      if (this != null) {
        Destroy(gameObject);
      }
    }
  }
}
