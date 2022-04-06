namespace SdfKit;

using System;
using System.Buffers;
using System.Numerics;

using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

public class IterativeClosestPoint
{
    readonly KdTree staticTree;

    readonly ArrayPool<Vector3> vpool = ArrayPool<Vector3>.Shared;
    readonly ArrayPool<float> fpool = ArrayPool<float>.Shared;

    public int MaxIterations { get; set; } = 100;

    // public int MaxCorresponses { get; } = 1000;

    public float GoodCorrespondenceDistance { get; set; } = 0.01f;

    public float ConvergedMaximumTranslation { get; set; } = 1.0e-4f;

    public float ConvergedMaximumRotation { get; set; } = 1.0e-5f;

    public IterativeClosestPoint (ReadOnlySpan<Vector3> staticPoints)
    {
        staticTree = new KdTree (staticPoints);
    }

    public IterativeClosestPoint (ReadOnlyMemory<Vector3>[] staticPoints)
    {
        var n = staticPoints.Length;
        if (n == 0)
            throw new ArgumentException ("At least one set of points must be given", nameof (staticPoints));
        staticTree = new KdTree (staticPoints[0].Span);
        for (int i = 1; i < n; i++)
            staticTree.AddPoints (staticPoints[i].Span);
    }

    public void AddStaticPoints (ReadOnlySpan<Vector3> staticPoints)
    {
        staticTree.AddPoints (staticPoints);
    }

    /// <summary>
    /// Rigidly move the given points to align with the static points
    /// used to construct this instance.
    /// The returned transform is the one used to convert
    /// the given points to their new locations.
    /// </summary>
    public Matrix4x4 RegisterPoints (Span<Vector3> points)
    {
        var totalTransform = Matrix4x4.Identity;

        var npoints = points.Length;
        Matrix<float> p = Matrix<float>.Build.Dense (npoints, 3);
        Matrix<float> q = Matrix<float>.Build.Dense (npoints, 3);

        var lastTransform = Matrix4x4.Identity;
        var converged = false;
        for (var iter = 0; !converged && iter < MaxIterations; iter++) {
            var transform = GetIterTransform (points, p, q);

            var delta = transform;
            var drot = MathF.Abs (1.0f-delta.M11) + MathF.Abs (1.0f-delta.M22) + MathF.Abs (1.0f-delta.M33);
            var dtrans = delta.Translation.Length ();
            converged = dtrans <= ConvergedMaximumTranslation && drot <= ConvergedMaximumRotation;

            lastTransform = transform;
            totalTransform = totalTransform * transform;
        }
        return totalTransform;
    }

