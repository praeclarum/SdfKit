namespace SdfKit;

public interface IVolumeMesher
{
    Mesh CreateMesh(float[,,] volume, float isovalue, int st);
}
