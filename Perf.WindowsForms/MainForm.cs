using SdfKit;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Numerics;

namespace Perf.WindowsForms
{
  public partial class MainForm : Form
  {
    Bitmap? bitmap;

    public MainForm()
    {
      InitializeComponent();
    }

    public void RenderSdf(Sdf sdf)
    {
      var sz = ClientSize;
      var sync = SynchronizationContext.Current!;
      Task.Run(() => BackgroundRenderSdf(sync, sz, sdf));
    }

    void BackgroundRenderSdf(SynchronizationContext sync, Size sz, Sdf sdf)
    {
      sync.RunInUIThread(() =>
      {
        Text = "Rendering SDF...";
      });

      var sw = Stopwatch.StartNew();

      using var image = sdf.ToImage(
          sz.Width
        , sz.Height
        , new Vector3(-2, 2, 4)
        , Vector3.Zero
        , Vector3.UnitY
        , depthIterations: 40
        );
      var bmp = new Bitmap(sz.Width, sz.Height, PixelFormat.Format24bppRgb);
      var bits = bmp.LockBits(new Rectangle(0,0,sz.Width,sz.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
      try
      {
        unsafe
        {
          var bytes = (byte*)bits.Scan0;
          for (var y = 0; y < sz.Height; ++y)
          {
            var line = bytes+bits.Stride*y;
            for (var x = 0; x < sz.Width; ++x)
            {
              var px = image[x, y];
              var off = x*3;
              static byte ToByte(float v) =>
                (byte)Math.Round(v*255.0F)
                ;
              line[off + 0] = ToByte(px.X);
              line[off + 1] = ToByte(px.Y);
              line[off + 2] = ToByte(px.Z);
            }
          }
        }
      }
      finally
      {
        bmp.UnlockBits(bits);
      }
      sw.Stop();
      var seconds = Math.Round(sw.ElapsedMilliseconds/1000.0, 2);

      sync.RunInUIThread(() =>
      {
        var oldBmp = bitmap;
        bitmap = null;
        oldBmp?.Dispose();

        bitmap = bmp;

        Text = $"Rendering SDF took {seconds} secs";

        Invalidate();
      });
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);
      var g = e.Graphics;
      if (bitmap is not null)
      {
        g.DrawImageUnscaled(bitmap, new Point(0, 0));
      }
    }
  }
}