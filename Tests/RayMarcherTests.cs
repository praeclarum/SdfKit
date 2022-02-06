namespace Tests;

public class RayMarcherTests
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
        var rt = new RayMarcher(w, h, sdf);
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
        var rt = new RayMarcher(w, h, sdf);
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
        var rt = new RayMarcher(w, h, sdf);
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
        var rt = new RayMarcher(w, h, sdf);
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
        var rt = new RayMarcher(w, h, sdf);
        using var img = rt.RenderDepth();
        img.SaveDepthTga("SphereRepeatDepth_50x30.tga", 3, 10);
    }

    [Test]
    public void SphereRepeat()
    {
        var r = 0.5f;
        var sdf = 
            SdfExprs
            .Sphere(r)
            .RepeatXY(
                2.25f*r, 2.25f*r,
                (i, p, d) => 0.9f*Vector3.One - Vector3.Abs(i)/6f)
            .ToSdf();
        TimeRender(sdf, nameof(SphereRepeat));
    }

    [Test]
    public void SphereRepeatStatic()
    {
        var r = 0.5f;
        var sdf = 
            SdfFuncs
            .Sphere(r)
            .RepeatXY(
                2.25f*r, 2.25f*r,
                (i, p, d) => 0.9f*Vector3.One - Vector3.Abs(i)/6f)
            .ToSdf();
        TimeRender(sdf, nameof(SphereRepeatStatic));
    }

    void TimeRender(Sdf sdf, string name,
        int loops = 1,
        int w = 192,
        int h = 108)
    {
        var sw = new Stopwatch();
        Vec3Data? img = null;
        sw.Start();
        for (var i = 0; i < loops; i++) {
            if (i == 1)
                sw.Restart();
            img = sdf.ToImage(w, h,
                new Vector3(-2, 2, 4),
                Vector3.Zero,
                Vector3.UnitY);
        }
        sw.Stop();
        if (loops > 1) loops--;
        var millis = sw.ElapsedMilliseconds / (float)loops;
        System.IO.File.WriteAllText($"{name}Time.txt", $"Render time: {millis}ms\n");
        img?.SaveTga($"{name}_{w}x{h}.tga");
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
            .Color(0.95f, 0.95f, 0)
            .ToSdf();
        var rt = new RayMarcher(w, h, sdf);
        using var img = rt.Render();
        img.SaveTga($"CylinderRepeat_{w}x{h}.tga");
    }
}
