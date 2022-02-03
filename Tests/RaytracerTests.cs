namespace Tests;

public class RaytracerTests
{
    [SetUp]
    public void Setup()
    {
    }


    [Test]
    public void SphereSmall()
    {
        var r = 0.5f;
        var sdf = Sdf.CreateSphere(r);
        var rt = new Raytracer(5, 3, sdf);
        using var img = rt.Render();
        Assert.AreEqual(5, img.Width);
        Assert.AreEqual(3, img.Height);
    }
}
