namespace DualSenseClient.Tests;

[TestFixture]
public class Tests
{
    private string _testString = string.Empty;

    [SetUp]
    public void Setup()
    {
        _testString = "Hello DualSense!";
    }

    [Test]
    public void TestString()
    {
        Assert.That(_testString, Does.Contain("DualSense"));
    }
}