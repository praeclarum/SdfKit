namespace SdfKit;

/// <summary>
/// A regular 3D grid of distance values.
/// </summary>
public class Volume : IVolume
{
    public const int DefaultBatchSize = 2*1024;

    public readonly float[,,] Values;
    public int NX => Values.GetLength(0);
    public int NY => Values.GetLength(1);
    public int NZ => Values.GetLength(2);

    public Vector3 Min { get; private set; }
    public Vector3 Max { get; private set; }
    public Vector3 Center => (Min + Max) * 0.5f;
    public Vector3 Size => Max - Min;
    public float Radius => (Max - Min).Length() * 0.5f;

    public Volume(float[,,] values, Vector3 min, Vector3 max)
    {
        Values = values;
        Min = min;
        Max = max;
    }

    public Volume(Vector3 min, Vector3 max, int nx, int ny, int nz)
        : this(CreateSamplingVolume(min, max, ref nx, ref ny, ref nz, out var newMin, out var dx, out var dy, out var dz), min, max)
    {
    }

    public float this[int x, int y, int z]
    {
        get => Values[x, y, z];
        set => Values[x, y, z] = value;
    }

    public Mesh CreateMesh(float isoValue = 0.0f, int step = 1, IProgress<float>? progress = null)
    {
        return MarchingCubes.CreateMesh(this, isoValue, step, progress);
    }

    public void SampleSdf(Action<Memory<Vector3>, Memory<float>> sdf, int batchSize = DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        var volume = Values;
        var nx = NX;
        var ny = NY;
        var nz = NZ;
        var min = Min;
        var max = Max;
        MeasureSamplingVolume(min, max, ref nx, ref ny, ref nz, out var newMin, out var dx, out var dy, out var dz);
        min = newMin;
        var ntotal = nx * ny * nz;
        var numBatches = (ntotal + batchSize - 1) / batchSize;
        
        var options = new System.Threading.Tasks.ParallelOptions { 
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
        };
        System.Threading.Tasks.Parallel.For<(Vector3[],float[])>(0, numBatches, options, () => {
            var positions = new Vector3[batchSize];
            var values = new float[batchSize];
            
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
                volume[ix, iy, iz] = values[i - startI];
            }
            return pvs;
        }, x => {
            // No cleanup needed
        });
    }

    public static Volume SampleSdf(Sdf sdf, int nx, int ny, int nz, int batchSize = DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        return SampleSdf(sdf.SampleBatch, sdf.Min, sdf.Max, nx, ny, nz, batchSize, maxDegreeOfParallelism);
    }

    public static Volume SampleSdf(Func<Vector3, float> sdf, Vector3 min, Vector3 max, int nx, int ny, int nz, int batchSize = DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        void BatchedSdf(Memory<Vector3> positions, Memory<float> values)
        {
            var count = positions.Length;
            var p = positions.Span;
            var v = values.Span;
            for (int i = 0; i < count; i++)
            {
                v[i] = sdf(p[i]);
            }
        }
        return SampleSdf(BatchedSdf, min, max, nx, ny, nz, batchSize, maxDegreeOfParallelism);
    }

    public static Volume SampleSdf(Action<Memory<Vector3>, Memory<float>> sdf, Vector3 min, Vector3 max, int nx, int ny, int nz, int batchSize = DefaultBatchSize, int maxDegreeOfParallelism = -1)
    {
        var volume = new Volume(min, max, nx, ny, nz);
        volume.SampleSdf(sdf, batchSize, maxDegreeOfParallelism);
        return volume;
    }

    static float[,,] CreateSamplingVolume(Vector3 min, Vector3 max, ref int nx, ref int ny, ref int nz, out Vector3 newMin, out float dx, out float dy, out float dz)
    {
        MeasureSamplingVolume(min, max, ref nx, ref ny, ref nz, out newMin, out dx, out dy, out dz);
        return new float[nx, ny, nz];
    }

    static void MeasureSamplingVolume(Vector3 min, Vector3 max, ref int nx, ref int ny, ref int nz, out Vector3 newMin, out float dx, out float dy, out float dz)
    {
        newMin = min;
        dx = nx > 1 ? (max.X - min.X) / (nx - 1) : 0.0f;
        dy = ny > 1 ? (max.Y - min.Y) / (ny - 1) : 0.0f;
        dz = nz > 1 ? (max.Z - min.Z) / (nz - 1) : 0.0f;
        if (nx <= 1)
        {
            newMin.X = (min.X + max.X) / 2.0f;
            nx = 1;
        }
        if (ny <= 1)
        {
            newMin.Y = (min.Y + max.Y) / 2.0f;
            ny = 1;
        }
        if (nz <= 1)
        {
            newMin.Z = (min.Z + max.Z) / 2.0f;
            nz = 1;
        }
    }
}
