using System.Buffers;

namespace SdfKit;

public class FrameData : IDisposable
{
    public readonly float[] Values;
    public readonly int Width;
    public readonly int Height;
    public readonly int Dimensions;
    public readonly int Length;
    public readonly ArrayPool<float> Pool;
    bool returned;

    public Memory<float> FloatMemory => Values.AsMemory(0, Length);

    public FrameData(int width, int height, int dim, ArrayPool<float> pool)
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
    public void AddInplace(float other)
    {
        var n = Length;
        for (int i = 0; i < n; i++) {
            Values[i] += other;
        }
    }
    public void SubtractInplace(float other)
    {
        var n = Length;
        for (int i = 0; i < n; i++) {
            Values[i] -= other;
        }
    }
    public void MultiplyInplace(float other)
    {
        var n = Length;
        for (int i = 0; i < n; i++) {
            Values[i] *= other;
        }
    }
    public void DivideInplace(float other)
    {
        var n = Length;
        for (int i = 0; i < n; i++) {
            Values[i] /= other;
        }
    }

    protected void GenericAddInplace(FrameData other)
    {
        var n = Length;
        Debug.Assert(Length == other.Length);
        for (int i = 0; i < n; i++) {
            Values[i] += other.Values[i];
        }
    }

    protected void GenericMultiplyInplace(FrameData other)
    {
        var n = Length;
        Debug.Assert(Length == other.Length);
        for (int i = 0; i < n; i++) {
            Values[i] *= other.Values[i];
        }
    }
}

public class FloatData : FrameData
{
    public float this[int x, int y] => Values[y*Width + x];

    public FloatData(int width, int height, ArrayPool<float> pool) 
        : base(width, height, 1, pool)
    {
    }

    public void AddInplace(FloatData other) => GenericAddInplace(other);
    public void MultiplyInplace(FloatData other) => GenericMultiplyInplace(other);

    public void SaveTga(string path, float near, float far)
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

public class Vec2Data : FrameData
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

public class Vec3Data : FrameData
{
    public Memory<Vector3> Vector3Memory => MemoryUtils.Cast<float, Vector3>(FloatMemory);

    public Vec3Data(int width, int height, ArrayPool<float> pool) 
        : base(width, height, 3, pool)
    {
    }
    public Vec3Data(Vec3Data other)
        : base(other.Width, other.Height, other.Dimensions, other.Pool)
    {
        Buffer.BlockCopy(other.Values, 0, Values, 0, Length * sizeof(float));
    }

    public static Vec3Data operator +(Vec3Data a, Vec3Data b)
    {
        var data = new Vec3Data(a);
        data.GenericAddInplace(b);
        return data;
    }
    public void AddInplace(Vec3Data other) => GenericAddInplace(other);
    public static Vec3Data operator *(Vec3Data a, FloatData b)
    {
        var data = new Vec3Data(a);
        data.MultiplyInplace(b);
        return data;
    }
    public void MultiplyInplace(FloatData other)
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
    }
}
