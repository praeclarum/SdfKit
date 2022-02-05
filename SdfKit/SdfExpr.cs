using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using static SdfKit.VectorOps;
using static SdfKit.SdfExprMembers;

namespace SdfKit;

public delegate SdfInput SdfInputModifierFunc(SdfInput position);
public delegate SdfColor SdfOutputModifierFunc(SdfInput position, SdfOutput output);

public delegate SdfIndexedInput SdfIndexedInputModifierFunc(SdfInput position);
public delegate SdfColor SdfIndexedOutputModifierFunc(SdfIndex index, SdfInput position, SdfOutput output);

public static class SdfExprs
{
    public static SdfExpr Cylinder(float r, float h, Vector3 color) =>
        p => new Vector4(color, MathF.Max(MathF.Sqrt(p.X * p.X + p.Z * p.Z) - r, MathF.Abs(p.Y) - h));

    public static SdfExpr Cylinder(float r, float h) =>
        p => new Vector4(1, 1, 1, MathF.Max(MathF.Sqrt(p.X * p.X + p.Z * p.Z) - r, MathF.Abs(p.Y) - h));

    static readonly ConstructorInfo Vector4Ctor = typeof(Vector4).GetConstructor(new[]{typeof(Vector3),typeof(float)});
    public static SdfExpr Solid(SdfDistExpr sdf, Vector3 color)
    {
        var p = Expression.Parameter(typeof(SdfInput), "p");
        return Expression.Lambda<SdfFunc>(
            Expression.New(Vector4Ctor,
                Expression.Constant(color),
                Expression.Invoke(sdf, p)),
            p);
    }

    public static SdfExpr Solid(SdfDistExpr sdf) => Solid(sdf, Vector3.One);

    public static SdfExpr Sphere(float r, Vector3 color) =>
        p => new Vector4(color, p.Length() - r);

    public static SdfExpr Sphere(float r) =>
        p => new Vector4(1, 1, 1, p.Length() - r);
}

public struct SdfIndexedInput
{
    public SdfInput Position;
    public Vector3 Cell;
}

public static class SdfExprEx
{
    public static SdfExpr ModifyInput(this SdfExpr sdf, Expression<SdfInputModifierFunc> changePosition)
    {
        var p = Expression.Parameter(typeof(Vector3), "p");
        return Expression.Lambda<SdfFunc>(
                Expression.Invoke(
                    sdf,
                    Expression.Invoke(
                        changePosition,
                        p)),
                p);
    }

    public static SdfExpr ModifyOutput(this SdfExpr sdf, Expression<SdfOutputModifierFunc> mod)
    {
        var p = Expression.Parameter(typeof(Vector3), "p");

        var d = Expression.Variable(typeof(SdfOutput), "d");
        var mo = Expression.Variable(typeof(SdfColor), "mo");

        return Expression.Lambda<SdfFunc>(
            Expression.Block(
                new[] { d, mo },
                Expression.Assign(d, Expression.Invoke(sdf, p)),
                Expression.Assign(mo, 
                    Expression.Invoke(
                        mod,
                        p,
                        d)),
                Expression.New(Vector4Ctor,
                    mo,
                    Expression.Field(d, WOfVector4))),
            new[] { p });
    }

    public static SdfExpr ModifyInputAndOutput<T>(
        this SdfExpr sdf,
        Expression<SdfIndexedInputModifierFunc> modInput,
        Expression<SdfIndexedOutputModifierFunc> modOutput)
    {
        var p = Expression.Parameter(typeof(Vector3), "p");

        var mp = Expression.Variable(typeof(Vector3), "mp");
        var d = Expression.Variable(typeof(SdfOutput), "d");
        var i = Expression.Variable(typeof(SdfIndexedInput), "i");
        var mo = Expression.Variable(typeof(SdfColor), "mo");

        return Expression.Lambda<SdfFunc>(
            Expression.Block(
                new[] { mp, d, i, mo },
                Expression.Assign(i, Expression.Invoke(modInput, p)),
                Expression.Assign(mp, Expression.Field(i, PositionOfInstance)),
                Expression.Assign(d, Expression.Invoke(sdf, mp)),
                Expression.Assign(mo, 
                    Expression.Invoke(
                        modOutput,
                        Expression.Field(i, CellOfInstance),
                        mp,
                        d)),
                Expression.New(Vector4Ctor,
                    mo,
                    Expression.Field(d, WOfVector4))),
            new[] { p });
    }

    public static SdfExpr Color(this SdfExpr sdf, Vector3 color) =>
        sdf.ModifyOutput((p, d) => color);

    public static SdfExpr Color(this SdfExpr sdf, float r, float g, float b) =>
        sdf.ModifyOutput((p, d) => new Vector3(r, g, b));

