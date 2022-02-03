namespace Tests;

public class VolumeTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Zeros()
    {
        var sdf = (Vector3 p) => 0.0f;
        var v = Volume.SampleSdf(
            sdf,
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            5, 7, 11);
        Assert.AreEqual(5, v.GetLength(0));
        Assert.AreEqual(7, v.GetLength(1));
        Assert.AreEqual(11, v.GetLength(2));
    }

    [Test]
    public void SphereWidthSdf()
    {
        var r = 0.5f;
        var sdf = (Vector3 p) => p.Length() - r;
        var v = Volume.SampleSdf(
            sdf,
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            5, 5, 5);
        Assert.Less(v[3, 3, 3], -1.0e-6f);
    }

    [Test]
    public void Sphere()
    {
        var r = 0.5f;
        var sdf = (Vector3 p) => p.Length() - r;
        var v = Volume.SampleSphere(
            r,
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            5, 5, 5);
        Assert.Less(v[3, 3, 3], -1.0e-6f);
    }
}
