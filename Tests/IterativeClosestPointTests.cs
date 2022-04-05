namespace Tests;

public class IterativeClosestPointTest
{
    static readonly Vector3[] threePoints = new Vector3[] {
        new Vector3 (0, 0, 1),
        new Vector3 (0, 1, 0),
        new Vector3 (1, 0, 0),
    };
    static readonly Vector3[] threePointsOffsetX = new Vector3[] {
        new Vector3 (0+0.1f, 0, 1),
        new Vector3 (0+0.1f, 1, 0),
        new Vector3 (1+0.1f, 0, 0),
    };

    [Test]
    public void ThreePointsOffsetX ()
    {
        var cp = new IterativeClosestPoint (threePoints);
        var transform = cp.RegisterPoints (threePointsOffsetX);
        var translation = transform.Translation;
        Assert.AreEqual (-0.1f, translation.X, 1.0e-4f);
        Assert.AreEqual (0.0f, translation.Y, 1.0e-4f);
        Assert.AreEqual (0.0f, translation.Z, 1.0e-4f);
        Assert.AreEqual (1.0f, transform.M11, 1.0e-4f);
        Assert.AreEqual (1.0f, transform.M22, 1.0e-4f);
        Assert.AreEqual (1.0f, transform.M33, 1.0e-4f);
    }
}
