using System.Buffers;
using System.Runtime.InteropServices;

#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace SdfKit;

public class VectorData : IDisposable
{
    public readonly float[] Values;
    public readonly int Width;
    public readonly int Height;
    public readonly int Dimensions;
    public readonly int Length;
    public readonly ArrayPool<float> Pool;
    bool returned;

    public Memory<float> FloatMemory => Values.AsMemory(0, Length);

    protected VectorData(int width, int height, int dim, ArrayPool<float> pool)
    {
        this.Width = width;
        this.Height = height;
        this.Dimensions = dim;
        this.Pool = pool;
        Length = width * height * dim;
        Values = pool.Rent(Length);
    }

    public void Dispose()
    {
        Return();
    }

    public void Return()
    {
        if (!returned) {
            Pool.Return(Values);
            returned = true;
        }
    }
    public VectorData AddInplace(float other)
    {
        var n = Length;
        for (int i = 0; i < n; i++) {
            Values[i] += other;
        }
        return this;
    }
    public VectorData SubtractInplace(float other)
    {
        var n = Length;
        for (int i = 0; i < n; i++) {
            Values[i] -= other;
        }
        return this;
    }
    public VectorData MultiplyInplace(float other)
    {
        var n = Length;
        for (int i = 0; i < n; i++) {
            Values[i] *= other;
        }
        return this;
    }
    public VectorData DivideInplace(float other)
    {
        var n = Length;
        for (int i = 0; i < n; i++) {
            Values[i] /= other;
        }
        return this;
    }

    protected VectorData GenericAddInplace(VectorData other)
    {
        var n = Length;
        Debug.Assert(Length == other.Length);
        for (int i = 0; i < n; i++) {
            Values[i] += other.Values[i];
        }
        return this;
    }

    protected VectorData GenericMultiplyInplace(VectorData other)
    {
        var n = Length;
        Debug.Assert(Length == other.Length);
        for (int i = 0; i < n; i++) {
            Values[i] *= other.Values[i];
        }
        return this;
    }

    protected VectorData GenericNotInplace()
    {
        var n = Length;
        for (int i = 0; i < n; i++) {
            Values[i] = Values[i] == 0.0f ? 1.0f : 0.0f;
        }
        return this;
    }

    protected VectorData GenericSubtractInplace(VectorData other)
    {
        var n = Length;
        Debug.Assert(Length == other.Length);
        for (int i = 0; i < n; i++) {
            Values[i] -= other.Values[i];
        }
        return this;
    }
}

public class FloatData : VectorData
{
    public float this[int x, int y]
    {
        get => Values[y * Width + x];
        set => Values[y * Width + x] = value;
    }

    public FloatData(int width, int height, ArrayPool<float>? pool = null)
        : base(width, height, 1, pool ?? ArrayPool<float>.Shared)
    {
    }

    public void AddInplace(FloatData other) => GenericAddInplace(other);

    public FloatData AddInplace(FloatSwizzling b)
    {
        if (b.Component == VectorComponent.W && b.Data is Vec4Data) {
            var n = Length;
            var bv = b.Data.Values;
            for (int i = 0; i < n; i++) {
                Values[i] += bv[i*4 + 3];
            }
        } else {
            throw new NotSupportedException($"Cannot add {b.Component} from {b.Data.GetType()}");
        }
        return this;
    }

    public void MultiplyInplace(FloatData other) => GenericMultiplyInplace(other);
    public void NotInplace() => GenericNotInplace();

    public static FloatData operator >(FloatData a, float b)
    {
        var data = new FloatData(a.Width, a.Height, a.Pool);
        var v = data.Values;
        var av = a.Values;
        var n = data.Length;
        for (int i = 0; i < n; i++) {
            v[i] = av[i] > b ? 1 : 0;
        }
        return data;
    }

