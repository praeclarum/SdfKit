using static SdfKit.VectorOps;

namespace SdfKit;


public delegate SdfOutput SdfFunc(SdfInput p);
public delegate void Sdf(Memory<SdfInput> points, Memory<SdfOutput> colorsAndDistances);


public class SdfConfig
{
    public const int DefaultBatchSize = 2*1024;
}


/// <summary>
/// An abstract signed distance function with boundaries. Implement the method SampleBatch to return distances for a batch of points.
/// </summary>
public static class SdfEx
{
    public static void Sample(this Sdf sdf, Memory<Vector3> points, Memory<Vector4> distances, int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        var ntotal = distances.Length;
        var numBatches = (ntotal + batchSize - 1) / batchSize;
        var options = new System.Threading.Tasks.ParallelOptions { 
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
        };
        System.Threading.Tasks.Parallel.For(0, numBatches, options, ib => {
            // Console.WriteLine($"Batch {ib} of {numBatches}");
            var startI = ib * batchSize;
            var endI = Math.Min(ntotal, startI + batchSize);
            var pointsSlice = points.Slice(startI, endI - startI);
            var distancesSlice = distances.Slice(startI, endI - startI);
            sdf(pointsSlice, distancesSlice);
        });
    }

    public static Volume CreateVolume(this Sdf sdf, Vector3 min, Vector3 max, int nx, int ny, int nz, int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        return Volume.SampleSdf(sdf, min, max, nx, ny, nz, batchSize, maxDegreeOfParallelism);
    }

    public static Mesh CreateMesh(this Sdf sdf, Vector3 min, Vector3 max, int nx, int ny, int nz, int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1, bool clipToVolume = true, float isoValue = 0.0f, int step = 1, IProgress<float>? progress = null)
    {
        var volume = CreateVolume(sdf, min, max, nx, ny, nz, batchSize, maxDegreeOfParallelism);
        if (clipToVolume)
        {
            volume.Clip();
        }
        return volume.CreateMesh(isoValue, step, progress);
    }
}

public static class Sdfs
{
    public static Sdf Box(float bounds)
    {
        return Box(new Vector3(bounds, bounds, bounds));
    }

    public static Sdf Box(Vector3 bounds)
    {
        return (ps, ds) =>
        {
            int n = ps.Length;
            var p = ps.Span;
            var d = ds.Span;
            for (var i = 0; i < n; ++i)
            {
                var wd = Vector3.Abs(p[i]) - bounds;
                d[i].W = Vector3.Max(wd, Vector3.Zero).Length() +
                         VMax(Vector3.Min(wd, Vector3.Zero));
            }
        };
    }

    public static Sdf Cylinder(float radius, float height) =>
        SdfExprs.Cylinder(radius, height).ToSdf();

    public static Sdf Plane(Vector3 normal, float distanceFromOrigin)
    {
        return (ps, ds) =>
        {
            int n = ps.Length;
            var p = ps.Span;
            var d = ds.Span;
            for (var i = 0; i < n; ++i)
            {
                d[i].W = Vector3.Dot(p[i], normal) + distanceFromOrigin;
            }
        };
    }

    public static Sdf PlaneXY(float z = 0)
    {
        return Plane(
            new Vector3(0, 0, 1),
            z);
    }

    public static Sdf PlaneXZ(float y = 0)
    {
        return Plane(
            new Vector3(0, 1, 0),
            y);
    }

    public static Sdf Solid(SdfFunc sdf)
    {
        return (ps, ds) =>
        {
            int n = ps.Length;
            var p = ps.Span;
            var d = ds.Span;
            for (var i = 0; i < n; ++i)
            {
                d[i] = sdf(p[i]);
            }
        };
    }

    public static Sdf Sphere(float radius)
    {
        return (ps, ds) =>
        {
            int n = ps.Length;
            var p = ps.Span;
            var d = ds.Span;
            for (var i = 0; i < n; ++i)
            {
                d[i].W = p[i].Length() - radius;
            }
        };
    }
}

