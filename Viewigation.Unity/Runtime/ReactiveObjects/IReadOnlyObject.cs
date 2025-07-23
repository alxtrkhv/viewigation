using System;

namespace Viewigation.ReactiveObjects
{
  public interface IReadOnlyObject { }

  public interface IReadOnlyObject<out TObject> : IReadOnlyObject
  {
    public TObject Value { get; }

    public void Read(Action<TObject> handler, bool forceInit = true);
    public void StopReading(Action<TObject> handler);

    public void Read(Action<TObject, TObject> handler);
    public void StopReading(Action<TObject, TObject> handler);
  }
}
