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
        var mesh = MarchingCubes.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj("Sphere5.obj");
        Assert.AreEqual(30, mesh.Vertices.Length);
    }

    [Test]
    public void Sphere10()
    {
        var volume = Volume.SampleSphere(1, 0.5f, 10, 10, 10);
        Assert.AreEqual(10, volume.GetLength(0));
        var mesh = MarchingCubes.CreateMesh(volume, 0.0f, 1);
        mesh.WriteObj("Sphere10.obj");
        Assert.AreEqual(192, mesh.Vertices.Length);
    }

    [Test]
    public void Sphere256Progress()
    {
        var volume = Volume.SampleSphere(1, 0.1f, 256, 256, 256);
        Assert.AreEqual(256, volume.GetLength(0));
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
        mesh.WriteObj("Sphere256.obj");
        Assert.AreEqual(253296, mesh.Vertices.Length);
        Assert.IsTrue(gotZero);
        Assert.IsTrue(gotOne);
    }
}
