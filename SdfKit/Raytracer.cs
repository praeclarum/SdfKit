using System.Buffers;

namespace SdfKit;

public class Raytracer
{
    readonly Sdf sdf;
    readonly int batchSize;
    readonly int maxDegreeOfParallelism;

    readonly int width;
    readonly int height;
    readonly ArrayPool<float> pool = ArrayPool<float>.Create();

    public float ZNear { get; set; } = 3.0f;
    public float ZFar { get; set; } = 1e3f;

    public Raytracer(int width, int height, Sdf sdf, int batchSize = Sdf.DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        this.width = width;
        this.height = height;
        this.sdf = sdf;
        this.batchSize = batchSize;
        this.maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    /// <summary>
    /// Renders the SDF to an RGB buffer.
    /// </summary>
    public Vec3Data Render()
    {
        GetCameraRays(out var ro, out var rd);
        using (ro)
        using (rd) {
            return Render(ro, rd);
        }
    }

    /// <summary>
    /// Renders the SDF to a depth buffer.
    /// </summary>
    public FloatData RenderDepth()
    {
        GetCameraRays(out var ro, out var rd);
        using (ro)
        using (rd) {
            return RenderDepth(ro, rd);
        }
    }

    void GetCameraRays(out Vec3Data ro, out Vec3Data rd)
    {
        using var uv = NewVec2();
        var uvv = uv.Values;
        var aspect = (float)width / height;
        var vheight = 2.0f;
        var vwidth = aspect * vheight;
        var dx = vwidth / (width - 1);
        var dy = -vheight / (height - 1);
        var startx = -vwidth * 0.5f;
        var starty = vheight * 0.5f;
        var i = 0;
        for (var yi = 0; yi < height; ++yi)
        {
            var y = starty + yi * dy;
            for (var xi = 0; xi < width; ++xi)
            {
                uvv[i++] = startx + xi * dx;
                uvv[i++] = y;
            }
        }
        ro = Vec3(0, 0, 5);
        using var nearPlane = Vec3(uv, -ZNear);
        rd = Normalize(nearPlane);
    }

    /// <summary>
    /// Returns a color for every pixel in fragCoord.
    /// </summary>
    Vec3Data Render(Vec3Data ro, Vec3Data rd)
    {
        using var t = Float(ZNear - 0.1f);
        for (int i = 0; i < 10; i++) {
            using var dp = rd*t;
            dp.AddInplace(ro);
            using var d = Map(dp);
            t.AddInplace(d);
        }
        // using var bg = t > 9.0f;
        // bg.MultiplyInplace(new Vector3(0.5f, 0.75f, 1.0f));
        var rp = ro + rd*t;
        var fragColor = rp;
        return fragColor;
    }

    /// <summary>
    /// Returns a depth for every ray.
    /// </summary>
    FloatData RenderDepth(Vec3Data ro, Vec3Data rd)
    {
        var depth = Float(ZNear - 0.1f);
        for (int i = 0; i < 10; i++) {
            using var dp = rd*depth;
            dp.AddInplace(ro);
            using var d = Map(dp);
            depth.AddInplace(d);
        }
        return depth;
    }

    FloatData Map(Vec3Data p)
    {
        var distances = NewFloat();
        sdf.Sample(p.Vector3Memory, distances.FloatMemory, batchSize, maxDegreeOfParallelism);
        return distances;
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
}
