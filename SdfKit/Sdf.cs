using static SdfKit.VectorOps;

namespace SdfKit;


public delegate float SdfDistFunc(SdfInput p);
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

    public static Voxels ToVoxels(this Sdf sdf, Vector3 min, Vector3 max, int nx, int ny, int nz, int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1, bool clipToBounds = true)
    {
        var voxels = Voxels.SampleSdf(sdf, min, max, nx, ny, nz, batchSize, maxDegreeOfParallelism);
        if (clipToBounds)
        {
            voxels.ClipToBounds();
        }
        return voxels;
    }

    public static Mesh ToMesh(this Sdf sdf, Vector3 min, Vector3 max, int nx, int ny, int nz, int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1, bool clipToBounds = true, float isoValue = 0.0f, int step = 1, IProgress<float>? progress = null)
    {
        var voxels = ToVoxels(sdf, min, max, nx, ny, nz, batchSize, maxDegreeOfParallelism, clipToBounds);
        return voxels.ToMesh(isoValue, step, progress);
    }

    public static Vec3Data ToImage(this Sdf sdf,
        int width, int height,
        Matrix4x4 viewTransform,
        float verticalFieldOfViewDegrees = RayMarcher.DefaultVerticalFieldOfViewDegrees,
        float nearPlaneDistance = RayMarcher.DefaultNearPlaneDistance,
        float farPlaneDistance = RayMarcher.DefaultFarPlaneDistance,
        int depthIterations = RayMarcher.DefaultDepthIterations,
        int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        var rm = new RayMarcher(width, height, sdf, batchSize, maxDegreeOfParallelism) {
            ViewTransform = viewTransform,
            VerticalFieldOfViewDegrees = verticalFieldOfViewDegrees,
            NearPlaneDistance = nearPlaneDistance,
            FarPlaneDistance = farPlaneDistance,
            DepthIterations = depthIterations,
        };
        return rm.Render();
    }

    public static Vec3Data ToImage(this Sdf sdf,
        int width, int height,
        Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector,
        float verticalFieldOfViewDegrees = RayMarcher.DefaultVerticalFieldOfViewDegrees,
        float nearPlaneDistance = RayMarcher.DefaultNearPlaneDistance,
        float farPlaneDistance = RayMarcher.DefaultFarPlaneDistance,
        int depthIterations = RayMarcher.DefaultDepthIterations,
        int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        return ToImage(sdf,
            width, height,
            Matrix4x4.CreateLookAt(cameraPosition, cameraTarget, cameraUpVector),
            verticalFieldOfViewDegrees, nearPlaneDistance, farPlaneDistance,
            depthIterations,
            batchSize, maxDegreeOfParallelism);
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

    public static Sdf Solid(SdfDistFunc sdf, Vector3 color)
    {
        return (ps, ds) =>
        {
            int n = ps.Length;
            var p = ps.Span;
            var d = ds.Span;
            for (var i = 0; i < n; ++i)
            {
                d[i] = new Vector4(color, sdf(p[i]));
            }
        };
    }

    public static Sdf Solid(SdfDistFunc sdf) => Solid(sdf, Vector3.One);

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

public static class SdfFuncs
{
    public static SdfFunc Sphere(float radius)
    {
        return (p) =>
        {
            return new Vector4(Vector3.One, p.Length() - radius);
        };
    }
}

public static class SdfFuncEx
{
    public static SdfFunc ModifyInputAndOutput<T>(
        this SdfFunc sdf,
        SdfIndexedInputModifierFunc modInput,
        SdfIndexedOutputModifierFunc modOutput)
    {
        return (p) => {
            var i = modInput(p);
            var mp = i.Position;
            var d = sdf(mp);
            var mo = modOutput(i.Cell, mp, d);
            return new Vector4(mo, d.W);
        };
    }

    public static SdfFunc RepeatXY(this SdfFunc sdf, float sizeX, float sizeY, SdfIndexedOutputModifierFunc mod)
    {
        return sdf.ModifyInputAndOutput<Vector2>(
            p => new SdfIndexedInput
            {
                Position = new Vector3(
                    Mod((p.X + sizeX * 0.5f), sizeX) - sizeX * 0.5f,
                    Mod((p.Y + sizeY * 0.5f), sizeY) - sizeY * 0.5f,
                    p.Z),
                Cell = new Vector3(
                    MathF.Floor((p.X + sizeX * 0.5f) / sizeX),
                    MathF.Floor((p.Y + sizeY * 0.5f) / sizeY),
                    0.0f),
            },
            mod);
    }

    public static Sdf ToSdf(this SdfFunc sdf)
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
}

