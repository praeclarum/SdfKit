namespace Tests;

public class LewinerTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Sphere10()
    {
        var volume = Volume.SampleSphere(1, 0.5f, 10, 10, 10);
        Assert.AreEqual(10, volume.GetLength(0));
        var luts = new LutProvider();
        var mc = new LewinerMarchingCubes(luts);
        var mesh = mc.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj("Sphere10.obj");
        Assert.AreEqual(222, mesh.Vertices.Length);
    }
}
