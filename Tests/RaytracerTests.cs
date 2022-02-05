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
        var sdf = Sdfs.Sphere(r);
        var rt = new Raytracer(w, h, sdf);
        using var img = rt.RenderDepth();
        Assert.AreEqual(w, img.Width);
        Assert.AreEqual(h, img.Height);
        Assert.AreEqual(4.0f, img[w/2, h/2], 1.0e-2f);
        Assert.Greater(img[0, 0], 9.0f);
        img.SaveDepthTga("SphereDepth_50x30.tga", 3, 10);
    }

    [Test]
    public void BoxDepth()
    {
        var w = 50;
        var h = 30;
        var r = 1.0f;
        var sdf = Sdfs.Box(r);
        var rt = new Raytracer(w, h, sdf);
        using var img = rt.RenderDepth();
        Assert.AreEqual(w, img.Width);
        Assert.AreEqual(h, img.Height);
        Assert.AreEqual(4.0f, img[w/2, h/2], 1.0e-2f);
        Assert.Greater(img[0, 0], 9.0f);
        img.SaveDepthTga("BoxDepth_50x30.tga", 3, 10);
    }

    [Test]
    public void CylinderDepth()
    {
        var w = 50;
        var h = 30;
        var r = 0.25f;
        var sdf = 
            SdfExprs
            .Cylinder(r, r*2)
            .RepeatX(4*r)
            .ToSdf();
        var rt = new Raytracer(w, h, sdf);
        using var img = rt.RenderDepth();
        img.SaveDepthTga("CylinderDepth_50x30.tga", 3, 10);
        Assert.AreEqual(w, img.Width);
        Assert.AreEqual(h, img.Height);
        Assert.AreEqual(5-r, img[w/2, h/2-2], 1.0e-1f);
        Assert.Greater(img[0, 0], 9.0f);
    }

    [Test]
    public void PlaneDepth()
    {
        var w = 50;
        var h = 30;
        var sdf = Sdfs.PlaneXY();
        var rt = new Raytracer(w, h, sdf);
        using var img = rt.RenderDepth();
        Assert.AreEqual(w, img.Width);
        Assert.AreEqual(h, img.Height);
        Assert.AreEqual(5.0f, img[w/2, h/2], 1.0e-2f);
        Assert.Less(img[0, 0], 9.0f);
        img.SaveDepthTga("PlaneDepth_50x30.tga", 3, 10);
    }

    [Test]
    public void SphereRepeatDepth()
    {
        var w = 50;
        var h = 30;
        var r = 0.5f;
        var sdf = 
            SdfExprs
            .Sphere(r)
            .RepeatXY(2*r, 2*r)
            .ToSdf();
        var rt = new Raytracer(w, h, sdf);
        using var img = rt.RenderDepth();
        img.SaveDepthTga("SphereRepeatDepth_50x30.tga", 3, 10);
    }

    [Test]
    public void SphereRepeat()
    {
        var w = 1920;
        var h = 1080;
        var r = 0.5f;
        var sdf = 
            SdfExprs
            .Sphere(r, new Vector3(0.8f, 0.25f, 0.65f))
            .RepeatXY(
                2*r, 2*r,
                (d, c) => new Vector4(Vector3.Abs(c)/5f, d.W))
            .ToSdf();
        var rt = new Raytracer(w, h, sdf);
        using var img = rt.Render();
        img.SaveRgbTga($"SphereRepeat_{w}x{h}.tga");
    }

    [Test]
    public void CylinderRepeat()
    {
        var w = 192;
        var h = 108;
        var r = 0.5f;
        var sdf = 
            SdfExprs
            .Cylinder(r, r/4)
            .RepeatXY(2*r, r)
            .ToSdf();
        var rt = new Raytracer(w, h, sdf);
        using var img = rt.Render();
        img.SaveRgbTga($"CylinderRepeat_{w}x{h}.tga");
    }
}
