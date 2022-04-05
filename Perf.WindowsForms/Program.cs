using SdfKit;
using System.Numerics;

namespace Perf.WindowsForms
{
  static class Program
  {
    public static Sdf MakeSdf()
    {
      var r = 0.5f;
      var boxes =
        SdfExprs
        .Box(r/2)
        .RepeatXZ(
            2.25f*r, 2.25f*r
          , (i, p, d) => 0.9f*Vector3.One - Vector3.Abs(i)/6f
          );
      var spheres =
        SdfExprs
        .Sphere(r)
        .RepeatXY(
            2.25f*r, 2.25f*r
          , (i, p, d) => 0.9f*Vector3.One - Vector3.Abs(i)/6f
          );
      return SdfExprs.Union(spheres, boxes).ToSdf();
    }

    [STAThread]
    static void Main()
    {
      ApplicationConfiguration.Initialize();

      var mainForm = new MainForm();
      mainForm.RenderSdf(MakeSdf());
      Application.Run(mainForm);
    }
  }
}