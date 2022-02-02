namespace MarchingCubes;

public class Mesh
{
    public Vector3[] Vertices { get; }
    public Vector3[] Normals { get; }
    public int[] Faces { get; }

    public Mesh(Vector3[] vertices, Vector3[] normals, int[] faces)
    {
        Vertices = vertices;
        Normals = normals;
        Faces = faces;
    }
}
