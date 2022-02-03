namespace SdfKit;

public static class Volume
{
    public static float[,,] SampleSdf(Func<Vector3, float> sdf, Vector3 min, Vector3 max, int nx, int ny, int nz)
    {
        var volume = CreateSamplingVolume(ref min, max, ref nx, ref ny, ref nz, out var dx, out var dy, out var dz);
        Vector3 p = min;
        for (int iz = 0; iz < nz; iz++)
        {
            p.Z = min.Z + iz * dz;
            for (int iy = 0; iy < ny; iy++)
            {
                p.Y = min.Y + iy * dy;
                for (int ix = 0; ix < nx; ix++)
                {
                    p.X = min.X + ix * dx;
                    volume[ix, iy, iz] = sdf(p);
                }
            }
        }
        return volume;
    }

    public static float[,,] SampleSdfBatches(Action<Vector3[], float[], int> sdf, Vector3 min, Vector3 max, int nx, int ny, int nz, int batchSize = 2*1024, int maxDegreeOfParallelism = -1)
    {
        var volume = CreateSamplingVolume(ref min, max, ref nx, ref ny, ref nz, out var dx, out var dy, out var dz);
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
            sdf(positions, values, endI - startI);
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
        return volume;
    }

    public static float[,,] SampleSdfZPlanes(Action<Vector3[], float[]> sdf, Vector3 min, Vector3 max, int nx, int ny, int nz, int maxDegreeOfParallelism = -1)
    {
        var volume = CreateSamplingVolume(ref min, max, ref nx, ref ny, ref nz, out var dx, out var dy, out var dz);
        Vector3 p = min;
        var nplane = nx * ny;
        
        var options = new System.Threading.Tasks.ParallelOptions { 
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
        };
        System.Threading.Tasks.Parallel.For<(Vector3[],float[])>(0, nz, options, () => {
            var positions = new Vector3[nplane];
            var values = new float[nplane];
            for (int iy = 0; iy < ny; iy++)
            {
                var y = min.Y + iy * dy;
                for (int ix = 0; ix < nx; ix++)
                {
                    var i = ix+iy*nx;
                    positions[i].X = min.X + ix * dx;
                    positions[i].Y = y;
                    positions[i].Z = min.Z;
                }
            }
            return (positions, values);
        }, (iz, _, pvs) =>
        {
            var (positions, values) = pvs;
            var z = min.Z + iz * dz;
            for (int i = 0; i < nplane; i++)
            {
                positions[i].Z = z;
            }
            sdf(positions, values);
            for (int iy = 0; iy < ny; iy++)
            {
                for (int ix = 0; ix < nx; ix++)
                {
                    var i = ix+iy*nx;
                    volume[ix, iy, iz] = values[i];
                }
            }
            return pvs;
        }, x => {
            // No cleanup needed
        });
        return volume;
    }

    static float[,,] CreateSamplingVolume(ref Vector3 min, Vector3 max, ref int nx, ref int ny, ref int nz, out float dx, out float dy, out float dz)
    {
        dx = nx > 1 ? (max.X - min.X) / (nx - 1) : 0.0f;
        dy = ny > 1 ? (max.Y - min.Y) / (ny - 1) : 0.0f;
        dz = nz > 1 ? (max.Z - min.Z) / (nz - 1) : 0.0f;
        if (nx <= 1)
        {
            min.X = (min.X + max.X) / 2.0f;
            nx = 1;
        }
        if (ny <= 1)
        {
            min.Y = (min.Y + max.Y) / 2.0f;
            ny = 1;
        }
        if (nz <= 1)
        {
            min.Z = (min.Z + max.Z) / 2.0f;
            nz = 1;
        }
        return new float[nx, ny, nz];
    }

    public static float[,,] SampleSphere(float r, Vector3 min, Vector3 max, int nx, int ny, int nz)
    {
        var sdf = (Vector3[] ps, float[] ds, int n) => {
            for (var i = 0; i < n; ++i)
            {
                ds[i] = ps[i].Length() - r;
            }
        };
        return SampleSdfBatches(
            sdf,
            min,
            max,
            nx, ny, nz);
    }

    public static float[,,] SampleSphere(float r, float padding, int nx, int ny, int nz)
    {
        var min = new Vector3(-r - padding, -r - padding, -r - padding);
        var max = new Vector3(r + padding, r + padding, r + padding);
        return SampleSphere(r, min, max, nx, ny, nz);
    }
}
