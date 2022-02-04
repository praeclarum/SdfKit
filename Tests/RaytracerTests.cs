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
        var w = 50;
        var h = 30;
        var r = 1.0f;
        var sdf = Sdf.Sphere(r);
        var rt = new Raytracer(w, h, sdf);
        using var img = rt.RenderDepth();
        Assert.AreEqual(w, img.Width);
        Assert.AreEqual(h, img.Height);
        Assert.AreEqual(4.0f, img[w/2, h/2], 1.0e-2f);
        Assert.Greater(img[0, 0], 9.0f);
        img.SaveTga("SphereDepth_50x30.tga", 3, 10);
    }

    [Test]
    public void BoxDepth()
    {
        var w = 50;
        var h = 30;
        var r = 1.0f;
        var sdf = Sdf.Box(r);
        var rt = new Raytracer(w, h, sdf);
        using var img = rt.RenderDepth();
        Assert.AreEqual(w, img.Width);
        Assert.AreEqual(h, img.Height);
        Assert.AreEqual(4.0f, img[w/2, h/2], 1.0e-2f);
        Assert.Greater(img[0, 0], 9.0f);
        img.SaveTga("BoxDepth_50x30.tga", 3, 10);
    }
}
