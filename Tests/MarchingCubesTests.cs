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
        var volume = Volume.SampleSdf(Sdf.Sphere(r, 0.5f), 5, 5, 5);
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
        var volume = Volume.SampleSdf(Sdf.Sphere(r, 0.5f), 10, 10, 10);
        Assert.AreEqual(10, volume.NX);
        var mesh = MarchingCubes.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj("Sphere10.obj");
        Assert.AreEqual(264, mesh.Vertices.Length);
        Assert.AreEqual(mesh.Center.Length(), 0.0f, 1e-6f);
        Assert.AreEqual(r, mesh.Size.X/2f, 1e-1f);
    }

    [Test]
    public void Box10()
    {
        var r = 2f;
        var volume = Volume.SampleSdf(Sdf.Box(r, 0.5f), 10, 10, 10);
        Assert.AreEqual(10, volume.NX);
        var mesh = MarchingCubes.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj("Box10.obj");
        Assert.AreEqual(384, mesh.Vertices.Length);
        Assert.AreEqual(mesh.Center.Length(), 0.0f, 1e-6f);
        Assert.AreEqual(r, mesh.Size.X/2f, 1e-1f);
    }

    [Test]
    public void Sphere128Progress()
    {
        var r = 3f;
        var volume = Volume.SampleSdf(Sdf.Sphere(r, 0.1f), 128, 128, 128);
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