    public static FloatData operator <(FloatData a, float b)
    {
        var data = new FloatData(a.Width, a.Height, a.Pool);
        var v = data.Values;
        var av = a.Values;
        var n = data.Length;
        for (int i = 0; i < n; i++) {
            v[i] = av[i] < b ? 1 : 0;
        }
        return data;
    }

    public FloatData MaxInplace(float value)
    {
        var n = Length;
        for (int i = 0; i < n; i++) {
            Values[i] = MathF.Max(Values[i], value);
        }
        return this;
    }

    public static Vec3Data operator *(FloatData a, Vector3 b)
    {
        var data = new Vec3Data(a.Width, a.Height, a.Pool);
        var v = data.Values;
        var av = a.Values;
        var n = a.Length;
        for (int i = 0, j = 0; i < n; i++) {
            v[j++] = av[i] * b.X;
            v[j++] = av[i] * b.Y;
            v[j++] = av[i] * b.Z;
        }
        return data;
    }

    public static Vec3Data operator *(FloatData a, Vec3Data b)
    {
        var data = new Vec3Data(a.Width, a.Height, a.Pool);
        var v = data.Values;
        var av = a.Values;
        var bv = b.Values;
        var n = a.Length;
        for (int i = 0, j = 0; i < n; i++) {
            v[j] = av[i] * bv[j];
            v[j+1] = av[i] * bv[j+1];
            v[j+2] = av[i] * bv[j+2];
            j += 3;
        }
        return data;
    }

    public void SaveDepthTga(string path, float near, float far)
    {
        using var s = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read);
        using var w = new System.IO.BinaryWriter(s);
        w.Write((byte)0); // ID length
        w.Write((byte)0); // Color map type
        w.Write((byte)3); // Image type = Grayscale
        // Color map specification is 5 bytes
        w.Write((ushort)0); // Color map origin
        w.Write((ushort)0); // Color map length
        w.Write((byte)0); // Color map entry size
        // Image specification is 10 bytes
        w.Write((ushort)0); // X origin
        w.Write((ushort)0); // Y origin
        w.Write((ushort)Width); // Width
        w.Write((ushort)Height); // Height
        w.Write((byte)8); // Bits per pixel
        w.Write((byte)0b00100000); // Image descriptor
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                var v = this[x, y];
                if (v >= far) {
                    w.Write((byte)0);
                } else if (v <= near) {
                    w.Write((byte)255);
                } else {
                    w.Write((byte)(255.0f * (far - v) / (far - near)));
                }
            }
        }
    }
}

public class Vec2Data : VectorData
{
    public Memory<Vector2> Vector2Memory => MemoryUtils.Cast<float, Vector2>(FloatMemory);

    public Vec2Data(int width, int height, ArrayPool<float>? pool = null)
        : base(width, height, 2, pool ?? ArrayPool<float>.Shared)
    {
    }
    public Vec2Data(Vec2Data other)
        : base(other.Width, other.Height, other.Dimensions, other.Pool)
    {
        Buffer.BlockCopy(other.Values, 0, Values, 0, Length * sizeof(float));
    }

