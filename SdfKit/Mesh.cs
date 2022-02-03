namespace SdfKit;

using System.IO;

public class Mesh
{
    public Vector3[] Vertices { get; }
    public Vector3[] Normals { get; }
    public int[] Triangles { get; }

    public Vector3 Min { get; private set; }
    public Vector3 Max { get; private set; }
    public Vector3 Center => (Min + Max) * 0.5f;
    public Vector3 Size => Max - Min;
    public float Radius => (Max - Min).Length() * 0.5f;

    public Mesh(Vector3[] vertices, Vector3[] normals, int[] triangles)
    {
        Vertices = vertices;
        Normals = normals;
        Triangles = triangles;
        Measure();
    }

    void Measure()
    {
        if (Vertices.Length > 0)
        {
            var min = Vertices[0];
            var max = min;
            for (int i = 1; i < Vertices.Length; i++)
            {
                var v = Vertices[i];
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }
            Min = min;
            Max = max;
        }
    }

    public void Transform(Matrix4x4 transform)
    {
        var normalTransform = transform;
        normalTransform.M41 = 0;
        normalTransform.M42 = 0;
        normalTransform.M43 = 0;
        normalTransform.M44 = 1;
        Matrix4x4.Invert(normalTransform, out var itransform);
        normalTransform = Matrix4x4.Transpose(itransform);
        
        for (int i = 0; i < Vertices.Length; i++) {
            var tv = Vector4.Transform(Vertices[i], transform);
            var tvn = Vector4.Transform(new Vector4(Normals[i], 0), normalTransform);
            Vertices[i] = new Vector3(tv.X, tv.Y, tv.Z);
            Normals[i] = Vector3.Normalize(new Vector3(tvn.X, tvn.Y, tvn.Z));
        }
        Measure();
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
