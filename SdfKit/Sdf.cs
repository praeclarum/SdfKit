namespace SdfKit;

/// <summary>
/// An abstract signed distance function with boundaries. Implement the method SampleBatch to return distances for a batch of points.
/// </summary>
public abstract class Sdf : IVolume
{
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

    public abstract void SampleBatch(Vector3[] points, float[] distances, int count);

    public virtual Volume CreateVolume(int nx, int ny, int nz, int batchSize = Volume.DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        var volume = new Volume(Min, Max, nx, ny, nz);
        volume.SampleSdf(SampleBatch, batchSize, maxDegreeOfParallelism);
        return volume;
    }

    public Mesh CreateMesh(int nx, int ny, int nz, int batchSize = Volume.DefaultBatchSize, int maxDegreeOfParallelism = -1, float isoValue = 0.0f, int step = 1, IProgress<float>? progress = null)
    {
        var volume = CreateVolume(nx, ny, nz, batchSize, maxDegreeOfParallelism);
        return volume.CreateMesh(isoValue, step, progress);
    }

    public static ActionSdf FromAction(Action<Vector3[], float[], int> sdf, Vector3 min, Vector3 max)
    {
        return new ActionSdf(sdf, min, max);
    }
}

public class ActionSdf : Sdf
{
    public delegate void SampleBatchDelegate(Vector3[] points, float[] distances, int count);

    Action<Vector3[], float[], int> sampleAction;

    public ActionSdf(Action<Vector3[], float[], int> action, Vector3 min, Vector3 max)
        : base(min, max)
    {
        sampleAction = action;
    }

    public override void SampleBatch(Vector3[] points, float[] distances, int count)
    {
        sampleAction(points, distances, count);
    }
}