    public static Vec2Data operator +(Vec2Data a, Vec2Data b)
    {
        var data = new Vec2Data(a);
        data.GenericAddInplace(b);
        return data;
    }
    public static Vec2Data operator *(Vec2Data a, FloatData b)
    {
        var data = new Vec2Data(a);
        data.MultiplyInplace(b);
        return data;
    }
    public void MultiplyInplace(FloatData other)
    {
        var n = Length;
        Debug.Assert(Length == 2*other.Length);
        var bv = other.Values;
        for (int i = 0, j = 0; i < n; ) {
            var x = bv[j++];
            Values[i++] *= x;
            Values[i++] *= x;
        }
    }
    public static Vec2Data operator +(Vec2Data a, float b)
    {
        var data = new Vec2Data(a);
        data.AddInplace(b);
        return data;
    }
    public static Vec2Data operator -(Vec2Data a, float b)
    {
        var data = new Vec2Data(a);
        data.SubtractInplace(b);
        return data;
    }
    public static Vec2Data operator *(Vec2Data a, float b)
    {
        var data = new Vec2Data(a);
        data.MultiplyInplace(b);
        return data;
    }
    public static Vec2Data operator /(Vec2Data a, float b)
    {
        var data = new Vec2Data(a);
        data.DivideInplace(b);
        return data;
    }
    public static Vec2Data operator /(Vec2Data a, Vector2 b)
    {
        var data = new Vec2Data(a);
        data.DivideInplace(b);
        return data;
    }
    public void DivideInplace(Vector2 other)
    {
        var n = Length;
        for (int i = 0; i < n; ) {
            Values[i++] /= other.X;
            Values[i++] /= other.Y;
        }
    }
    public static Vec2Data operator /(Vec2Data a, FloatData b)
    {
        var data = new Vec2Data(a);
        data.DivideInplace(b);
        return data;
    }
    public void DivideInplace(FloatData other)
    {
        var n = Length;
        Debug.Assert(Length == 2*other.Length);
        var bv = other.Values;
        for (int i = 0, j = 0; i < n; ) {
            var x = bv[j++];
            Values[i++] /= x;
            Values[i++] /= x;
        }
    }
}

public class Vec3Data : VectorData
{
    public Memory<Vector3> Vector3Memory => MemoryUtils.Cast<float, Vector3>(FloatMemory);

    public Vector3 this[int x, int y] {
        get {
            var i = (y*Width + x)*3;
            return new Vector3(Values[i], Values[i+1], Values[i+2]);
        }
        set {
            var i = (y*Width + x)*3;
            Values[i] = value.X;
            Values[i+1] = value.Y;
            Values[i+2] = value.Z;
        }
    }

    public Vec3Data(int width, int height, ArrayPool<float>? pool = null)
        : base(width, height, 3, pool ?? ArrayPool<float>.Shared)
    {
    }

    public Vec3Data(Vec3Data other)
        : base(other.Width, other.Height, other.Dimensions, other.Pool)
    {
        Buffer.BlockCopy(other.Values, 0, Values, 0, Length * sizeof(float));
    }

    public Vec3Data Clone()
    {
        return new Vec3Data(this);
    }

    public static Vec3Data operator +(Vec3Data a, Vec3Data b)
    {
        var data = new Vec3Data(a);
        data.AddInplace(b);
        return data;
    }

    public static Vec3Data operator +(Vec3Data a, Vector3 b)
    {
        var data = new Vec3Data(a);
        data.AddInplace(b);
        return data;
    }

    public Vec3Data AddInplace(Vec3Data b) =>
        (Vec3Data)GenericAddInplace(b);

    public Vec3Data AddInplace(Vec3Swizzling b)
    {
        if (b.Data is Vec4Data b4 && b.XComponent == VectorComponent.X && b.YComponent == VectorComponent.Y && b.ZComponent == VectorComponent.Z) {
            var n = Length;
            var bv = b4.Values;
            for (int i = 0, j = 0; i < n; i+= 3, j += 4) {
                Values[i] += bv[j];
                Values[i+1] += bv[j+1];
                Values[i+2] += bv[j+2];
            }
            return this;
        }
        else {
            throw new NotSupportedException($"Cannot add {b.Data.GetType()} to {GetType()}");
        }
    }

    public Vec3Data AddInplace(Vector3 b)
    {
        var n = Length;
        for (int i = 0; i < n; i += 3) {
            Values[i] += b.X;
            Values[i+1] += b.Y;
            Values[i+2] += b.Z;
        }
        return this;
    }

    public FloatData DotInplace(Vec3Data b)
    {
        var data = new FloatData(Width, Height, Pool);
        var n = Length;
        var av = Values;
        var bv = b.Values;
        var v = data.Values;
        for (int i = 0, j = 0; i < n; i += 3, j++) {
            v[j] = av[i]*bv[i] + av[i+1]*bv[i+1] + av[i+2]*bv[i+2];
        }
        return data;
    }

