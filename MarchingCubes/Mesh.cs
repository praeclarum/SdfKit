namespace MarchingCubes;

using System.IO;

public class Mesh
{
    public Vector3[] Vertices { get; }
    public Vector3[] Normals { get; }
    public int[] Triangles { get; }

    public Mesh(Vector3[] vertices, Vector3[] normals, int[] triangles)
    {
        Vertices = vertices;
        Normals = normals;
        Triangles = triangles;
    }

    void WriteObjVector(TextWriter w, string head, Vector3 v)
    {
        var icult = System.Globalization.CultureInfo.InvariantCulture;
        w.WriteLine(String.Format(icult, "{0} {1} {2} {3}", head, v.X, v.Y, v.Z));
    }

    public void WriteObj(TextWriter w)
    {
        for (int i = 0; i < Vertices.Length; i++)
        {
            var v = Vertices[i];
            WriteObjVector(w, "v", v);
        }
        for (int i = 0; i < Normals.Length; i++)
        {
            var vn = Normals[i];
            WriteObjVector(w, "vn", vn);
        }
        for (int i = 0; i < Triangles.Length; i += 3)
        {
            var f = Triangles[i];
            w.WriteLine($"f {f + 1}//{f + 1} {Triangles[i + 1] + 1}//{Triangles[i + 1] + 1} {Triangles[i + 2] + 1}//{Triangles[i + 2] + 1}");
        }
    }

    public void WriteObj(string path)
    {
        using (var w = new StreamWriter(path))
        {
            WriteObj(w);
        }
    }
}
