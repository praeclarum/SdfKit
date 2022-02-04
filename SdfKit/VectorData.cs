using System.Buffers;

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

    public VectorData(int width, int height, int dim, ArrayPool<float> pool)
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
    public float this[int x, int y] => Values[y*Width + x];

    public FloatData(int width, int height, ArrayPool<float> pool) 
        : base(width, height, 1, pool)
    {
    }

    public void AddInplace(FloatData other) => GenericAddInplace(other);
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
        w.Write((byte)0); // Image descriptor
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

    public Vec2Data(int width, int height, ArrayPool<float> pool) 
        : base(width, height, 2, pool)
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
    }

    public Vec3Data(int width, int height, ArrayPool<float> pool) 
        : base(width, height, 3, pool)
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

    public Vec3Data AddInplace(Vec3Data other) =>
        (Vec3Data)GenericAddInplace(other);

    public Vec3Data AddInplace(Vector3 b)
    {
        var n = Length;
        for (int i = 0; i < n; i += 3) {
            Values[i] += b.X;
            Values[i+0] += b.Y;
            Values[i+1] += b.Z;
        }
        return this;
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
                v[i++] = 0;
                v[i++] = 0;
                v[i++] = 0;
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

    public void SaveRgbTga(string path)
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
        w.Write((byte)0); // Image descriptor
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

public static class VectorOps
{
    public static Vec3Data Normalize(Vec3Data xyz)
    {
        var data = xyz.Clone();
        data.NormalizeInplace();
        return data;
    }
}
