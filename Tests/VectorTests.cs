namespace Tests;

public class VectorTests
{
    [SetUp]
    public void Setup()
    {
    }


    [Test]
    public void BlackOnTop()
    {
        var near = 0.0f;
        var far = 1.0f;
        var image = new FloatData(30, 20);
        for (var y = 0; y < image.Height; ++y)
        {
            for (var x = 0; x < image.Width; ++x)
            {
                var color = (x, y) switch {
                    (_, <10) => far,
                    _ => near,
                };
                image[x, y] = color;
            }
        }
        image.SaveDepthTga("BlackOnTop.tga", near, far);
    }

    [Test]
    public void RedOnTop()
    {
        var image = new Vec3Data(30, 20);
        for (var y = 0; y < image.Height; ++y)
        {
            for (var x = 0; x < image.Width; ++x)
            {
                var color = (x, y) switch {
                    (_, <10) => new Vector3(1, 0, 0),
                    _ => new Vector3(0, 1, 0),
                };
                image[x, y] = color;
            }
        }
        image.SaveTga("RedOnTop.tga");
    }
}
