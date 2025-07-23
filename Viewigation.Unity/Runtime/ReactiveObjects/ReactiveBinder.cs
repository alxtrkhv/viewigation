using System;

namespace Viewigation.ReactiveObjects
{
  public class ReactiveBinder : IDisposable
  {
    private event Action? Flushed;

    public ReactiveBinder Bind<TValue>(IReadOnlyObject<TValue> value, Action<TValue> handler)
    {
      value.Read(handler, true);

      Flushed += () => value.StopReading(handler);

      return this;
    }

    public ReactiveBinder Bind<TValue>(IReadOnlyObject<TValue> value, Action<TValue, TValue> handler)
    {
      value.Read(handler);

      Flushed += () => value.StopReading(handler);

      return this;
    }

    public ReactiveBinder Bind<TValue>(IReadOnlyObject<TValue> value, Action<TValue, TValue> handler, TValue initValue,
      TValue finalValue)
    {
      handler.Invoke(initValue, value.Value);
      value.Read(handler);

      Flushed += () => {
        value.StopReading(handler);
        handler.Invoke(value.Value, finalValue);
      };

      return this;
    }

    public ReactiveBinder BindWithInit<TValue>(IReadOnlyObject<TValue> value, Action<TValue, TValue> handler,
      TValue initValue)
    {
      handler.Invoke(initValue, value.Value);
      value.Read(handler);

      Flushed += () => value.StopReading(handler);

      return this;
    }

    public ReactiveBinder BindWithFinal<TValue>(IReadOnlyObject<TValue> value, Action<TValue, TValue> handler,
      TValue finalValue)
    {
      value.Read(handler);

      Flushed += () => {
        value.StopReading(handler);
        handler.Invoke(value.Value, finalValue);
      };

      return this;
    }

    public void Flush()
    {
      Flushed?.Invoke();
      Flushed = null;
    }

    public void Dispose()
    {
      Flush();
    }
  }
}
