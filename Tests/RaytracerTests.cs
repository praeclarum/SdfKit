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
        var rt = new Raytracer(50, 30, sdf);
        var img = rt.Render();
        Assert.AreEqual(50, img.Width);
        Assert.AreEqual(30, img.Height);
    }
}
