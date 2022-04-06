namespace SdfKit;

/// <summary>
/// A regular 3D grid of distance values.
/// </summary>
public class Voxels : IBoundedVolume
{
    public readonly float[,,] Values;
    public readonly int NX;
    public readonly int NY;
    public readonly int NZ;
    public readonly float DX;
    public readonly float DY;
    public readonly float DZ;

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
        NX = values.GetLength(0);
        NY = values.GetLength(1);
        NZ = values.GetLength(2);
        DX = NX >= 1 ? (max.X - min.X) / NX : 0.0f;
        DY = NY >= 1 ? (max.Y - min.Y) / NY : 0.0f;
        DZ = NZ >= 1 ? (max.Z - min.Z) / NZ : 0.0f;
    }

    public Voxels(Vector3 min, Vector3 max, int nx, int ny, int nz)
        : this(new float[nx, ny, nz], min, max)
    {
    }

    public float this[int ix, int iy, int iz]
    {
        get => Values[ix, iy, iz];
        set => Values[ix, iy, iz] = value;
    }

    public float this[Vector3 p]
    {
        get
        {
            var ix = (int)((p.X - Min.X) / DX);
            var iy = (int)((p.Y - Min.Y) / DY);
            var iz = (int)((p.Z - Min.Z) / DZ);
            return Values[ix, iy, iz];
        }

        set
        {
            var ix = (int)((p.X - Min.X) / DX);
            var iy = (int)((p.Y - Min.Y) / DY);
            var iz = (int)((p.Z - Min.Z) / DZ);
            Values[ix, iy, iz] = value;
        }
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
        min += new Vector3(0.5f*DX, 0.5f*DY, 0.5f*DZ);
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
                p.X = min.X + ix * DX;
                p.Y = min.Y + iy * DY;
                p.Z = min.Z + iz * DZ;
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
}
