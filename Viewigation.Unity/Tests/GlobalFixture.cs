using NUnit.Framework;
using UnityEngine;

[assembly: Category(nameof(Viewigation))]

namespace Viewigation.Tests.Tests
{
  [SetUpFixture]
  public class GlobalFixture
  {
    [OneTimeSetUp]
    public void Init()
    {
      Debug.unityLogger.logEnabled = false;
    }

    [OneTimeTearDown]
    public void Dispose()
    {
      Debug.unityLogger.logEnabled = true;
    }
  }
}
