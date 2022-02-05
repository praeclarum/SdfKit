using System.Buffers;

using static SdfKit.VectorOps;

namespace SdfKit;

public class RayMarcher
{
    public const float DefaultNearPlaneDistance = 1.0f;
    public const float DefaultFarPlaneDistance = 100.0f;
    public const float DefaultVerticalFieldOfViewDegrees = 60.0f;
    public const int DefaultDepthIterations = 40;

    readonly Sdf sdf;
    readonly int batchSize;
    readonly int maxDegreeOfParallelism;

    readonly int width;
    readonly int height;
    readonly ArrayPool<float> pool = ArrayPool<float>.Create();

    public Matrix4x4 ViewTransform { get; set; } = 
        Matrix4x4.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.UnitY);

    public float NearPlaneDistance { get; set; } = DefaultNearPlaneDistance;
    public float FarPlaneDistance { get; set; } = DefaultFarPlaneDistance;
    public float VerticalFieldOfViewDegrees { get; set; } = DefaultVerticalFieldOfViewDegrees;

    const float GradOffset = 1e-5f;

    public int DepthIterations { get; set; } = DefaultDepthIterations;

    public RayMarcher(int width, int height, Sdf sdf, int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1)
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

    /// <summary>
    /// Returns a depth for every ray.
    /// </summary>
    FloatData RenderDepth(Vec3Data rayOrigin, Vec3Data rayDir)
    {
        var depth = Float(NearPlaneDistance - 0.1f);
        for (int i = 0; i < DepthIterations; i++) {
            using var samplePos = rayDir*depth;
            samplePos.AddInplace(rayOrigin);
            using var sampleDistance = Scene(samplePos);
            depth.AddInplace(sampleDistance.W);
        }
        return depth;
    }

    void GetCameraRays(out Vec3Data rayOrigin, out Vec3Data rayDirection)
    {
        Matrix4x4.Invert(ViewTransform, out var cameraTransform);
        var cameraPosition = Vector3.Transform(Vector3.Zero, cameraTransform);
        rayOrigin = NewVec3(cameraPosition.X, cameraPosition.Y, cameraPosition.Z);

        var projectionTransform = Matrix4x4.CreatePerspectiveFieldOfView(
            VerticalFieldOfViewDegrees * MathF.PI / 180.0f,
            (float)width / height,
            NearPlaneDistance,
            FarPlaneDistance);

        var viewProjectionTransform = ViewTransform * projectionTransform;
        Matrix4x4.Invert(viewProjectionTransform, out var viewProjectionInverse);
        Vector3 GetDirection(float x, float y, float z = 0) {
            var vv = new Vector4(x, y, z, 1);
            var vvv = Vector4.Transform(vv, viewProjectionInverse);
            var pos = new Vector3(vvv.X/vvv.W, vvv.Y/vvv.W, vvv.Z/vvv.W);
            var d = pos - cameraPosition;
            return Vector3.Normalize(d);
        }
        var topLeft = GetDirection(-1, 1);
        var topRight = GetDirection(1, 1);
        var bottomLeft = GetDirection(-1, -1);
        var bottomRight = GetDirection(1, -1);
        var center = GetDirection(0, 0);
        rayDirection = NewVec3();
        for (int j = 0; j < height; j++) {
            var y = 1.0f - 2.0f * (float)j / (height - 1);
            for (int i = 0; i < width; i++) {
                var x = -1.0f + 2.0f * (float)i / (width - 1);
                var dir = GetDirection(x, y);
                rayDirection[i, j] = dir;
            }
        }
    }

    /// <summary>
    /// Returns a color for every pixel in fragCoord.
    /// </summary>
    Vec3Data Render(Vec3Data rayOrigin, Vec3Data rayDir)
    {
        // Find surface intersection for every ray.
        using var depth = Float(NearPlaneDistance - 0.1f);
        using var diffuseColor = NewVec3(0.0f, 0.0f, 0.0f);
        for (int i = 0; i < DepthIterations; i++) {
            using var samplePos = rayDir*depth;
            samplePos.AddInplace(rayOrigin);
            using var sampleDist = Scene(samplePos);
            depth.AddInplace(sampleDist.W);
            if (i == DepthIterations - 1) {
                diffuseColor.AddInplace(sampleDist.Xyz);
            }
        }
        var surfacePos = rayOrigin + rayDir*depth;
        // Calculate the lighting
        using var surfaceNormal = DistanceGradient(surfacePos).NormalizeInplace();
        var lightPosition = new Vector3(5f, 5f, 10.0f);
        using var lightDirection = (lightPosition - surfacePos).NormalizeInplace();
        using var diffuseValue =
            Dot(surfaceNormal, lightDirection)
            .MaxInplace(0.0f);
        using var lighting = diffuseValue * diffuseColor;
        lighting.AddInplace(0.1f);
        // Calculate the color
        using var bgMask = depth > FarPlaneDistance;
        using var bg = bgMask * new Vector3(0.5f, 0.75f, 1.0f);
        bgMask.NotInplace();
        using var fg = bgMask * lighting;
        var fragColor = fg + bg;
        return fragColor;
    }

    static readonly Vector3[] GradOffsets = {
        GradOffset * Vector3.UnitX,
        GradOffset * Vector3.UnitY,
        GradOffset * Vector3.UnitZ,
        -GradOffset * Vector3.UnitX,
        -GradOffset * Vector3.UnitY,
        -GradOffset * Vector3.UnitZ,
    };

    Vec3Data DistanceGradient(Vec3Data p)
    {
        Vec4Data? px = null, py = null, pz = null;
        Vec4Data? nx = null, ny = null, nz = null;
        
        Parallel.ForEach(GradOffsets, (o, s, i) => {
            // Console.WriteLine($"{i} = {o}");
            using var op = p + o;
            var d = Scene(op);
            switch (i) {
                case 0: px = d; break;
                case 1: py = d; break;
                case 2: pz = d; break;
                case 3: nx = d; break;
                case 4: ny = d; break;
                case 5: nz = d; break;
            }
        });
        if (px is null || py is null || pz is null) {
            throw new System.Exception("px, py, pz are null");
        }
        if (nx is null || ny is null || nz is null) {
            throw new System.Exception("nx, ny, nz are null");
        }
        using (px) using (py) using (pz)
        using (nx) using (ny) using (nz) {
            using var pp = Vec3(px.W, py.W, pz.W);
            using var np = Vec3(nx.W, ny.W, nz.W);
            return pp - np;
        }
    }

    Vec4Data Scene(Vec3Data p)
    {
        var distances = NewVec4();
        sdf.Sample(p.Vector3Memory, distances.Vector4Memory, batchSize, maxDegreeOfParallelism);
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
    Vec4Data NewVec4() => new Vec4Data(width, height, pool);
    
    Vec3Data NewVec3(float x, float y, float z)
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

}
