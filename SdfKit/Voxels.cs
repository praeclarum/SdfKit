namespace SdfKit;

/// <summary>
/// A regular 3D grid of distance values.
/// </summary>
public class Voxels : IBoundedVolume
{
    public readonly float[,,] Values;
    public int NX => Values.GetLength(0);
    public int NY => Values.GetLength(1);
    public int NZ => Values.GetLength(2);

    public Vector3 Min { get; private set; }
    public Vector3 Max { get; private set; }
    public Vector3 Center => (Min + Max) * 0.5f;
    public Vector3 Size => Max - Min;
    public float Radius => (Max - Min).Length() * 0.5f;

    public Voxels(float[,,] values, Vector3 min, Vector3 max)
    {
        Values = values;
        Min = min;
        Max = max;
    }

    public Voxels(Vector3 min, Vector3 max, int nx, int ny, int nz)
        : this(CreateSamplingVolume(min, max, ref nx, ref ny, ref nz, out var newMin, out var dx, out var dy, out var dz), min, max)
    {
    }

    public float this[int x, int y, int z]
    {
        get => Values[x, y, z];
        set => Values[x, y, z] = value;
    }

    public Mesh ToMesh(float isoValue = 0.0f, int step = 1, IProgress<float>? progress = null)
    {
        return MarchingCubes.CreateMesh(this, isoValue, step, progress);
    }

    public void SampleSdf(Sdf sdf, int batchSize =SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        var voxels = Values;
        var nx = NX;
        var ny = NY;
        var nz = NZ;
        var min = Min;
        var max = Max;
        MeasureSamplingVolume(min, max, ref nx, ref ny, ref nz, out var newMin, out var dx, out var dy, out var dz);
        min = newMin;
        min += new Vector3(0.5f*dx, 0.5f*dy, 0.5f*dz);
        var ntotal = nx * ny * nz;
        var numBatches = (ntotal + batchSize - 1) / batchSize;
        
        var options = new System.Threading.Tasks.ParallelOptions { 
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
        };
        System.Threading.Tasks.Parallel.For<(Vector3[],Vector4[])>(0, numBatches, options, () => {
            var positions = new Vector3[batchSize];
            var values = new Vector4[batchSize];
            
            return (positions, values);
        }, (ib, _, pvs) =>
        {
            var (positions, values) = pvs;
            var startI = ib * batchSize;
            var endI = Math.Min(ntotal, startI + batchSize);
            Vector3 p = min;
            for (int i = startI; i < endI; i++)
            {
                var ix = i % nx;
                var iy = (i / nx) % ny;
                var iz = i / (nx * ny);
                p.X = min.X + ix * dx;
                p.Y = min.Y + iy * dy;
                p.Z = min.Z + iz * dz;
                positions[i - startI] = p;
            }
            var pmem = positions.AsMemory().Slice(0, endI - startI);
            var vmem = values.AsMemory().Slice(0, endI - startI);
            sdf(pmem, vmem);
            for (int i = startI; i < endI; i++)
            {
                var ix = i % nx;
                var iy = (i / nx) % ny;
                var iz = i / (nx * ny);
                voxels[ix, iy, iz] = values[i - startI].W;
            }
            return pvs;
        }, x => {
            // No cleanup needed
        });
    }

    /// <summary>
    /// Clip the solid to the walls of this volume.
    /// This ensures that meshing the object will produce a solid object.
    /// Clipping is accomplished by overwriting the volume's outer wall
    /// to be "outside" values equal to a cell's size.
    /// </summary>
    public void ClipToBounds()
    {
        var voxels = Values;
        var nx = NX;
        var ny = NY;
        var nz = NZ;
        float outsideValue = Size.X / NX;
        // X = 0 and X = nx - 1 sides
        for (int iy = 0; iy < ny; iy++)
        {
            for (int iz = 0; iz < nz; iz++)
            {
                voxels[0, iy, iz] = outsideValue;
                voxels[nx - 1, iy, iz] = outsideValue;
            }
        }
        // Y = 0 and Y = ny - 1 sides
        for (int ix = 0; ix < nx; ix++)
        {
            for (int iz = 0; iz < nz; iz++)
            {
                voxels[ix, 0, iz] = outsideValue;
                voxels[ix, ny - 1, iz] = outsideValue;
            }
        }
        // Z = 0 and Z = nz - 1 sides
        for (int ix = 0; ix < nx; ix++)
        {
            for (int iy = 0; iy < ny; iy++)
            {
                voxels[ix, iy, 0] = outsideValue;
                voxels[ix, iy, nz - 1] = outsideValue;
            }
        }
    }

    public static Voxels SampleSdf(Sdf sdf, Vector3 min, Vector3 max, int nx, int ny, int nz, int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        var voxels = new Voxels(min, max, nx, ny, nz);
        voxels.SampleSdf(sdf, batchSize, maxDegreeOfParallelism);
        return voxels;
    }

    public static Voxels SampleSdf(Func<Vector3, Vector4> sdf, Vector3 min, Vector3 max, int nx, int ny, int nz, int batchSize = SdfConfig.DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        Sdf batchedSdf = (Memory<Vector3> positions, Memory<Vector4> values) =>
        {
            var count = positions.Length;
            var p = positions.Span;
            var v = values.Span;
            for (int i = 0; i < count; i++)
            {
                v[i] = sdf(p[i]);
            }
        };
        return SampleSdf(batchedSdf, min, max, nx, ny, nz, batchSize, maxDegreeOfParallelism);
    }

    static float[,,] CreateSamplingVolume(Vector3 min, Vector3 max, ref int nx, ref int ny, ref int nz, out Vector3 newMin, out float dx, out float dy, out float dz)
    {
        MeasureSamplingVolume(min, max, ref nx, ref ny, ref nz, out newMin, out dx, out dy, out dz);
        return new float[nx, ny, nz];
    }

    static void MeasureSamplingVolume(Vector3 min, Vector3 max, ref int nx, ref int ny, ref int nz, out Vector3 newMin, out float dx, out float dy, out float dz)
    {
        newMin = min;
        dx = nx >= 1 ? (max.X - min.X) / (nx) : 0.0f;
        dy = ny >= 1 ? (max.Y - min.Y) / (ny) : 0.0f;
        dz = nz >= 1 ? (max.Z - min.Z) / (nz) : 0.0f;
    }
}