    public static Vec3Data operator *(Vec3Data a, FloatData b)
    {
        var data = new Vec3Data(a);
        data.MultiplyInplace(b);
        return data;
    }

    public Vec3Data MultiplyInplace(FloatData other)
    {
        var n = Length;
        Debug.Assert(Length == 3*other.Length);
        var bv = other.Values;
        for (int i = 0, j = 0; i < n; ) {
            var x = bv[j++];
            Values[i++] *= x;
            Values[i++] *= x;
            Values[i++] *= x;
        }
        return this;
    }

    public Vec3Data NormalizeInplace()
    {
        var v = Values;
        var n = Length;
        for (int i = 0; i < n;) {
            var x = v[i];
            var y = v[i+1];
            var z = v[i+2];
            var len = MathF.Sqrt(x*x + y*y + z*z);
            if (len > 0) {
                var r = 1.0f / len;
                v[i++] = x*r;
                v[i++] = y*r;
                v[i++] = z*r;
            }
            else {
                i += 3;
            }
        }
        return this;
    }

    public static Vec3Data operator -(Vec3Data a, Vec3Data b)
    {
        var data = new Vec3Data(a);
        data.SubtractInplace(b);
        return data;
    }

    public static Vec3Data operator -(Vector3 a, Vec3Data b)
    {
        var data = new Vec3Data(b.Width, b.Height, b.Pool);
        var v = data.Values;
        var bv = b.Values;
        var n = data.Length;
        for (int i = 0; i < n; i += 3) {
            v[i] = a.X - bv[i];
            v[i+1] = a.Y - bv[i+1];
            v[i+2] = a.Z - bv[i+2];
        }
        return data;
    }

    public static Vec3Data operator -(Vec3Data a, Vector3 b)
    {
        var data = new Vec3Data(a);
        data.SubtractInplace(b);
        return data;
    }

    public Vec3Data SubtractInplace(Vec3Data other) =>
        (Vec3Data)GenericSubtractInplace(other);

    public Vec3Data SubtractInplace(Vector3 b)
    {
        var n = Length;
        for (int i = 0; i < n; i += 3) {
            Values[i] -= b.X;
            Values[i+0] -= b.Y;
            Values[i+1] -= b.Z;
        }
        return this;
    }

    public void SaveTga(string path)
    {
        using var s = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read);
        using var w = new System.IO.BinaryWriter(s);
        w.Write((byte)0); // ID length
        w.Write((byte)0); // Color map type
        w.Write((byte)2); // Image type = uncompressed RGB
        // Color map specification is 5 bytes
        w.Write((ushort)0); // Color map origin
        w.Write((ushort)0); // Color map length
        w.Write((byte)0); // Color map entry size
        // Image specification is 10 bytes
        w.Write((ushort)0); // X origin
        w.Write((ushort)0); // Y origin
        w.Write((ushort)Width); // Width
        w.Write((ushort)Height); // Height
        w.Write((byte)24); // Bits per pixel
        w.Write((byte)0b00100000); // Image descriptor
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                var v = this[x, y] * 255.0f;
                if (v.Z <= 0.0f) {
                    w.Write((byte)0);
                } else if (v.Z >= 255.0f) {
                    w.Write((byte)255);
                } else {
                    w.Write((byte)v.Z);
                }
                if (v.Y <= 0.0f) {
                    w.Write((byte)0);
                } else if (v.Y >= 255.0f) {
                    w.Write((byte)255);
                } else {
                    w.Write((byte)v.Y);
                }
                if (v.X <= 0.0f) {
                    w.Write((byte)0);
                } else if (v.X >= 255.0f) {
                    w.Write((byte)255);
                } else {
                    w.Write((byte)v.X);
                }
            }
        }
    }
}

public class Vec4Data : VectorData
{
    public Memory<Vector4> Vector4Memory => MemoryUtils.Cast<float, Vector4>(FloatMemory);

