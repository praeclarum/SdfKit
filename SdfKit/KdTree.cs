namespace SdfKit;

using System.Buffers;
using System.Numerics;

/// <summary>
/// A triangle mesh with normals.
/// </summary>
public class KdTree
{
    public Vector3 Point;
    public float SplitValue;
    public KdTree? Left;
    public KdTree? Right;
    public byte SplitAxis;

    public bool IsLeaf => Left == null;

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

    public KdTree (Vector3 point)
    {
        Point = point;
    }

    public KdTree (ReadOnlySpan<Vector3> points, byte axis = 0)
    {
        System.Console.WriteLine($"KdTree.ctor: npoints={points.Length} axis={axis}");
        var npoints = points.Length;
        if (npoints == 0)
            throw new InvalidOperationException ("Points must not be empty");
        
        if (npoints == 1)
        {
            Point = points[0];
            return;
        }

        SplitAxis = axis;
        var splitValue = 0.0f;
        var di = npoints < 10 ? 1 : npoints / 10;
        var nsplits = 0;
        if (axis == 0) {
            for (var i = 0; i < npoints; i += di) {
                splitValue += points[i].X;
                nsplits++;
            }
        }
        else if (axis == 1) {
            for (var i = 0; i < npoints; i += di) {
                splitValue += points[i].Y;
                nsplits++;
            }
        }
        else if (axis == 2) {
            for (var i = 0; i < npoints; i += di) {
                splitValue += points[i].Z;
                nsplits++;
            }
        }
        SplitValue = splitValue / nsplits;
        var pool = ArrayPool<Vector3>.Shared;
        var left = pool.Rent (npoints);
        var right = pool.Rent (npoints);
        var leftCount = 0;
        var rightCount = 0;
        if (axis == 0) {
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
            for (int i = 0; i < npoints; i++)
            {
                var p = points[i];
                if (p.Z <= SplitValue)
                    left[leftCount++] = p;
                else
                    right[rightCount++] = p;
            }
        }
        if (leftCount == 1)
            Left = new KdTree (left[0]);
        else if (leftCount > 1)
            Left = new KdTree (left.AsSpan (0, leftCount), (byte)((axis + 1) % 3));
        if (rightCount > 0)
            Right = new KdTree (right.AsSpan (0, rightCount), (byte)((axis + 1) % 3));
        pool.Return (left);
        pool.Return (right);
    }

    public Vector3 Search (Vector3 q)
    {
        var nearest = Point;
        var nearestDistance = float.MaxValue;
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
