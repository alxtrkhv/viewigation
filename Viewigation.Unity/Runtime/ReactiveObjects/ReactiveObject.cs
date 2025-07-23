using System;
using System.Collections.Generic;

namespace Viewigation.ReactiveObjects
{
  public class ReactiveObject<TValue> : IMutableObject<TValue>
  {
    private event Action<TValue>? NewValueSet;
    private event Action<TValue, TValue>? OldAndNewValuesSet;

    private TValue _currentValue;
    private TValue _oldValue;

    public TValue Value
    {
      get => _currentValue;
      set => Write(value);
    }

    public ReactiveObject(TValue currentValue)
    {
      _currentValue = currentValue;
      _oldValue = _currentValue;
    }

    public void Read(Action<TValue> handler, bool forceInit = true)
    {
      if (forceInit) {
        handler.Invoke(_currentValue);
      }

      NewValueSet += handler;
    }

    public void StopReading(Action<TValue> handler)
    {
      NewValueSet -= handler;
    }

    public void Read(Action<TValue, TValue> handler)
    {
      OldAndNewValuesSet += handler;
    }

    public void StopReading(Action<TValue, TValue> handler)
    {
      OldAndNewValuesSet -= handler;
    }

    public void Write(TValue value)
    {
      if (EqualityComparer<TValue>.Default.Equals(value, _currentValue)) {
        return;
      }

      ForceWrite(value);
    }

    public void ForceWrite(TValue value)
    {
      _oldValue = _currentValue;
      _currentValue = value;

      NewValueSet?.Invoke(_currentValue);
      OldAndNewValuesSet?.Invoke(_oldValue, _currentValue);
    }
  }
}
