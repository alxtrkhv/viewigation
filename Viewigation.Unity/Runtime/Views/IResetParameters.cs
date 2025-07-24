namespace Viewigation.Views
{
  public interface IResetParameters
  {
    public bool ParametersSet { get; protected set; }

    public void ResetParameters()
    {
      ResetParametersImpl();
      ParametersSet = false;
    }

    protected void ResetParametersImpl();
  }
}
