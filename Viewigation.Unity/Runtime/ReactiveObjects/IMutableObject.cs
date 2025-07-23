namespace Viewigation.ReactiveObjects
{
  public interface IMutableObject<TObject> : IReadOnlyObject<TObject>
  {
    public new TObject Value { get; set; }
    public void Write(TObject reference);
    public void ForceWrite(TObject reference);

    public void Refresh() => ForceWrite(Value);
  }
}
