namespace Tests;

public class KdTreeTests
{
    static readonly Vector3[] threePoints = new Vector3[] {
        new Vector3 (0, 0, 1),
        new Vector3 (0, 1, 0),
        new Vector3 (1, 0, 0),
    };

    [Test]
    public void ThreePoints()
    {
        var tree = new KdTree (threePoints);
        Assert.AreEqual (0, tree.SplitAxis);
        Assert.AreEqual (3, tree.TotalPoints);
        var q = new Vector3 (0.0f, 1.5f, 0.0f);
        var nearest = tree.Search (q);
        Assert.AreEqual (new Vector3 (0, 1, 0), nearest);
    }

    [Test]
    public void RandomPoints()
    {
        var randomPoints = new Vector3[10_000];
        var rng = new Random (0);
        for (int i = 0; i < randomPoints.Length; i++)
            randomPoints[i] = 1000.0f * new Vector3 (
                (float)(rng.NextDouble () * 2 - 1),
                (float)(rng.NextDouble () * 2 - 1),
                (float)(rng.NextDouble () * 2 - 1));
        var tree = new KdTree (randomPoints);
        Assert.AreEqual (randomPoints.Length, tree.TotalPoints);
        var qIndex = rng.Next (randomPoints.Length);
        var q = randomPoints[qIndex] + new Vector3 (0.01f, 0.01f, 0.01f);
        var p = randomPoints[qIndex];
        var nearest = tree.Search (q);
        Assert.AreEqual (p, nearest);
    }
}
