using System.Linq.Expressions;
using System.Reflection;

using SdfAction = System.Action<System.Memory<System.Numerics.Vector3>, System.Memory<float>>;
using SdfExpression = System.Linq.Expressions.Expression<SdfKit.SdfDelegate>;

namespace SdfKit;

public delegate float SdfDelegate(Vector3 p);

/// <summary>
/// An abstract signed distance function with boundaries. Implement the method SampleBatch to return distances for a batch of points.
/// </summary>
public abstract class Sdf
{
    public const int DefaultBatchSize = 2 * 1024;

    public abstract void SampleBatch(Memory<Vector3> points, Memory<float> distances);

    public void Sample(Memory<Vector3> points, Memory<float> distances, int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1)
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
            SampleBatch(pointsSlice, distancesSlice);
        });
    }

    public virtual Volume CreateVolume(Vector3 min, Vector3 max, int nx, int ny, int nz, int batchSize = DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        return Volume.SampleSdf(this, min, max, nx, ny, nz, batchSize, maxDegreeOfParallelism);
    }

    public Mesh CreateMesh(Vector3 min, Vector3 max, int nx, int ny, int nz, int batchSize = DefaultBatchSize, int maxDegreeOfParallelism = -1, float isoValue = 0.0f, int step = 1, IProgress<float>? progress = null)
    {
        var volume = CreateVolume(min, max, nx, ny, nz, batchSize, maxDegreeOfParallelism);
        return volume.CreateMesh(isoValue, step, progress);
    }

    public static ActionSdf FromAction(Action<Memory<Vector3>, Memory<float>> sdf)
    {
        return new ActionSdf(sdf);
    }

    static float VMax(Vector3 v)
    {
        return Math.Max(Math.Max(v.X, v.Y), v.Z);
    }

    public static Sdf Box(float bounds)
    {
        return Box(new Vector3(bounds, bounds, bounds));
    }

    public static Sdf Box(Vector3 bounds)
    {
        return Sdf.FromAction((ps, ds) =>
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
        });
    }

    public static SdfExpression CylinderExpression(float r, float h) =>
        p => MathF.Max(MathF.Sqrt(p.X * p.X + p.Z * p.Z) - r, MathF.Abs(p.Y) - h);

    public static Sdf Cylinder(float radius, float height, float padding = 0.0f)
    {
        var min = new Vector3(-radius - padding, 0 - padding, -radius - padding);
        var max = new Vector3(radius + padding, height + padding, radius + padding);
        return CylinderExpression(radius, height).ToSdf();
        // return Sdf.FromAction((ps, ds) =>
        // {
        //     int n = ps.Length;
        //     var p = ps.Span;
        //     var d = ds.Span;
        //     for (var i = 0; i < n; ++i)
        //     {
        //         var wd = MathF.Sqrt(p[i].X * p[i].X + p[i].Z * p[i].Z) - radius;
        //         d[i] = Math.Max(wd, MathF.Abs(p[i].Y) - height);
        //     }
        // }, min, max);
    }

    public static Sdf Plane(Vector3 normal, float distanceFromOrigin)
    {
        return Sdf.FromAction((ps, ds) =>
        {
            int n = ps.Length;
            var p = ps.Span;
            var d = ds.Span;
            for (var i = 0; i < n; ++i)
            {
                d[i] = Vector3.Dot(p[i], normal) + distanceFromOrigin;
            }
        });
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

    public static SdfExpression SphereExpression(float r) =>
        p => p.Length() - r;

    public static Sdf Sphere(float radius)
    {
        return Sdf.FromAction((ps, ds) =>
        {
            int n = ps.Length;
            var p = ps.Span;
            var d = ds.Span;
            for (var i = 0; i < n; ++i)
            {
                d[i] = p[i].Length() - radius;
            }
        });
    }

}

/// <summary>
/// A signed distance fuction that uses an Action to implement sampling.
/// </summary>
public class ActionSdf : Sdf
{
    SdfAction sampleAction;

    public ActionSdf(SdfAction action)
    {
        sampleAction = action;
    }

    public override void SampleBatch(Memory<Vector3> points, Memory<float> distances)
    {
        sampleAction(points, distances);
    }
}

public static class SdfExpressionExtensions
{
    static float Mod(float a, float b)
    {
        return a - b * MathF.Floor(a / b);
    }

    public static SdfExpression ChangePostion(this SdfExpression sdf, Expression<Func<Vector3, Vector3>> changePosition)
    {
        var p = Expression.Parameter(typeof(Vector3), "p");
        return Expression.Lambda<SdfDelegate>(
                Expression.Invoke(
                    sdf,
                    Expression.Invoke(
                        changePosition,
                        p)),
                new[]{p});
    }
    public static SdfExpression RepeatX(this SdfExpression sdf, float sizeX) =>
        sdf.ChangePostion(p => new Vector3(
            Mod((p.X + sizeX*0.5f), sizeX) - sizeX*0.5f,
            p.Y,
            p.Z));
    public static SdfExpression RepeatXY(this SdfExpression sdf, float sizeX, float sizeY)
    {
        return sdf.ChangePostion(p => new Vector3(
            Mod((p.X + sizeX*0.5f), sizeX) - sizeX*0.5f,
            Mod((p.Y + sizeY*0.5f), sizeY) - sizeY*0.5f,
            p.Z));
    }
    public static SdfExpression RepeatY(this SdfExpression sdf, float sizeY) =>
        sdf.ChangePostion(p => new Vector3(
            p.X,
            Mod((p.Y + sizeY*0.5f), sizeY) - sizeY*0.5f,
            p.Z));
    public static Sdf ToSdf(this SdfExpression expression)
    {
        return new ActionSdf(CompileSdfAction(expression));
    }
    public static SdfDelegate ToSdfDelegate(this SdfExpression expression)
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
    static readonly System.Reflection.MethodInfo SpanFloatGetter = typeof(SdfExpressionExtensions).GetMethod(nameof(SdfExpressionExtensions.SpanGetItem)).MakeGenericMethod(typeof(float));
    static readonly System.Reflection.MethodInfo SpanFloatSetter = typeof(SdfExpressionExtensions).GetMethod(nameof(SdfExpressionExtensions.SpanSetItem)).MakeGenericMethod(typeof(float));
    static readonly System.Reflection.MethodInfo SpanVector3Getter = typeof(SdfExpressionExtensions).GetMethod(nameof(SdfExpressionExtensions.SpanGetItem)).MakeGenericMethod(typeof(Vector3));
    public static SdfAction CompileSdfAction(this SdfExpression expression)
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
        var lambda = Expression.Lambda<SdfAction>(body, pm, dm);
        return lambda.Compile();
    }
}

public class SdfConfig
{
    public const int DefaultBatchSize = 2*1024;
}
