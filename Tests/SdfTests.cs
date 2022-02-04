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
        Sdf sdf = (ps, ds) => {
            int n = ps.Length;
            var p = ps.Span;
            var d = ds.Span;
            for (var i = 0; i < n; ++i)
            {
                d[i] = new Vector4(1, 1, 1, p[i].Length() - r);
            }
        };
        var v = sdf.CreateVolume(new Vector3(-1, -1, -1), new Vector3(1, 1, 1), 128, 128, 128);
        Assert.AreEqual(-0.5f, v[63, 63, 63], 2.0e-2f);
    }

    [Test]
    public void CreateMeshSphere()
    {
        var r = 0.5f;
        var sdf = Sdfs.Sphere(r);
        var mesh = sdf.CreateMesh(
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            128, 128, 128);
        Assert.AreEqual(19008, mesh.Vertices.Length);
    }
}