    public Vec3Swizzling Xyz => new Vec3Swizzling(this, VectorComponent.X, VectorComponent.Y, VectorComponent.Z);
    public FloatSwizzling W => new FloatSwizzling(this, VectorComponent.W);

    public Vector4 this[int x, int y] {
        get {
            var i = (y*Width + x)*4;
            return new Vector4(Values[i], Values[i+1], Values[i+2], Values[i+3]);
        }
    }

    public Vec4Data(int width, int height, ArrayPool<float>? pool = null)
        : base(width, height, 4, pool ?? ArrayPool<float>.Shared)
    {
    }

    public Vec4Data(Vec4Data other)
        : base(other.Width, other.Height, other.Dimensions, other.Pool)
    {
        Buffer.BlockCopy(other.Values, 0, Values, 0, Length * sizeof(float));
    }

    public Vec4Data Clone()
    {
        return new Vec4Data(this);
    }
}

public enum VectorComponent
{
    X,
    Y,
    Z,
    W,
}

public abstract class Swizzling {}

public class FloatSwizzling : Swizzling {
    public readonly VectorData Data;
    public readonly VectorComponent Component;

    public FloatSwizzling(VectorData data, VectorComponent component)
    {
        Data = data;
        Component = component;
    }
}

public class Vec3Swizzling : Swizzling {
    public readonly VectorData Data;
    public readonly VectorComponent XComponent;
    public readonly VectorComponent YComponent;
    public readonly VectorComponent ZComponent;

    public Vec3Swizzling(VectorData data, VectorComponent xComponent, VectorComponent yComponent, VectorComponent zComponent)
    {
        Data = data;
        XComponent = xComponent;
        YComponent = yComponent;
        ZComponent = zComponent;
    }
}

public static class VectorOps
{
    public static FloatData Dot(Vec3Data a, Vec3Data b) =>
        a.Clone().DotInplace(b);

    /// <summary>
    /// Modulus using the floor method vs .NET's trunc method.
    /// </summary>
    public static float Mod(float a, float b) =>
        a - b * MathF.Floor(a / b);

    public static Vec3Data MulAdd(Vec3Data a, Vec3Data b, Vec3Data c)
    {
        var data = c.Clone();
        var v = data.Values;
        var av = a.Values;
        var bv = b.Values;
        var n = data.Length;
        for (int i = 0; i < n; i += 3) {
            v[i] += av[i] * bv[i];
            v[i+1] += av[i+1] * bv[i+1];
            v[i+2] += av[i+2] * bv[i+2];
        }
        return data;
    }

    public static Vec3Data MulAdd(FloatData a, Vec3Data b, float c)
    {
        var data = new Vec3Data(b.Width, b.Height, b.Pool);
        var v = data.Values;
        var av = a.Values;
        var bv = b.Values;
        var n = data.Length;
        for (int i = 0, j = 0; i < n; i += 3, j++) {
            v[i] = av[j] * bv[i] + c;
            v[i+1] = av[j] * bv[i+1] + c;
            v[i+2] = av[j] * bv[i+2] + c;
        }
        return data;
    }

#if NET6_0_OR_GREATER
    static readonly bool hasFma = Fma.IsSupported;
    static readonly bool hasAvx = Avx.IsSupported;
#endif