    public static SdfExpr RepeatX(this SdfExpr sdf, float sizeX) =>
        sdf.ModifyInput(p => new Vector3(
            Mod((p.X + sizeX * 0.5f), sizeX) - sizeX * 0.5f,
            p.Y,
            p.Z));

    public static SdfExpr RepeatXY(this SdfExpr sdf, float sizeX, float sizeY)
    {
        return sdf.ModifyInput(p => new Vector3(
            Mod((p.X + sizeX * 0.5f), sizeX) - sizeX * 0.5f,
            Mod((p.Y + sizeY * 0.5f), sizeY) - sizeY * 0.5f,
            p.Z));
    }

    public static SdfExpr RepeatXY(this SdfExpr sdf, float sizeX, float sizeY, Expression<SdfIndexedOutputModifierFunc> mod)
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

    public static SdfExpr RepeatY(this SdfExpr sdf, float sizeY) =>
        sdf.ModifyInput(p => new Vector3(
            p.X,
            Mod((p.Y + sizeY * 0.5f), sizeY) - sizeY * 0.5f,
            p.Z));

    public static SdfFunc ToSdfFunc(this SdfExpr expression)
    {
        return expression.Compile();
    }

    public static Sdf ToSdf(this SdfExpr expression)
    {
        return SdfExprCompiler.Compile(expression);
    }
}

static class SdfExprMembers
{
    public static readonly ConstructorInfo Vector4Ctor = typeof(Vector4).GetConstructor(new[]{typeof(Vector3),typeof(float)});
    public static readonly PropertyInfo SpanOfOutputMemory = typeof(Memory<SdfOutput>).GetProperty(nameof(Memory<SdfOutput>.Span));
    public static readonly PropertyInfo LengthOfInputMemory = typeof(Memory<SdfInput>).GetProperty(nameof(Memory<SdfInput>.Length));
    public static readonly PropertyInfo SpanOfInputMemory = typeof(Memory<SdfInput>).GetProperty(nameof(Memory<SdfInput>.Span));
    public static readonly FieldInfo PositionOfInstance = typeof(SdfIndexedInput).GetField(nameof(SdfIndexedInput.Position));
    public static readonly FieldInfo CellOfInstance = typeof(SdfIndexedInput).GetField(nameof(SdfIndexedInput.Cell));
    public static readonly FieldInfo WOfVector4 = typeof(Vector4).GetField(nameof(Vector4.W));
}

static class SdfExprCompiler
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T SpanGetItem<T>(Span<T> span, int index) => span[index];
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T SpanSetItem<T>(Span<T> span, int index, T value) => span[index] = value;
    static readonly MethodInfo SpanOutputSetter = typeof(SdfExprCompiler).GetMethod(nameof(SdfExprCompiler.SpanSetItem)).MakeGenericMethod(typeof(SdfOutput));
    static readonly MethodInfo SpanInputGetter = typeof(SdfExprCompiler).GetMethod(nameof(SdfExprCompiler.SpanGetItem)).MakeGenericMethod(typeof(SdfInput));

    public static Sdf Compile(SdfExpr expression)
    {
        var lambda = CreateBatchedLambda(expression);
        return lambda.Compile(preferInterpretation: false);
    }

    static Expression<Sdf> CreateBatchedLambda(SdfExpr expression)
    {
        var pm = Expression.Parameter(typeof(Memory<SdfInput>), "pm");
        var dm = Expression.Parameter(typeof(Memory<SdfOutput>), "dm");
        var ps = Expression.Variable(typeof(Span<SdfInput>), "ps");
        var ds = Expression.Variable(typeof(Span<SdfOutput>), "ds");
        var p = Expression.Variable(typeof(SdfInput), "p");
        var i = Expression.Variable(typeof(int), "i");
        var n = Expression.Variable(typeof(int), "n");
        var init = Expression.Block(
            Expression.Assign(ps, Expression.Property(pm, SpanOfInputMemory)),
            Expression.Assign(ds, Expression.Property(dm, SpanOfOutputMemory)),
            Expression.Assign(n, Expression.Property(pm, LengthOfInputMemory)),
            Expression.Assign(i, Expression.Constant(0)));
        var loopLabel = Expression.Label("loop");
        var loop = Expression.Loop(
            Expression.IfThenElse(
                Expression.LessThan(i, n),
                Expression.Block(
                    Expression.Assign(p, Expression.Call(SpanInputGetter, ps, i)),
                    Expression.Call(SpanOutputSetter, ds, i,
                        Expression.Invoke(expression, p)),
                    Expression.PostIncrementAssign(i)),
                Expression.Break(loopLabel)),
            loopLabel);
        var body = Expression.Block(
            new[] { ps, ds, p, i, n },
            init,
            loop);
        var lambda = Expression.Lambda<Sdf>(body, pm, dm);
        return lambda;
    }

}
