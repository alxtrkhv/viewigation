namespace Viewigation.Blocks
{
  public class BlockWithOverride : Block, IBlockWithOverride
  {
    public new bool IsActive => _override ?? _counter > 0;

    private bool? _override;

    public BlockWithOverride(bool isExclusive = true) : base(isExclusive) { }

    public void SetOverride(bool value)
    {
      _override = value;
    }

    public void UnsetOverride()
    {
      _override = null;
    }
  }
}
