namespace MarchingCubes;

public static class Volume
{
    public static float[,,] SampleSdf(Func<Vector3, float> sdf, Vector3 min, Vector3 max, int nx, int ny, int nz)
    {
        float[,,] volume = new float[nx, ny, nz];
        float dx = (max.X - min.X) / nx;
        float dy = (max.Y - min.Y) / ny;
        float dz = (max.Z - min.Z) / nz;
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

    public static float[,,] SampleSphere(float r, Vector3 min, Vector3 max, int nx, int ny, int nz)
    {
        var sdf = (Vector3 p) => p.Length() - r;
        return Volume.SampleSdf(sdf, min, max, nx, ny, nz);
    }

    public static float[,,] SampleSphere(float r, float padding, int nx, int ny, int nz)
    {
        var sdf = (Vector3 p) => p.Length() - r;
        var min = new Vector3(-r - padding, -r - padding, -r - padding);
        var max = new Vector3(r + padding, r + padding, r + padding);
        return Volume.SampleSdf(sdf, min, max, nx, ny, nz);
    }
}



