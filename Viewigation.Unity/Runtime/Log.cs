namespace Viewigation
{
  public static class Log
  {
    public static void Error(string message)
    {
      UnityEngine.Debug.LogError($"[{nameof(Viewigation)}] {message}");
    }

    public static void Warning(string message)
    {
      UnityEngine.Debug.LogWarning($"[{nameof(Viewigation)}] {message}");
    }

    public static void Debug(string message)
    {
      UnityEngine.Debug.Log($"[{nameof(Viewigation)}] {message}");
    }
  }
}