    Matrix4x4 GetIterTransform (Span<Vector3> points, Matrix<float> p, Matrix<float> q)
    {
        var npoints = points.Length;
        var corA = vpool.Rent (npoints);
        var distA = fpool.Rent (npoints);

        try {
            // Find the corresponding points in the static tree
            var cor = corA.AsSpan (0, npoints);
            var dist = distA.AsSpan (0, npoints);
            var distMean = 0.0f;
            for (var i = 0; i < npoints; i++) {
                cor[i] = staticTree.Search (points[i], out dist[i]);
                distMean += dist[i];
            }
            distMean /= npoints;

            // Find distance statistics for filtering
            var distStd = 0.0f;
            for (var i = 0; i < npoints; i++) {
                var d = dist[i] - distMean;
                distStd += d * d;
            }
            distStd = MathF.Sqrt (distStd / npoints);
            float distMax;
            if (distMean < GoodCorrespondenceDistance) {
                distMax = distMean + 3.0f * distStd;
            }
            else if (distMean < 3.0f * GoodCorrespondenceDistance) {
                distMax = distMean + 2.0f * distStd;
            }
            else if (distMean < 6.0f * GoodCorrespondenceDistance) {
                distMax = distMean + distStd;
            }
            else {
                // "We have chosen in our implementation the valley after the maximal peak as the value of distMax"
                distMax = distMean + 0.5f + distStd;
            }

            // Filter out points that are too far away and find the mean points
            // p points are the dynamic points
            // q points are the corresponding static points
            var nfilter = 0;
            var pmean = Vector3.Zero;
            var qmean = Vector3.Zero;
            for (var i = 0; i < npoints; i++) {
                if (dist[i] <= distMax) {
                    p[nfilter, 0] = points[i].X;
                    p[nfilter, 1] = points[i].Y;
                    p[nfilter, 2] = points[i].Z;
                    q[nfilter, 0] = cor[i].X;
                    q[nfilter, 1] = cor[i].Y;
                    q[nfilter, 2] = cor[i].Z;
                    nfilter++;
                    pmean += points[i];
                    qmean += cor[i];
                }
            }
            pmean /= nfilter;
            qmean /= nfilter;

            // Center p and q using their means
            for (var i = 0; i < nfilter; i++) {
                p[i, 0] -= pmean.X;
                p[i, 1] -= pmean.Y;
                p[i, 2] -= pmean.Z;
                q[i, 0] -= qmean.X;
                q[i, 1] -= qmean.Y;
                q[i, 2] -= qmean.Z;
            }

            // Compute the cross correlation matrix
            var c = Matrix<float>.Build.Dense (3, 3);
            for (var i = 0; i < nfilter; i++) {
                var px = p[i, 0];
                var py = p[i, 1];
                var pz = p[i, 2];
                var qx = q[i, 0];
                var qy = q[i, 1];
                var qz = q[i, 2];
                c[0, 0] += px * qx;
                c[0, 1] += px * qy;
                c[0, 2] += px * qz;
                c[1, 0] += py * qx;
                c[1, 1] += py * qy;
                c[1, 2] += py * qz;
                c[2, 0] += pz * qx;
                c[2, 1] += pz * qy;
                c[2, 2] += pz * qz;
            }

            // Use Svd to compute the rotation matrix
            var svd = c.Svd ();
            var u = svd.U;
            var ut = u.Transpose ();
            var vt = svd.VT;
            var v = vt.Transpose ();
            var detSign = MathF.Sign ((v * ut).Determinant ());
            var sd = Matrix<float>.Build.Diagonal (new float[] { 1, 1, detSign });
            var r = v * sd * ut;
            var rMatrix = new Matrix4x4 (
                r[0, 0], r[0, 1], r[0, 2], 0,
                r[1, 0], r[1, 1], r[1, 2], 0,
                r[2, 0], r[2, 1], r[2, 2], 0,
                0, 0, 0, 1
            );
            Matrix4x4.Invert (rMatrix, out var invRMatrix);

            // Calculate the translation vector
            var pRotatedMean = Vector3.Transform (pmean, invRMatrix);
            var translation = pRotatedMean - qmean;

            // Calculate the transformation matrix
            var transformMatrix = rMatrix * Matrix4x4.CreateTranslation (translation);

            // Apply the transformation to the points
            Matrix4x4.Invert (transformMatrix, out var invTransformMatrix);
            for (var i = 0; i < npoints; i++) {
                points[i] = Vector3.Transform (points[i], invTransformMatrix);
            }

            // Return the transformation matrix
            return invTransformMatrix;
        }
        finally {
            vpool.Return (corA);
            fpool.Return (distA);
        }
    }

    public Matrix4x4[] GlobalRegisterPoints (ReadOnlyMemory<Vector3>[] staticPoints, Memory<Vector3>[] dynamicPoints)
    {
        var n = dynamicPoints.Length;
        if (n == 0) {
            return Array.Empty<Matrix4x4> ();
        }
        var icp = new IterativeClosestPoint (staticPoints);
        var transforms = new Matrix4x4[n];
        for (var i = 0; i < n; i++) {
            var dpoints = dynamicPoints[i].Span;
            transforms[i] = icp.RegisterPoints (dpoints);
            var goodRegistration = true;
            if (goodRegistration) {
                icp.AddStaticPoints (dpoints);
            }
        }
        return transforms;
    }

    public Matrix4x4[] GlobalRegisterPoints (Memory<Vector3>[] points)
    {
        var n = points.Length;
        if (n == 0) {
            return Array.Empty<Matrix4x4> ();
        }
        else if (n == 1) {
            return new Matrix4x4[] { Matrix4x4.Identity };
        }
        var spoints = points.Take (1).Select(x => (ReadOnlyMemory<Vector3>)x).ToArray ();
        var dpoints = points.Skip (1).ToArray ();
        return GlobalRegisterPoints (spoints, dpoints);
    }

}

