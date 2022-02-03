namespace SdfKit;

/// <summary>
/// An abstract signed distance function with boundaries. Implement the method SampleBatch to return distances for a batch of points.
/// </summary>
public abstract class Sdf : IVolume
{
    public const int DefaultBatchSize = 2*1024;

    public Vector3 Min { get; protected set; }
    public Vector3 Max { get; protected set; }
    public Vector3 Center => (Min + Max) * 0.5f;
    public Vector3 Size => Max - Min;
    public float Radius => (Max - Min).Length() * 0.5f;

    public Sdf(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public abstract void SampleBatch(Memory<Vector3> points, Memory<float> distances);

    public void Sample(Memory<Vector3> points, Memory<float> distances, int batchSize = DefaultBatchSize)
    {
        var i = 0;
        var ntotal = distances.Length;
        while (i < ntotal) {
            var n = Math.Min(batchSize, ntotal - i);
            SampleBatch(points.Slice(i, n), distances.Slice(i, n));
            i += n;
        }
    }

    public virtual Volume CreateVolume(int nx, int ny, int nz, int batchSize = DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        return Volume.SampleSdf(this, nx, ny, nz, batchSize, maxDegreeOfParallelism);
    }

    public Mesh CreateMesh(int nx, int ny, int nz, int batchSize = DefaultBatchSize, int maxDegreeOfParallelism = -1, float isoValue = 0.0f, int step = 1, IProgress<float>? progress = null)
    {
        var volume = CreateVolume(nx, ny, nz, batchSize, maxDegreeOfParallelism);
        return volume.CreateMesh(isoValue, step, progress);
    }

    public static ActionSdf FromAction(Action<Memory<Vector3>, Memory<float>> sdf, Vector3 min, Vector3 max)
    {
        return new ActionSdf(sdf, min, max);
    }

    public static Sdf CreateSphere(float radius, float padding = 0.0f)
    {
        var min = new Vector3(-radius - padding, -radius - padding, -radius - padding);
        var max = new Vector3(radius + padding, radius + padding, radius + padding);
        return Sdf.FromAction((ps, ds) =>
        {
            int n = ps.Length;
            var p = ps.Span;
            var d = ds.Span;
            for (var i = 0; i < n; ++i)
            {
                d[i] = p[i].Length() - radius;
            }
        }, min, max);
    }
}

/// <summary>
/// A signed distance fuction that uses an Action to implement sampling.
/// </summary>
public class ActionSdf : Sdf
{
    Action<Memory<Vector3>, Memory<float>> sampleAction;

    public ActionSdf(Action<Memory<Vector3>, Memory<float>> action, Vector3 min, Vector3 max)
        : base(min, max)
    {
        sampleAction = action;
    }

    public override void SampleBatch(Memory<Vector3> points, Memory<float> distances)
    {
        sampleAction(points, distances);
    }
}
