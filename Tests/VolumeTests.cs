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
    public void OneIsCentered()
    {
        var sdf = (Vector3 p) => {
            Assert.AreEqual(0.0f, p.X, 0.001f);
            Assert.AreEqual(0.0f, p.Y, 0.001f);
            Assert.AreEqual(0.0f, p.Z, 0.001f);
            return 1.0f;
        };
        var v = Volume.SampleSdf(
            sdf,
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            1, 1, 1);
        Assert.AreEqual(1.0f, v[0,0,0]);
        Assert.AreEqual(1, v.GetLength(0));
        Assert.AreEqual(1, v.GetLength(1));
        Assert.AreEqual(1, v.GetLength(2));
    }

    [Test]
    public void TwoHasMinMax()
    {
        var sdf = (Vector3 p) => {
            Assert.AreEqual(1.0f, Math.Abs(p.X), 0.001f);
            Assert.AreEqual(1.0f, Math.Abs(p.Y), 0.001f);
            Assert.AreEqual(1.0f, Math.Abs(p.Z), 0.001f);
            return 1.0f;
        };
        var v = Volume.SampleSdf(
            sdf,
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            2, 2, 2);
        Assert.AreEqual(2, v.GetLength(0));
        Assert.AreEqual(2, v.GetLength(1));
        Assert.AreEqual(2, v.GetLength(2));
    }

    [Test]
    public void ThreeHasCenter()
    {
        var hasCenter = false;
        var sdf = (Vector3 p) => {
            if (p.Length() < 0.001f)
            {
                hasCenter = true;
            }
            return 1.0f;
        };
        var v = Volume.SampleSdf(
            sdf,
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            3, 3, 3);
        Assert.AreEqual(3, v.GetLength(0));
        Assert.AreEqual(3, v.GetLength(1));
        Assert.AreEqual(3, v.GetLength(2));
        Assert.IsTrue(hasCenter);
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
        Assert.Less(v[2, 2, 2], -1.0e-6f);
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
        Assert.Less(v[2, 2, 2], -1.0e-6f);
    }
}
