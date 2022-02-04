using System.Linq.Expressions;
using SdfAction = System.Action<System.Memory<System.Numerics.Vector3>, System.Memory<float>>;
namespace SdfKit;

/// <summary>
/// An abstract signed distance function with boundaries. Implement the method SampleBatch to return distances for a batch of points.
/// </summary>
public abstract class Sdf : IVolume
{
    public const int DefaultBatchSize = 2 * 1024;

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

    public void Sample(Memory<Vector3> points, Memory<float> distances, int batchSize = DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        var i = 0;
        var ntotal = distances.Length;
        while (i < ntotal)
        {
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

    static float VMax(Vector3 v)
    {
        return Math.Max(Math.Max(v.X, v.Y), v.Z);
    }

    public static Sdf Box(float bounds, float padding = 0.0f)
    {
        return Box(new Vector3(bounds, bounds, bounds), padding);
    }

    public static Sdf Box(Vector3 bounds, float padding = 0.0f)
    {
        var min = new Vector3(-bounds.X - padding, -bounds.Y - padding, -bounds.Z - padding);
        var max = new Vector3(bounds.X + padding, bounds.Y + padding, bounds.Z + padding);
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
        }, min, max);
    }

    delegate float SdfDelegate(Vector3 p);

    static Expression<SdfDelegate> CylinderExpression(float r, float h) =>
        p => MathF.Max(MathF.Sqrt(p.X * p.X + p.Z * p.Z) - r, MathF.Abs(p.Y) - h);

    public static Sdf Cylinder(float radius, float height, float padding = 0.0f)
    {
        var min = new Vector3(-radius - padding, 0 - padding, -radius - padding);
        var max = new Vector3(radius + padding, height + padding, radius + padding);
        SdfAction action = CompileSdfExpression(CylinderExpression(radius, height));
        return Sdf.FromAction(action, min, max);
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

    static readonly System.Reflection.PropertyInfo SpanOfFloatMemory = typeof(Memory<float>).GetProperty(nameof(Memory<float>.Span));
    static readonly System.Reflection.PropertyInfo LengthOfFloatMemory = typeof(Memory<float>).GetProperty(nameof(Memory<float>.Length));
    static readonly System.Reflection.PropertyInfo SpanOfVector3Memory = typeof(Memory<Vector3>).GetProperty(nameof(Memory<Vector3>.Span));

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static T SpanGetItem<T>(Span<T> span, int index) => span[index];
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static T SpanSetItem<T>(Span<T> span, int index, T value) => span[index] = value;
    static readonly System.Reflection.MethodInfo SpanFloatGetter = typeof(Sdf).GetMethod(nameof(Sdf.SpanGetItem)).MakeGenericMethod(typeof(float));
    static readonly System.Reflection.MethodInfo SpanFloatSetter = typeof(Sdf).GetMethod(nameof(Sdf.SpanSetItem)).MakeGenericMethod(typeof(float));
    static readonly System.Reflection.MethodInfo SpanVector3Getter = typeof(Sdf).GetMethod(nameof(Sdf.SpanGetItem)).MakeGenericMethod(typeof(Vector3));
    static SdfAction CompileSdfExpression(Expression<SdfDelegate> expression)
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

    public static Sdf Plane(Vector3 normal, float distanceFromOrigin, Vector3 min, Vector3 max)
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
        }, min, max);
    }

    public static Sdf PlaneXY(float zmin, float zmax, float xbound, float ybound, float padding = 0.0f)
    {
        return Plane(
            new Vector3(0, 0, 1),
            zmax,
            new Vector3(-xbound - padding, -ybound - padding, zmin - padding),
            new Vector3(xbound + padding, ybound + padding, zmax + padding));
    }

    public static Sdf PlaneXZ(float ymin, float ymax, float xbound, float zbound, float padding = 0.0f)
    {
        return Plane(
            new Vector3(0, 1, 0),
            ymax,
            new Vector3(-xbound - padding, ymin - padding, -zbound - padding),
            new Vector3(xbound + padding, ymax + padding, zbound + padding));
    }

    public static Sdf Sphere(float radius, float padding = 0.0f)
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
    SdfAction sampleAction;

    public ActionSdf(SdfAction action, Vector3 min, Vector3 max)
        : base(min, max)
    {
        sampleAction = action;
    }

    public override void SampleBatch(Memory<Vector3> points, Memory<float> distances)
    {
        sampleAction(points, distances);
    }
}
