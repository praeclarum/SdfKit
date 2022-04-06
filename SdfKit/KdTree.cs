namespace SdfKit;

using System.Buffers;
using System.Numerics;

/// <summary>
/// A binary tree of points optimized to search for nearest points.
/// </summary>
public class KdTree
{
    public Vector3 Point;
    public float SplitValue;
    public KdTree? Left;
    public KdTree? Right;
    /// <summary>
    /// 0 = x, 1 = y, 2 = z
    /// </summary>
    public readonly byte SplitAxis;

    public bool IsLeaf => Left == null && Right == null;

    public int TotalPoints
    {
        get
        {
            if (Left != null || Right != null) {
                var nl = Left?.TotalPoints ?? 0;
                var nr = Right?.TotalPoints ?? 0;
                return nl + nr;
            }
            return 1;
        }
    }

    public KdTree (Vector3 point, byte axis)
    {
        Point = point;
        SplitAxis = axis;
    }

    public KdTree (ReadOnlySpan<Vector3> points, byte axis = 0)
        : this (points.Length > 0 ? points[0] : throw new ArgumentException ("At least on point must be given", nameof (points)), axis)
    {
        AddPoints (points.Slice (1));
    }

    public void AddPoints (ReadOnlySpan<Vector3> points)
    {
        var npoints = points.Length;
        if (npoints == 0)
            return;

        var axis = SplitAxis;
        byte nextAxis = (byte)((SplitAxis + 1) % 3);

        var wasLeaf = IsLeaf;
        if (wasLeaf) {
            var splitValue = 0.0f;
            var di = npoints < 10 ? 1 : npoints / 10;
            var nsplits = 1;
            if (axis == 0) {
                splitValue = Point.X;
                for (var i = 0; i < npoints; i += di) {
                    splitValue += points[i].X;
                    nsplits++;
                }
            }
            else if (axis == 1) {
                splitValue = Point.Y;
                for (var i = 0; i < npoints; i += di) {
                    splitValue += points[i].Y;
                    nsplits++;
                }
            }
            else if (axis == 2) {
                splitValue = Point.Z;
                for (var i = 0; i < npoints; i += di) {
                    splitValue += points[i].Z;
                    nsplits++;
                }
            }
            SplitValue = splitValue / nsplits;
        }
        var pool = ArrayPool<Vector3>.Shared;
        var left = pool.Rent (npoints + 1);
        var right = pool.Rent (npoints + 1);
        var leftCount = 0;
        var rightCount = 0;
        if (axis == 0) {
            if (wasLeaf) {
                var p = Point;
                if (p.X <= SplitValue)
                    left[leftCount++] = p;
                else
                    right[rightCount++] = p;
            }
            for (int i = 0; i < npoints; i++)
            {
                var p = points[i];
                if (p.X <= SplitValue)
                    left[leftCount++] = p;
                else
                    right[rightCount++] = p;
            }
        }
        else if (axis == 1) {
            if (wasLeaf) {
                var p = Point;
                if (p.Y <= SplitValue)
                    left[leftCount++] = p;
                else
                    right[rightCount++] = p;
            }
            for (int i = 0; i < npoints; i++)
            {
                var p = points[i];
                if (p.Y <= SplitValue)
                    left[leftCount++] = p;
                else
                    right[rightCount++] = p;
            }
        }
        else {
            if (wasLeaf) {
                var p = Point;
                if (p.Z <= SplitValue)
                    left[leftCount++] = p;
                else
                    right[rightCount++] = p;
            }
            for (int i = 0; i < npoints; i++)
            {
                var p = points[i];
                if (p.Z <= SplitValue)
                    left[leftCount++] = p;
                else
                    right[rightCount++] = p;
            }
        }
        if (leftCount > 0) {
            if (Left is KdTree ltree) {
                ltree.AddPoints (left.AsSpan (0, leftCount));
            }
            else {
                Left = new KdTree (left.AsSpan (0, leftCount), nextAxis);
            }
        }
        if (rightCount > 0) {
            if (Right is KdTree rtree) {
                rtree.AddPoints (right.AsSpan (0, rightCount));
            }
            else {
                Right = new KdTree (right.AsSpan (0, rightCount), nextAxis);
            }
        }
        pool.Return (left);
        pool.Return (right);
    }

    public Vector3 Search (Vector3 q, out float nearestDistance)
    {
        var nearest = Point;
        nearestDistance = float.MaxValue;
        NearestNeighborSearch (q, this, ref nearest, ref nearestDistance);
        return nearest;
    }

    void NearestNeighborSearch (Vector3 q, KdTree node, ref Vector3 p, ref float w)
    {
        if (node.IsLeaf)
        {
            var d = Vector3.Distance (q, node.Point);
            if (d < w)
            {
                w = d;
                p = node.Point;
            }
            return;
        }
        var axis = node.SplitAxis;
        var qv = axis == 0 ? q.X : axis == 1 ? q.Y : q.Z;
        var nv = node.SplitValue;
        if (qv < nv)
        {
            if (qv - w <= nv && node.Left != null)
                NearestNeighborSearch (q, node.Left, ref p, ref w);
            if (qv + w > nv && node.Right != null)
                NearestNeighborSearch (q, node.Right, ref p, ref w);
        }
        else
        {
            if (qv + w > nv && node.Right != null)
                NearestNeighborSearch (q, node.Right, ref p, ref w);
            if (qv - w <= nv && node.Left != null)
                NearestNeighborSearch (q, node.Left, ref p, ref w);
        }
    }
}
