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
        var v = sdf.ToVoxels(new Vector3(-1, -1, -1), new Vector3(1, 1, 1), 128, 128, 128);
        Assert.AreEqual(-0.5f, v[63, 63, 63], 2.0e-2f);
    }

    [Test]
    public void CreateMeshSphere()
    {
        var r = 0.5f;
        var n = 32;
        var sdf = Sdfs.Sphere(r);
        var mesh = sdf.ToMesh(
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            n, n, n);
        Assert.AreEqual(1128, mesh.Vertices.Length);
    }

    [Test]
    public void SolidSphere()
    {
        var r = 0.5f;
        var n = 32;
        var sdf = SdfExprs.Solid(p => p.Length() - r).ToSdf();
        var mesh = sdf.ToMesh(
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            n, n, n);
        Assert.AreEqual(1128, mesh.Vertices.Length);
    }
}
