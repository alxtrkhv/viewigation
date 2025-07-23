using UnityEngine;

namespace Viewigation.Blocks
{
  public abstract class BaseInputBlocker : MonoBehaviour, IBlockWithOverride
  {
    public bool IsActive => _block.IsActive;

    private readonly BlockWithOverride _block = new(true);

    private bool _cache;

    public void Increment(object? actor)
    {
      _block.Increment(this);
      TrySetBlocked(_block.IsActive);
    }

    public void Decrement(object? actor)
    {
      _block.Decrement(this);
      TrySetBlocked(_block.IsActive);
    }

    public void Flush()
    {
      _block.Flush();
      TrySetBlocked(_block.IsActive);
    }

    private void TrySetBlocked(bool value)
    {
      if (value == _cache) {
        return;
      }

      SetBlocked(value);
      _cache = value;
    }

    public void SetOverride(bool value)
    {
      _block.SetOverride(value);
      TrySetBlocked(_block.IsActive);
    }

    public void UnsetOverride()
    {
      _block.UnsetOverride();
      TrySetBlocked(_block.IsActive);
    }

    protected abstract void SetBlocked(bool value);
  }
}
