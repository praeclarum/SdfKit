namespace Tests;

public class RaytracerTests
{
    [SetUp]
    public void Setup()
    {
    }


    [Test]
    public void Sphere()
    {
        var r = 0.5f;
        var sdf = Sdf.CreateSphere(r);
    }
}