    public static Vec3Data MulAdd(Vec3Data a, FloatData b, Vec3Data c)
    {
        var data = new Vec3Data(a.Width, a.Height, a.Pool);
        var v = data.Values;
        var av = a.Values;
        var bv = b.Values;
        var cv = c.Values;
        var n = data.Length;
        if (false) {
            // Just making the ifdef easier to read
        }
#if NET6_0_OR_GREATER
        else if (hasAvx) {
            var av256 = MemoryMarshal.Cast<float, Vector256<float>>(av);
            var cv256 = MemoryMarshal.Cast<float, Vector256<float>>(cv);
            var v256 = MemoryMarshal.Cast<float, Vector256<float>>(v);
            var floatsPerReg = Vector256<float>.Count;
            var num256s = n / floatsPerReg;
            var numRem = n % floatsPerReg;
            for (int i = 0, j = 0; i < num256s; i++) {
                var bx = bv[j / 3];
                var by = bv[(j + 1) / 3];
                var bz = bv[(j + 2) / 3];
                var bw = bv[(j + 3) / 3];
                var bx2 = bv[(j + 4) / 3];
                var by2 = bv[(j + 5) / 3];
                var bz2 = bv[(j + 6) / 3];
                var bw2 = bv[(j + 7) / 3];
                var b256 = Vector256.Create(bx, by, bz, bw, bx2, by2, bz2, bw2);
                var result = Avx.Add(Avx.Multiply(av256[i], b256), cv256[i]);
                v256[i] = result;
                j += floatsPerReg;
            }
        }
        else if (hasFma) {
            var av128 = MemoryMarshal.Cast<float, Vector128<float>>(av);
            var cv128 = MemoryMarshal.Cast<float, Vector128<float>>(cv);
            var v128 = MemoryMarshal.Cast<float, Vector128<float>>(v);
            var floatsPerReg = Vector128<float>.Count;
            var num128s = n / floatsPerReg;
            var numRem = n % floatsPerReg;
            for (int i = 0, j = 0; i < num128s; i++) {
                var bx = bv[j / 3];
                var by = bv[(j + 1) / 3];
                var bz = bv[(j + 2) / 3];
                var bw = bv[(j + 3) / 3];
                var b128 = Vector128.Create(bx, by, bz, bw);
                var result = Fma.MultiplyAdd(av128[i], b128, cv128[i]);
                v128[i] = result;
                j += floatsPerReg;
            }
        }
#endif
        else {
            for (int i = 0, j = 0; i < n; j++) {
                var bvj = bv[j];
                v[i] = av[i] * bvj + cv[i];
                i++;
                v[i] = av[i] * bvj + cv[i];
                i++;
                v[i] = av[i] * bvj + cv[i];
                i++;
            }
        }
        return data;
    }

    public static Vec3Data Normalize(Vec3Data xyz) =>
        xyz.Clone().NormalizeInplace();

    public static Vec3Data Vec3(FloatSwizzling x, FloatSwizzling y, FloatSwizzling z)
    {
        var data = new Vec3Data(x.Data.Width, x.Data.Height, x.Data.Pool);
        var v = data.Values;
        var xv = x.Data.Values;
        var yv = y.Data.Values;
        var zv = z.Data.Values;
        var xstride = x.Data.Dimensions;
        var ystride = y.Data.Dimensions;
        var zstride = z.Data.Dimensions;
        var n = data.Length;
        for (int i = 0, xi = (int)x.Component, yi = (int)y.Component, zi = (int)z.Component; i < n; ) {
            v[i++] = xv[xi];
            v[i++] = yv[yi];
            v[i++] = zv[zi];
            xi += xstride;
            yi += ystride;
            zi += zstride;
        }
        return data;
    }

    public static Vec3Data Vec3(Vec2Data xy, float z)
    {
        var data = new Vec3Data(xy.Width, xy.Height, xy.Pool);
        var v = data.Values;
        var bv = xy.Values;
        var n = data.Length;
        for (int i = 0, j = 0; i < n; ) {
            var x = bv[j++];
            var y = bv[j++];
            v[i++] = x;
            v[i++] = y;
            v[i++] = z;
        }
        return data;
    }

    public static Vec3Data Vec3(FloatData x, FloatData y, FloatData z)
    {
        var data = new Vec3Data(x.Width, x.Height, x.Pool);
        var v = data.Values;
        var bx = x.Values;
        var by = y.Values;
        var bz = z.Values;
        var n = data.Length;
        for (int i = 0, j = 0; i < n; ) {
            v[i++] = bx[j];
            v[i++] = by[j];
            v[i++] = bz[j];
            j++;
        }
        return data;
    }

    public static float VMax(Vector3 v) =>
        Math.Max(Math.Max(v.X, v.Y), v.Z);

}
