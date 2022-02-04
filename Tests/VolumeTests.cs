namespace Tests;

public class VolumeTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Empty()
    {
        var sdf = (Vector3 p) => 0.0f;
        var v = new Volume(
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            5, 7, 11);
        Assert.AreEqual(5, v.NX);
        Assert.AreEqual(7, v.NY);
        Assert.AreEqual(11, v.NZ);
        Assert.AreEqual(2f, v.Size.X, 1e-6f);
        Assert.AreEqual(2f, v.Size.Y, 1e-6f);
        Assert.AreEqual(2f, v.Size.Z, 1e-6f);
    }

    [Test]
    public void ZeroSdf()
    {
        var sdf = (Vector3 p) => 0.0f;
        var v = Volume.SampleSdf(
            sdf,
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            5, 7, 11);
        Assert.AreEqual(5, v.NX);
        Assert.AreEqual(7, v.NY);
        Assert.AreEqual(11, v.NZ);
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
        Assert.AreEqual(1, v.NX);
        Assert.AreEqual(1, v.NY);
        Assert.AreEqual(1, v.NZ);
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
        Assert.AreEqual(2, v.NX);
        Assert.AreEqual(2, v.NY);
        Assert.AreEqual(2, v.NZ);
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
        Assert.AreEqual(3, v.NX);
        Assert.AreEqual(3, v.NY);
        Assert.AreEqual(3, v.NZ);
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
        Assert.AreEqual(-0.5f, v[2, 2, 2], 1.0e-3f);
    }

    [Test]
    public void Sphere()
    {
        var r = 0.5f;
        var sdf = (Vector3 p) => p.Length() - r;
        var v = Volume.SampleSdf(
            sdf,
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            5, 5, 5);
        Assert.AreEqual(-0.5f, v[2, 2, 2], 1.0e-3f);
    }

    [Test]
    public void SphereWithBatchSize()
    {
        var r = 0.5f;
        Sdf sdf = (Memory<Vector3> ps, Memory<float> ds) => {
            // Console.WriteLine($"Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}, N = {n}");
            int n = ps.Length;
            var p = ps.Span;
            var d = ds.Span;
            if (n != 22)
                Assert.AreEqual(70, n);
            for (var i = 0; i < n; ++i)
            {
                d[i] = p[i].Length() - r;
            }
        };
        var sw = new Stopwatch();
        sw.Start();
        var v = Volume.SampleSdf(
            sdf,
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            128, 128, 128,
            batchSize: 70);
        sw.Stop();
        Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
        Assert.AreEqual(-0.5f, v[63, 63, 63], 2.0e-2f);
    }
}
