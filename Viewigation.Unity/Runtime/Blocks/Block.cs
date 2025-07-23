using System.Collections.Generic;
using UnityEngine;

namespace Viewigation.Blocks
{
  public class Block : IBlock
  {
    private readonly HashSet<object>? _actors;

    public bool IsActive => _counter > 0;

    protected int _counter;

    public Block(bool isExclusive = true)
    {
      _actors = isExclusive ? new() : null;
    }

    public void Increment(object? actor)
    {
      if (actor == null || (_actors?.Add(actor) ?? true)) {
        _counter++;
      }
    }

    public void Decrement(object? actor)
    {
      if (actor == null || (_actors?.Remove(actor) ?? true)) {
        _counter = Mathf.Max(0, _counter - 1);
      }
    }

    public void Flush()
    {
      _counter = 0;
      _actors?.Clear();
    }
  }
}
