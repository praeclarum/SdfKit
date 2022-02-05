namespace Tests;

public class MarchingCubesTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Sphere5()
    {
        var r = 1f;
        var volume = Volume.SampleSdf(
            Sdfs.Sphere(r),
            -1.5f * Vector3.One,
            1.5f * Vector3.One,
            5, 5, 5);
        Assert.AreEqual(5, volume.NX);
        var mesh = MarchingCubes.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj("Sphere5.obj");
        Assert.AreEqual(30, mesh.Vertices.Length);
        Assert.AreEqual(mesh.Center.Length(), 0.0f, 1e-6f);
        Assert.AreEqual(r, mesh.Size.X/2f, 1e-3f);
    }

    [Test]
    public void Sphere10()
    {
        var r = 2f;
        var volume = Volume.SampleSdf(
            Sdfs.Sphere(r),
            -2.5f * Vector3.One,
            2.5f * Vector3.One,
            10, 10, 10);
        Assert.AreEqual(10, volume.NX);
        var mesh = MarchingCubes.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj("Sphere10.obj");
        Assert.AreEqual(264, mesh.Vertices.Length);
        Assert.AreEqual(mesh.Center.Length(), 0.0f, 1e-6f);
        Assert.AreEqual(r, mesh.Size.X/2f, 1e-1f);
    }

    [Test]
    public void UnclippedSphere10()
    {
        var n = 10;
        var r = 2f;
        var volume = Volume.SampleSdf(
            Sdfs.Sphere(r),
            -Vector3.One,
            Vector3.One,
            n, n, n);
        Assert.AreEqual(n, volume.NX);
        var mesh = MarchingCubes.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj($"UnclippedSphere{n}.obj");
        Assert.AreEqual(0, mesh.Vertices.Length);
    }

    [Test]
    public void ClippedSphere10()
    {
        var n = 10;
        var r = 2f;
        var volume = Volume.SampleSdf(
            Sdfs.Sphere(r),
            -Vector3.One,
            Vector3.One,
            n, n, n);
        volume.Clip();
        Assert.AreEqual(n, volume.NX);
        var mesh = MarchingCubes.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj($"ClippedSphere{n}.obj");
        Assert.AreEqual(0, mesh.Vertices.Length);
        Assert.AreEqual(mesh.Center.Length(), 0.0f, 1e-6f);
        Assert.AreEqual(r, mesh.Size.X/2f, 1e-1f);
    }

    [Test]
    public void Box10()
    {
        var r = 2f;
        var volume = Volume.SampleSdf(
            Sdfs.Box(r),
            -2.5f * Vector3.One,
            2.5f * Vector3.One,
            10, 10, 10);
        Assert.AreEqual(10, volume.NX);
        var mesh = MarchingCubes.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj("Box10.obj");
        Assert.AreEqual(384, mesh.Vertices.Length);
        Assert.AreEqual(mesh.Center.Length(), 0.0f, 1e-6f);
        Assert.AreEqual(r, mesh.Size.X/2f, 1e-1f);
    }

    [Test]
    public void Cylinder50()
    {
        var n = 50;
        var sw = new Stopwatch();
        sw.Start();
        var volume = Volume.SampleSdf(
            Sdfs.Cylinder(1, 3),
            new Vector3(-1.5f, -3.5f, -1.5f),
            new Vector3(1.5f, 3.5f, 1.5f),
            n, n, n);
        sw.Stop();
        // Console.WriteLine($"SampleSdf: {sw.ElapsedMilliseconds}ms");
        Assert.AreEqual(n, volume.NX);
        var mesh = MarchingCubes.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj($"Cylinder{n}.obj");
        Assert.AreEqual(7056, mesh.Vertices.Length);
        Assert.AreEqual(0.0f, mesh.Center.X, 1e-6f);
        Assert.AreEqual(0.0f, mesh.Center.Y, 1e-6f);
        Assert.AreEqual(0.0f, mesh.Center.Z, 1e-6f);
        Assert.AreEqual(1, mesh.Size.X/2f, 1e-1f);
    }

    [Test]
    public void Sphere128Progress()
    {
        var r = 3f;
        var volume = Volume.SampleSdf(
            Sdfs.Sphere(r),
            -3.1f * Vector3.One,
            3.1f * Vector3.One,
            128, 128, 128);
        Assert.AreEqual(128, volume.NX);
        var gotZero = false;
        var gotOne = false;
        var progress = new Progress<float>(f => {
            Assert.GreaterOrEqual(f, 0.0f);
            Assert.LessOrEqual(f, 1.0f);
            if (f < 1e-6f)
            {
                gotZero = true;
            }
            else if (1.0f - f < 1e-6f)
            {
                gotOne = true;
            }
        });
        var mesh = MarchingCubes.CreateMesh(volume, 0.0f, 1, progress);
        mesh.WriteObj("Sphere128.obj");
        Assert.AreEqual(71016, mesh.Vertices.Length);
        Assert.IsTrue(gotZero);
        Assert.IsTrue(gotOne);
        Assert.AreEqual(mesh.Center.Length(), 0.0f, 1e-6f);
        Assert.AreEqual(r, mesh.Size.X/2f, 1e-3f);
    }
}
