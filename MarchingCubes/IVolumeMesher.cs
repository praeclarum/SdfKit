namespace MarchingCubes;

public interface IVolumeMesher
{
    Mesh CreateMesh(float[,,] volume, float isolevel, int st);
}
