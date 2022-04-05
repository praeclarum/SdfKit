namespace Tests;

public class IterativeClosestPointTest
{
    static readonly Vector3[] threePoints = new Vector3[] {
        new Vector3 (0, 0, 1),
        new Vector3 (0, 1, 0),
        new Vector3 (1, 0, 0),
    };

    static Vector3[] TransformPoints (Vector3[] points, Matrix4x4 transform)
    {
        var npoints = points.Length;
        var result = new Vector3[npoints];
        for (var i = 0; i < npoints; i++) {
            result [i] = Vector3.Transform (points [i], transform);
        }
        return result;
    }

    public void ThreePointsTest (Matrix4x4 expectedTransform)
    {
        var cp = new IterativeClosestPoint (threePoints);
        var transformedPoints = TransformPoints (threePoints, expectedTransform);
        var transform = cp.RegisterPoints (transformedPoints);
        var translation = transform.Translation;
        var expectedTranslation = expectedTransform.Translation;
        Assert.AreEqual (expectedTranslation.X, translation.X, 1.0e-4f);
        Assert.AreEqual (expectedTranslation.Y, translation.Y, 1.0e-4f);
        Assert.AreEqual (expectedTranslation.Z, translation.Z, 1.0e-4f);
        Assert.AreEqual (expectedTransform.M11, transform.M11, 1.0e-6f);
        Assert.AreEqual (expectedTransform.M22, transform.M22, 1.0e-6f);
        Assert.AreEqual (expectedTransform.M33, transform.M33, 1.0e-6f);
        for (var i = 0; i < threePoints.Length; i++) {
            var p = threePoints [i];
            var q = transformedPoints [i];
            Assert.AreEqual (p.X, q.X, 1.0e-4f);
            Assert.AreEqual (p.Y, q.Y, 1.0e-4f);
            Assert.AreEqual (p.Z, q.Z, 1.0e-4f);
        }
    }

    [Test]
    public void ThreePointsOffsetX ()
    {
        ThreePointsTest (Matrix4x4.CreateTranslation (0.1f, 0, 0));
    }

    [Test]
    public void ThreePointsOffsetXYZ ()
    {
        ThreePointsTest (Matrix4x4.CreateTranslation (0.1f, -0.2f, -0.3f));
    }

    [Test]
    public void ThreePointsRotateY ()
    {
        ThreePointsTest (Matrix4x4.CreateRotationY (1.0f * MathF.PI / 180.0f));
    }

    [Test]
    public void ThreePointsRotateXOffsetY ()
    {
        ThreePointsTest (Matrix4x4.CreateRotationX (1.0f * MathF.PI / 180.0f) * Matrix4x4.CreateTranslation (0, 0.1f, 0));
    }
}
