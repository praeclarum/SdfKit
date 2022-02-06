using SdfKit;
using System.Numerics;
using System.Diagnostics;

void SphereRepeat()
{
    var r = 0.5f;
    var sdf = 
        SdfExprs
        .Sphere(r)
        .RepeatXY(
            2.25f*r, 2.25f*r,
            (i, p, d) => 0.9f*Vector3.One - Vector3.Abs(i)/6f)
        .ToSdf();
    TimeRender(sdf, nameof(SphereRepeat));
}

void SphereRepeatStatic()
{
    var r = 0.5f;
    var sdf = 
        SdfFuncs
        .Sphere(r)
        .RepeatXY(
            2.25f*r, 2.25f*r,
            (i, p, d) => 0.9f*Vector3.One - Vector3.Abs(i)/6f)
        .ToSdf();
    TimeRender(sdf, nameof(SphereRepeatStatic));
}

void TimeRender(Sdf sdf, string name,
    int loops = 3,
    int w = 1920,
    int h = 1080)
{
    var sw = new Stopwatch();
    Vec3Data? img = null;
    sw.Start();
    for (var i = 0; i < loops; i++) {
        if (i == 1)
            sw.Restart();
        img = sdf.ToImage(w, h,
            new Vector3(-2, 2, 4),
            Vector3.Zero,
            Vector3.UnitY,
            depthIterations: 40);
    }
    sw.Stop();
    if (loops > 1) loops--;
    var millis = sw.ElapsedMilliseconds / (float)loops;
    Console.WriteLine($"{name.PadLeft(20)} Render Time: {millis}ms");
}

SphereRepeat();
SphereRepeatStatic();


