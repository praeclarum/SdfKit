namespace Tests;

public class IterativeClosestPointTest
{
    static readonly Vector3[] threePoints = new Vector3[] {
        new Vector3 (0, 0, 1),
        new Vector3 (0, 1, 0),
        new Vector3 (1, 0, 0),
    };

    static (Vector3[] Sources, Vector3[] Transformed) TransformPoints (Vector3[] points, Matrix4x4 transform, double keep)
    {
        var random = new Random (0);
        var npoints = points.Length;
        var sources = new List<Vector3> ();
        var result = new List<Vector3> ();
        for (var i = 0; i < npoints; i++) {
            if (random.NextDouble () < keep) {
                var t = Vector3.Transform (points [i], transform);
                sources.Add (points [i]);
                result.Add (t);
            }
        }
        return (sources.ToArray (), result.ToArray ());
    }

    void PointsTest (Vector3[] points, Matrix4x4 expectedTransform, double keep)
    {
        var cp = new IterativeClosestPoint (points);
        var (sourcePoints, transformedPoints) = TransformPoints (points, expectedTransform, keep);
        var transformedPointsCopy = transformedPoints.ToArray ();
        var invTransform = cp.RegisterPoints (transformedPoints);
        Matrix4x4.Invert (invTransform, out var transform);
        var translation = transform.Translation;
        var expectedTranslation = expectedTransform.Translation;
        Assert.AreEqual (expectedTranslation.X, translation.X, 1.0e-4f);
        Assert.AreEqual (expectedTranslation.Y, translation.Y, 1.0e-4f);
        Assert.AreEqual (expectedTranslation.Z, translation.Z, 1.0e-4f);
        Assert.AreEqual (expectedTransform.M11, transform.M11, 1.0e-6f);
        Assert.AreEqual (expectedTransform.M22, transform.M22, 1.0e-6f);
        Assert.AreEqual (expectedTransform.M33, transform.M33, 1.0e-6f);
        for (var i = 0; i < sourcePoints.Length; i++) {
            var p = sourcePoints [i];
            var q = transformedPoints [i];
            Assert.AreEqual (p.X, q.X, 1.0e-4f);
            Assert.AreEqual (p.Y, q.Y, 1.0e-4f);
            Assert.AreEqual (p.Z, q.Z, 1.0e-4f);
            var t = Vector3.Transform (transformedPointsCopy [i], invTransform);
            Assert.AreEqual (p.X, t.X, 1.0e-4f);
            Assert.AreEqual (p.Y, t.Y, 1.0e-4f);
            Assert.AreEqual (p.Z, t.Z, 1.0e-4f);
        }
    }

    void ThreePointsTest (Matrix4x4 expectedTransform)
    {
        PointsTest (threePoints, expectedTransform, keep: 1.0);
    }

    void RandomPointsTest (Matrix4x4 expectedTransform, double keep)
    {
        var random = new Random (0);
        var npoints = 100;
        var points = new Vector3[npoints];
        for (var i = 0; i < npoints; i++) {
            var x = (float)random.NextDouble () - 0.5f;
            var y = (float)random.NextDouble () - 0.5f;
            var z = (float)random.NextDouble () - 0.5f;
            points [i] = new Vector3 (x, y, z);
        }
        PointsTest (points, expectedTransform, keep);
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
        ThreePointsTest (
            Matrix4x4.CreateRotationX (1.0f * MathF.PI / 180.0f)
            * Matrix4x4.CreateTranslation (0, 0.1f, 0)
        );
    }

    [Test]
    public void ThreePointsOffsetZRotateXOffsetY ()
    {
        ThreePointsTest (
            Matrix4x4.CreateTranslation (0, 0.0f, 0.1f)
            * Matrix4x4.CreateRotationX (1.0f * MathF.PI / 180.0f)
            * Matrix4x4.CreateTranslation (0, 0.1f, 0)
        );
    }

    [Test]
    public void RandomPointsOffsetZRotateXOffsetY ()
    {
        RandomPointsTest (
            Matrix4x4.CreateTranslation (0, 0.0f, 0.1f)
            * Matrix4x4.CreateRotationX (1.0f * MathF.PI / 180.0f)
            * Matrix4x4.CreateTranslation (0, 0.1f, 0),
            keep: 0.5
        );
    }
}
