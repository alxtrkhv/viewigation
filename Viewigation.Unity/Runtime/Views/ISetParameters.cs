namespace Viewigation.Views
{
  public interface ISetParameters<TParameters> : IResetParameters
  {
    public TParameters? Parameters { get; protected set; }

    public void SetParameters(TParameters? parameters, bool overrideIfSet = false)
    {
      if (ParametersSet && !overrideIfSet) {
        return;
      }

      Parameters = parameters;
      ParametersSet = true;
    }

    void IResetParameters.ResetParametersImpl()
    {
      Parameters = default;
    }
  }
}
