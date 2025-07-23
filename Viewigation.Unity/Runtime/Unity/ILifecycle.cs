using System;

namespace Viewigation.Unity
{
  public interface ILifecycle : IDisposable
  {
    event Action<float>? Frame;
    event Action<float>? Tick;
    event Action<bool>? Pause;
    event Action<bool>? Focus;

    int FrameCount { get; }
    int TickCount { get; }
  }
}
