using NUnit.Framework;
using Viewigation.ReactiveObjects;

namespace Viewigation.Tests.Tests.ReactiveValues
{
  [TestFixture]
  public class ObjectTests
  {
    [Test]
    public void WriteWontTriggerUpdate_IfNewValueIsEqualToOldReference()
    {
      var stringReference = "foo";
      var reference = new ReactiveObject<string>(string.Empty);
      var counter = 0;
      reference.Read(_ => counter++, false);

      reference.Write(stringReference);
      reference.Write(stringReference);

      Assert.That(counter, Is.EqualTo(1));
    }

    [Test]
    public void ForceWriteTriggerUpdate_IfNewValueIsEqualToOldReference()
    {
      var stringReference = "foo";
      var reference = new ReactiveObject<string>(string.Empty);
      var counter = 0;
      reference.Read(_ => counter++, false);

      reference.Write(stringReference);
      reference.ForceWrite(stringReference);

      Assert.That(counter, Is.EqualTo(2));
    }

    [Test]
    public void WriteTriggersUpdate_ForBothNewAndOldNewArgumentHandlers()
    {
      var reference = new ReactiveObject<string>(string.Empty);

      var triggeredNewHandler = false;
      var triggerOldNewHandler = false;

      reference.Read(_ => triggeredNewHandler = true);
      reference.Read((_, _) => triggerOldNewHandler = true);

      reference.Write(" ");

      Assert.That(triggeredNewHandler);
      Assert.That(triggerOldNewHandler);
    }

    [Test]
    public void WriteUpdatesValue_WhenWritten()
    {
      var reference = new ReactiveObject<string>(string.Empty);

      reference.Write("foo");

      var bar = "bar";
      reference.Write(bar);

      Assert.That(reference.Value, Is.EqualTo(bar));
    }
  }
}
