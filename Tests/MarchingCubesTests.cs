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
        var volume = Volume.SampleSphere(1, 0.5f, 5, 5, 5);
        Assert.AreEqual(5, volume.GetLength(0));
        var luts = new LutProvider();
        var mc = new MarchingCubes(luts);
        var mesh = mc.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj("Sphere5.obj");
        Assert.AreEqual(30, mesh.Vertices.Length);
    }

    [Test]
    public void Sphere10()
    {
        var volume = Volume.SampleSphere(1, 0.5f, 10, 10, 10);
        Assert.AreEqual(10, volume.GetLength(0));
        var luts = new LutProvider();
        var mc = new MarchingCubes(luts);
        var mesh = mc.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj("Sphere10.obj");
        Assert.AreEqual(192, mesh.Vertices.Length);
    }

    [Test]
    public void Sphere256()
    {
        var volume = Volume.SampleSphere(1, 0.1f, 256, 256, 256);
        Assert.AreEqual(256, volume.GetLength(0));
        var luts = new LutProvider();
        var mc = new MarchingCubes(luts);
        var mesh = mc.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj("Sphere256.obj");
        Assert.AreEqual(253296, mesh.Vertices.Length);
    }
}
