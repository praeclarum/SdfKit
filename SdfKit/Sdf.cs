using System.Linq.Expressions;
using SdfExpr = System.Linq.Expressions.Expression<SdfKit.SdfFunc>;
using static SdfKit.VectorOps;

namespace SdfKit;


public delegate float SdfFunc(Vector3 p);
public delegate void Sdf(System.Memory<System.Numerics.Vector3> points, System.Memory<float> distances);


public class SdfConfig
{
    public const int DefaultBatchSize = 2*1024;
}

/// <summary>
/// An abstract signed distance function with boundaries. Implement the method SampleBatch to return distances for a batch of points.
/// </summary>
public static class SdfExtensions
{
    public static void Sample(this Sdf sdf, Memory<Vector3> points, Memory<float> distances, int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1)
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

    public static Mesh CreateMesh(this Sdf sdf, Vector3 min, Vector3 max, int nx, int ny, int nz, int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1, float isoValue = 0.0f, int step = 1, IProgress<float>? progress = null)
    {
        var volume = CreateVolume(sdf, min, max, nx, ny, nz, batchSize, maxDegreeOfParallelism);
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
                d[i] = Vector3.Max(wd, Vector3.Zero).Length() +
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
                d[i] = Vector3.Dot(p[i], normal) + distanceFromOrigin;
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
                d[i] = p[i].Length() - radius;
            }
        };
    }
}

public static class SdfExprs
{
    public static SdfExpr Cylinder(float r, float h) =>
        p => MathF.Max(MathF.Sqrt(p.X * p.X + p.Z * p.Z) - r, MathF.Abs(p.Y) - h);

    public static SdfExpr Sphere(float r) =>
        p => p.Length() - r;

}

public static class SdfExprExtensions
{
    public static SdfExpr ChangePostion(this SdfExpr sdf, Expression<Func<Vector3, Vector3>> changePosition)
    {
        var p = Expression.Parameter(typeof(Vector3), "p");
        return Expression.Lambda<SdfFunc>(
                Expression.Invoke(
                    sdf,
                    Expression.Invoke(
                        changePosition,
                        p)),
                new[]{p});
    }
    public static SdfExpr RepeatX(this SdfExpr sdf, float sizeX) =>
        sdf.ChangePostion(p => new Vector3(
            Mod((p.X + sizeX*0.5f), sizeX) - sizeX*0.5f,
            p.Y,
            p.Z));
    public static SdfExpr RepeatXY(this SdfExpr sdf, float sizeX, float sizeY)
    {
        return sdf.ChangePostion(p => new Vector3(
            Mod((p.X + sizeX*0.5f), sizeX) - sizeX*0.5f,
            Mod((p.Y + sizeY*0.5f), sizeY) - sizeY*0.5f,
            p.Z));
    }
    public static SdfExpr RepeatY(this SdfExpr sdf, float sizeY) =>
        sdf.ChangePostion(p => new Vector3(
            p.X,
            Mod((p.Y + sizeY*0.5f), sizeY) - sizeY*0.5f,
            p.Z));
    public static SdfFunc ToSdfFunc(this SdfExpr expression)
    {
        return expression.Compile();
    }

    static readonly System.Reflection.PropertyInfo SpanOfFloatMemory = typeof(Memory<float>).GetProperty(nameof(Memory<float>.Span));
    static readonly System.Reflection.PropertyInfo LengthOfFloatMemory = typeof(Memory<float>).GetProperty(nameof(Memory<float>.Length));
    static readonly System.Reflection.PropertyInfo SpanOfVector3Memory = typeof(Memory<Vector3>).GetProperty(nameof(Memory<Vector3>.Span));

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static T SpanGetItem<T>(Span<T> span, int index) => span[index];
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static T SpanSetItem<T>(Span<T> span, int index, T value) => span[index] = value;
    static readonly System.Reflection.MethodInfo SpanFloatGetter = typeof(SdfExprExtensions).GetMethod(nameof(SdfExprExtensions.SpanGetItem)).MakeGenericMethod(typeof(float));
    static readonly System.Reflection.MethodInfo SpanFloatSetter = typeof(SdfExprExtensions).GetMethod(nameof(SdfExprExtensions.SpanSetItem)).MakeGenericMethod(typeof(float));
    static readonly System.Reflection.MethodInfo SpanVector3Getter = typeof(SdfExprExtensions).GetMethod(nameof(SdfExprExtensions.SpanGetItem)).MakeGenericMethod(typeof(Vector3));
    public static Sdf ToSdf(this SdfExpr expression)
    {
        var pm = Expression.Parameter(typeof(Memory<Vector3>), "pm");
        var dm = Expression.Parameter(typeof(Memory<float>), "dm");
        var ps = Expression.Variable(typeof(Span<Vector3>), "ps");
        var ds = Expression.Variable(typeof(Span<float>), "ds");
        var p = Expression.Variable(typeof(Vector3), "p");
        var i = Expression.Variable(typeof(int), "i");
        var n = Expression.Variable(typeof(int), "n");
        var init = Expression.Block(
            Expression.Assign(ps, Expression.Property(pm, SpanOfVector3Memory)),
            Expression.Assign(ds, Expression.Property(dm, SpanOfFloatMemory)),
            Expression.Assign(n, Expression.Property(dm, LengthOfFloatMemory)),
            Expression.Assign(i, Expression.Constant(0)));
        var loopLabel = Expression.Label("loop");
        var loop = Expression.Loop(
            Expression.IfThenElse(
                Expression.LessThan(i, n),
                Expression.Block(
                    Expression.Assign(p, Expression.Call(SpanVector3Getter, ps, i)),
                    Expression.Call(SpanFloatSetter, ds, i,
                        Expression.Invoke(expression, p)),
                    Expression.PostIncrementAssign(i)),
                Expression.Break(loopLabel)),
            loopLabel);
        var body = Expression.Block(
            new[] { ps, ds, p, i, n },
            init,
            loop);
        var lambda = Expression.Lambda<Sdf>(body, pm, dm);
        return lambda.Compile();
    }
}
