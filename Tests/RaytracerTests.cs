namespace Tests;

public class RaytracerTests
{
    [SetUp]
    public void Setup()
    {
    }


    [Test]
    public void SphereDepth()
    {
        var r = 1.0f;
        var sdf = Sdf.CreateSphere(r);
        var rt = new Raytracer(5, 3, sdf);
        using var img = rt.RenderDepth();
        Assert.AreEqual(5, img.Width);
        Assert.AreEqual(3, img.Height);
        Assert.AreEqual(4.0f, img[2, 1], 1.0e-6f);
        Assert.Greater(img[0, 0], 9.0f);
    }
}
