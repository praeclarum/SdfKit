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
        Assert.AreEqual(-0.5f, v[2, 2, 2], 1.0e-3f);
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
        Assert.AreEqual(-0.5f, v[2, 2, 2], 1.0e-3f);
    }

    [Test]
    public void SphereWidthSdfPlanes()
    {
        var r = 0.5f;
        var sdf = (Vector3[] ps, float[] ds) => {
            var n = ps.Length;
            for (var i = 0; i < n; ++i)
            {
                ds[i] = ps[i].Length() - r;
            }
        };
        var v = Volume.SampleSdfZPlanes(
            sdf,
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            5, 5, 5,
            maxDegreeOfParallelism: 2);
        Assert.AreEqual(-0.5f, v[2, 2, 2], 1.0e-3f);
    }

    [Test]
    public void SphereWidthSdfBatch()
    {
        var r = 0.5f;
        var sdf = (Vector3[] ps, float[] ds, int n) => {
            // Console.WriteLine($"Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}, N = {n}");
            if (n != 36)
                Assert.AreEqual(70, n);
            for (var i = 0; i < n; ++i)
            {
                ds[i] = ps[i].Length() - r;
            }
        };
        var sw = new Stopwatch();
        sw.Start();
        var v = Volume.SampleSdfBatches(
            sdf,
            new Vector3(-1, -1, -1),
            new Vector3(1, 1, 1),
            256, 256, 256,
            batchSize: 70,
            maxDegreeOfParallelism: -1);
        sw.Stop();
        Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
        Assert.AreEqual(-0.5f, v[127, 127, 127], 1.0e-2f);
    }
}
