namespace Viewigation.Blocks
{
  public interface IBlock
  {
    public bool IsActive { get; }

    void Increment(object? actor);
    void Decrement(object? actor);

    void Flush();
  }

  public interface IBlockWithOverride : IBlock
  {
    void SetOverride(bool value);
    void UnsetOverride();
  }
}
