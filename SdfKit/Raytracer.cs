using System.Buffers;

namespace SdfKit;


public class Raytracer
{
    readonly Sdf sdf;

    readonly int width;
    readonly int height;
    readonly ArrayPool<float> pool = ArrayPool<float>.Create();

    public Raytracer(int width, int height, Sdf sdf)
    {
        this.width = width;
        this.height = height;
        this.sdf = sdf;
    }

    public Vec3Data Render()
    {
        using var fragCoord = NewVec2();
        var fragCoordV = fragCoord.Values;
        var i = 0;
        for (var y = 0; y < height; ++y)
        {
            for (var x = 0; x < width; ++x)
            {
                fragCoordV[i++] = x;
                fragCoordV[i++] = y;
            }
        }
        return Render(fragCoord);
    }

    /// <summary>
    /// Renders the SDF to a bitmap. Returns a color for every pixel in fragCoord.
    /// </summary>
    Vec3Data Render(Vec2Data fragCoord)
    {
        var uv = fragCoord;
        using var ro = Vec3(0, 0, 5);
        using var nearPlane = Vec3(uv, -3);
        using var rd = Normalize(nearPlane);
        using var t = Float(2.5f);
        for (int i = 0; i < 3; i++) {
            using var d = Map(ro + rd*t);
            t.AddInplace(d);
        }
        using var rp = ro + rd*t;
        var fragColor = Vec3(1, 0, 0);
        return fragColor;
    }

    FloatData Map(Vec3Data p)
    {
        var distances = NewFloat();
        throw new NotImplementedException ();
    }

    FloatData NewFloat() => new FloatData(width, height, pool);
    FloatData Float(float x)
    {
        var data = NewFloat();
        Array.Fill(data.Values, x);
        return data;
    }
    Vec2Data NewVec2() => new Vec2Data(width, height, pool);
    Vec3Data NewVec3() => new Vec3Data(width, height, pool);
    Vec3Data Vec3(float x, float y, float z)
    {
        var data = NewVec3();
        var v = data.Values;
        var n = data.Length;
        for (int i = 0; i < n; ) {
            v[i++] = x;
            v[i++] = y;
            v[i++] = z;
        }
        return data;
    }
    Vec3Data Vec3(Vec2Data xy, float z)
    {
        var data = NewVec3();
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
    Vec3Data Normalize(Vec3Data xyz)
    {
        var data = NewVec3();
        var v = data.Values;
        var bv = xyz.Values;
        var n = data.Length;
        for (int i = 0; i < n; i += 3) {
            var x = bv[i];
            var y = bv[i+1];
            var z = bv[i+2];
            var r = 1.0f / MathF.Sqrt(x*x + y*y + z*z);
            v[i] = x*r;
            v[i+1] = y*r;
            v[i+2] = z*r;
        }
        return data;
    }

    public class FrameData : IDisposable
    {
        public readonly float[] Values;
        public readonly int Width;
        public readonly int Height;
        public readonly int Dimensions;
        public readonly int Length;
        public readonly ArrayPool<float> Pool;
        bool returned;

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
        public FloatData(int width, int height, ArrayPool<float> pool) 
            : base(width, height, 1, pool)
        {
        }

        public void AddInplace(FloatData other) => GenericAddInplace(other);
        public void MultiplyInplace(FloatData other) => GenericMultiplyInplace(other);
    }
    public class Vec2Data : FrameData
    {
        public Vec2Data(int width, int height, ArrayPool<float> pool) 
            : base(width, height, 2, pool)
        {
        }
    }
    public class Vec3Data : FrameData
    {
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
}
