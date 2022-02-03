namespace Tests;

public class SdfTests
{
    [SetUp]
    public void Setup()
    {
    }


    [Test]
    public void CreateVolumeSphere()
    {
        var r = 0.5f;
        var sdf = Sdf.FromAction(
            (ps, ds, n) => {
                for (var i = 0; i < n; ++i)
                {
                    ds[i] = ps[i].Length() - r;
                }
            },
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1));
        var v = sdf.CreateVolume(128, 128, 128);
        Assert.AreEqual(-0.5f, v[63, 63, 63], 2.0e-2f);
    }

    [Test]
    public void CreateMeshSphere()
    {
        var r = 0.5f;
        var sdf = Sdf.FromAction(
            (ps, ds, n) => {
                for (var i = 0; i < n; ++i)
                {
                    ds[i] = ps[i].Length() - r;
                }
            },
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1));
        var mesh = sdf.CreateMesh(128, 128, 128);
        Assert.AreEqual(19008, mesh.Vertices.Length);
    }
}
