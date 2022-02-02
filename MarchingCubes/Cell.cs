//
// Based on https://github.com/scikit-image/scikit-image/blob/main/skimage/measure/_marching_cubes_lewiner_cy.pyx
// Copyright (C) 2019, the scikit-image team
// All rights reserved.
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//  1. Redistributions of source code must retain the above copyright
//     notice, this list of conditions and the following disclaimer.
//  2. Redistributions in binary form must reproduce the above copyright
//     notice, this list of conditions and the following disclaimer in
//     the documentation and/or other materials provided with the
//     distribution.
//  3. Neither the name of skimage nor the names of its contributors may be
//     used to endorse or promote products derived from this software without
//     specific prior written permission.
// THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
// STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
// IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
//

namespace MarchingCubes;

/// <summary>
/// Class to keep track of some stuff during the whole cube marching
/// procedure.
/// This "struct" keeps track of the current cell location, and the values
/// of corners of the cube. Gradients for the cube corners are calculated
/// when needed.
/// Additionally, it keeps track of the array of vertices, faces and normals.
/// Notes on vertices
/// -----------------
/// The vertices are stored in a C-array that is increased in size with
/// factors of two if needed. The same applies to the faces and normals.
/// Notes on faces
/// --------------
/// To keep track of the vertices already defined, this class maintains
/// two faceLayer arrays. faceLayer1 is of the current layer (z-value)
/// and faceLayer2 is of the next. Both face layers have 4 elements per
/// cell in that layer, 1 for each unique edge per cell (see
/// get_index_in_facelayer). These are initialized as -1, and set to the
/// index in the vertex array when a new vertex is created.
/// In summary, this allows us to keep track of the already created
/// vertices without keeping a very big array.
/// Notes on normals
/// ----------------
/// The normal is simply defined as the gradient. Each time that a face is
/// created, we also add the gradient of that vertex position to the
/// normals array. The gradients are all calculated from the differences between
/// the 8 corners of the current cube, but because the final value of a normal
/// was contributed from multiple cells, the normals are quite accurate.
/// </summary>
public class Cell
{
    const double FLT_EPSILON = 0.0000001;

    readonly List<Vector3> vertices = new(8);
    readonly List<Vector3> normals = new(8);
    readonly List<double> values = new(8);
    readonly List<int> faces = new(8);

    /// <summary>
    /// Values of cube corners (isovalue subtracted)
    /// </summary>
    double v0, v1, v2, v3, v4, v5, v6, v7;

    readonly double[] vv = new double[8];
    readonly double[] vg = new double[8 * 3];

    bool v12Calculated;
    double v12_xg, v12_yg, v12_zg;
    float v12_x, v12_y, v12_z;

    /// <summary>
    /// Max value of the eight corners
    /// </summary>
    double vmax = 0.0;

    int VertexCount => vertices.Count;

    public Vector3[] Vertices => vertices.ToArray();
    public Vector3[] Normals => normals.ToArray();
    public int[] Faces => faces.ToArray();

    readonly int nx;
    readonly int ny;
    readonly int nz;

    int x, y, z;
    int step;

    /// <summary>
    /// The current face layer
    /// </summary>
    int[] faceLayer;
    int[] faceLayer1;
    int[] faceLayer2;

    int index;
    public int Index => index;

    readonly LutProvider luts;

    public Cell(LutProvider luts, int nx, int ny, int nz)
    {
        this.luts = luts;
        this.nx = nx;
        this.ny = ny;
        this.nz = nz;
        faceLayer1 = new int[nx * ny * 4];
        faceLayer2 = new int[nx * ny * 4];
        for (int i = 0; i < faceLayer1.Length; i++)
        {
            faceLayer1[i] = -1;
            faceLayer2[i] = -1;
        }
        faceLayer = faceLayer1;
    }

    int AddVertex(float x, float y, float z)
    {
        vertices.Add(new Vector3(x, y, z));
        normals.Add(Vector3.Zero);
        values.Add(0.0);
        return VertexCount - 1;
    }

    void AddGradient(int vertexIndex, double gx, double gy, double gz) =>
        normals[vertexIndex] += new Vector3((float)gx, (float)gy, (float)gz);

    void AddGradientFromIndex(int vertexIndex, int i, double strength) =>
        AddGradient(vertexIndex, vg[i * 3 + 0] * strength, vg[i * 3 + 1] * strength, vg[i * 3 + 2] * strength);

    void AddFace(int vertexIndex)
    {
        faces.Add(vertexIndex);
        if (vmax > values[vertexIndex])
        {
            values[vertexIndex] = vmax;
        }
    }

    /// <summary>
    /// This method should be called each time a new z layer is entered.
    /// We will swap the layers with face information and empty the second.
    /// </summary>
    public void NewZValue()
    {
        var tmp = faceLayer1;
        faceLayer1 = faceLayer2;
        faceLayer2 = tmp;
        for (var i = 0; i < faceLayer2.Length; i++)
        {
            faceLayer2[i] = -1;
        }
    }

    /// <summary>
    /// Set the values of the cube corners. The isovalue is subtracted
    /// from them, such that in further calculations the isovalue can be
    /// taken as zero.
    /// This method also calculated the magic 256 word to identify the
    /// cases (i.e. cell.index).
    /// </summary>
    public void SetCube(double isovalue,
                        int x, int y, int z, int step,
                        double v0, double v1, double v2, double v3, double v4, double v5, double v6, double v7)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.step = step;

        this.v0 = v0 - isovalue;
        this.v1 = v1 - isovalue;
        this.v2 = v2 - isovalue;
        this.v3 = v3 - isovalue;
        this.v4 = v4 - isovalue;
        this.v5 = v5 - isovalue;
        this.v6 = v6 - isovalue;
        this.v7 = v7 - isovalue;

        // Calculate index
        int index = 0;
        if (this.v0 > 0.0) index += 1;
        if (this.v1 > 0.0) index += 2;
        if (this.v2 > 0.0) index += 4;
        if (this.v3 > 0.0) index += 8;
        if (this.v4 > 0.0) index += 16;
        if (this.v5 > 0.0) index += 32;
        if (this.v6 > 0.0) index += 64;
        if (this.v7 > 0.0) index += 128;
        this.index = index;

        // Reset c12
        v12Calculated = false;
    }

    /// <summary>
    /// The vertices for the triangles are specified in the given Lut at the specified index. There are nt triangles.
    /// </summary>
    void AddTriangles(Lut lut, int lutIndex, int nt)
    {
        PrepareForAddingTriangles();
        for (int i = 0; i < nt; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                var vi = lut.Get2(lutIndex, i * 3 + j);
                AddFaceFromEdgeIndex(vi);
            }
        }
    }

    /// <summary>
    /// Same as AddTriangles, except that now the geometry is in a LUT with 3 dimensions, and an extra index is provided.
    /// </summary>
    void AddTriangles2(Lut lut, int lutIndex, int lutIndex2, int nt)
    {
        PrepareForAddingTriangles();
        for (int i = 0; i < nt; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                var vi = lut.Get3(lutIndex, lutIndex2, i * 3 + j);
                AddFaceFromEdgeIndex(vi);
            }
        }
    }

    /// <summary>
    /// Add one face from an edge index.
    /// Only adds a face if the vertex already exists.
    /// Otherwise also adds a vertex and applies interpolation.
    /// </summary>
    void AddFaceFromEdgeIndex(int vi)
    {
        int dx1, dy1, dz1;
        int dx2, dy2, dz2;
        int index1, index2;
        double tmpf1, tmpf2;
        double fx, fy, fz, ff;
        double stp = this.step;

        // Get index in the face layer and corresponding vertex number
        var indexInFaceLayer = GetIndexInFacelayer(vi);
        var indexInVertexArray = faceLayer[indexInFaceLayer];

        // If we have the center vertex, we have things pre-calculated,
        // otherwise we need to interpolate.
        // In both cases we distinguish between having this vertex already
        // or not.
        if (vi == 12)
        {
            // center vertex
            if (!v12Calculated)
                CalculateCenterVertex();
            if (indexInVertexArray >= 0)
            {
                // Vertex already calculated, only need to add face and gradient
                AddFace(indexInVertexArray);
                AddGradient(indexInVertexArray, v12_xg, v12_yg, v12_zg);
            }
            else
            {
                // Add precalculated center vertex position (is interpolated)
                indexInVertexArray = AddVertex(v12_x, v12_y, v12_z);
                // Update face layer
                faceLayer[indexInFaceLayer] = indexInVertexArray;
                // Add face and gradient
                AddFace(indexInVertexArray);
                AddGradient(indexInVertexArray, v12_xg, v12_yg, v12_zg);
            }
        }
        else
        {
            // Get relative edge indices for x, y and z
            (dx1, dx2) = (luts.EDGESRELX.Get2(vi, 0), luts.EDGESRELX.Get2(vi, 1));
            (dy1, dy2) = (luts.EDGESRELY.Get2(vi, 0), luts.EDGESRELY.Get2(vi, 1));
            (dz1, dz2) = (luts.EDGESRELZ.Get2(vi, 0), luts.EDGESRELZ.Get2(vi, 1));
            // Make two vertex indices
            index1 = dz1 * 4 + dy1 * 2 + dx1;
            index2 = dz2 * 4 + dy2 * 2 + dx2;
            // Define strength of both corners
            tmpf1 = 1.0 / (FLT_EPSILON + Math.Abs(vv[index1]));
            tmpf2 = 1.0 / (FLT_EPSILON + Math.Abs(vv[index2]));

            // print('indexInVertexArray', x, y, z, '-', vi, indexInVertexArray, indexInFaceLayer)

            if (indexInVertexArray >= 0)
            {
                // Vertex already calculated, only need to add face and gradient
                AddFace(indexInVertexArray);
                AddGradientFromIndex(indexInVertexArray, index1, tmpf1);
                AddGradientFromIndex(indexInVertexArray, index2, tmpf2);

            }
            else
            {
                // Interpolate by applying a kind of center-of-mass method
                (fx, fy, fz, ff) = (0.0, 0.0, 0.0, 0.0);
                fx += dx1 * tmpf1; fy += dy1 * tmpf1; fz += dz1 * tmpf1; ff += tmpf1;
                fx += dx2 * tmpf2; fy += dy2 * tmpf2; fz += dz2 * tmpf2; ff += tmpf2;

                // Add vertex
                indexInVertexArray = AddVertex(
                                (float)(x + stp * fx / ff),
                                (float)(y + stp * fy / ff),
                                (float)(z + stp * fz / ff));
                // Update face layer
                faceLayer[indexInFaceLayer] = indexInVertexArray;
                // Add face and gradient
                AddFace(indexInVertexArray);
                AddGradientFromIndex(indexInVertexArray, index1, tmpf1);
                AddGradientFromIndex(indexInVertexArray, index2, tmpf2);
            }
        }
    }

    /// <summary>
    /// Get the index of a vertex position, given the edge on which it lies.
    /// We keep a list of faces so we can reuse vertices. This improves
    /// speed because we need less interpolation, and the result is more
    /// compact and can be visualized better because normals can be
    /// interpolated.
    /// For each cell, we store 4 vertex indices; all other edges can be
    /// represented as the edge of another cell.  The fourth is the center vertex.
    /// This method returns -1 if no vertex has been defined yet.
    /// </summary>
    int GetIndexInFacelayer(int vi)
    {
        int i = this.nx * this.y + this.x;  // Index of cube to get vertex at
        int j = 0; // Vertex number for that cell
        int vi_ = vi;

        int[] faceLayer;

        // Select either upper or lower half
        if (vi < 8)
        {
            //  8 horizontal edges
            if (vi < 4)
            {
                faceLayer = this.faceLayer1;
            }
            else
            {
                vi -= 4;
                faceLayer = this.faceLayer2;
            }

            // Calculate actual index based on edge
            //if (vi == 0) { pass  // no step
            if (vi == 1)
            {  // step in x
                i += this.step;
                j = 1;
            }
            else if (vi == 2)
            {  // step in y
                i += this.nx * this.step;
            }
            else if (vi == 3)
            {  // no step
                j = 1;
            }

        }
        else if (vi < 12)
        {
            // 4 vertical edges
            faceLayer = this.faceLayer1;
            j = 2;

            //if (vi == 8) { pass // no step
            if (vi == 9)
            {   // step in x
                i += this.step;
            }
            else if (vi == 10)
            {   // step in x and y
                i += this.nx * this.step + this.step;
            }
            else if (vi == 11)
            {  // step in y
                i += this.nx * this.step;
            }

        }
        else
        {
            // center vertex
            faceLayer = this.faceLayer1;
            j = 3;
        }

        // Store facelayer and return index
        this.faceLayer = faceLayer; // Dirty way of returning a value
        return 4 * i + j;
    }

    /// <summary>
    /// Calculates some things to help adding the triangles:
    /// array with corner values, max corner value, gradient at each corner.
    /// </summary>
    void PrepareForAddingTriangles()
    {
        int i;

        // Copy values in array so we can index them. Note the misalignment
        // because the numbering does not correspond with bitwise OR of xyz.
        this.vv[0] = this.v0;
        this.vv[1] = this.v1;
        this.vv[2] = this.v3;//
        this.vv[3] = this.v2;//
        this.vv[4] = this.v4;
        this.vv[5] = this.v5;
        this.vv[6] = this.v7;//
        this.vv[7] = this.v6;//

        // Calculate max
        double vmin = 0.0, vmax = 0.0;
        for (i = 0; i < 8; i++)
        {
            if (this.vv[i] > vmax)
            {
                vmax = this.vv[i];
            }
            if (this.vv[i] < vmin)
            {
                vmin = this.vv[i];
            }
        }
        this.vmax = vmax - vmin;

        // Calculate gradients
        // Derivatives, selected to always point in same direction.
        // Note that many corners have the same components as other points,
        // by interpolating  and averaging the normals this is solved.
        // todo: we can potentially reuse these similar to how we store vertex indices in face layers
        (this.vg[0 * 3 + 0], this.vg[0 * 3 + 1], this.vg[0 * 3 + 2]) = (this.v0 - this.v1, this.v0 - this.v3, this.v0 - this.v4);
        (this.vg[1 * 3 + 0], this.vg[1 * 3 + 1], this.vg[1 * 3 + 2]) = (this.v0 - this.v1, this.v1 - this.v2, this.v1 - this.v5);
        (this.vg[2 * 3 + 0], this.vg[2 * 3 + 1], this.vg[2 * 3 + 2]) = (this.v3 - this.v2, this.v1 - this.v2, this.v2 - this.v6);
        (this.vg[3 * 3 + 0], this.vg[3 * 3 + 1], this.vg[3 * 3 + 2]) = (this.v3 - this.v2, this.v0 - this.v3, this.v3 - this.v7);
        (this.vg[4 * 3 + 0], this.vg[4 * 3 + 1], this.vg[4 * 3 + 2]) = (this.v4 - this.v5, this.v4 - this.v7, this.v0 - this.v4);
        (this.vg[5 * 3 + 0], this.vg[5 * 3 + 1], this.vg[5 * 3 + 2]) = (this.v4 - this.v5, this.v5 - this.v6, this.v1 - this.v5);
        (this.vg[6 * 3 + 0], this.vg[6 * 3 + 1], this.vg[6 * 3 + 2]) = (this.v7 - this.v6, this.v5 - this.v6, this.v2 - this.v6);
        (this.vg[7 * 3 + 0], this.vg[7 * 3 + 1], this.vg[7 * 3 + 2]) = (this.v7 - this.v6, this.v4 - this.v7, this.v3 - this.v7);
    }

    void CalculateCenterVertex()
    {
        double v0, v1, v2, v3, v4, v5, v6, v7;
        double fx = 0.0, fy = 0.0, fz = 0.0, ff = 0.0;

        // Define "strength" of each corner of the cube that we need
        v0 = 1.0 / (FLT_EPSILON + Math.Abs(this.v0));
        v1 = 1.0 / (FLT_EPSILON + Math.Abs(this.v1));
        v2 = 1.0 / (FLT_EPSILON + Math.Abs(this.v2));
        v3 = 1.0 / (FLT_EPSILON + Math.Abs(this.v3));
        v4 = 1.0 / (FLT_EPSILON + Math.Abs(this.v4));
        v5 = 1.0 / (FLT_EPSILON + Math.Abs(this.v5));
        v6 = 1.0 / (FLT_EPSILON + Math.Abs(this.v6));
        v7 = 1.0 / (FLT_EPSILON + Math.Abs(this.v7));

        // Apply a kind of center-of-mass method
        fx += 0.0 * v0; fy += 0.0 * v0; fz += 0.0 * v0; ff += v0;
        fx += 1.0 * v1; fy += 0.0 * v1; fz += 0.0 * v1; ff += v1;
        fx += 1.0 * v2; fy += 1.0 * v2; fz += 0.0 * v2; ff += v2;
        fx += 0.0 * v3; fy += 1.0 * v3; fz += 0.0 * v3; ff += v3;
        fx += 0.0 * v4; fy += 0.0 * v4; fz += 1.0 * v4; ff += v4;
        fx += 1.0 * v5; fy += 0.0 * v5; fz += 1.0 * v5; ff += v5;
        fx += 1.0 * v6; fy += 1.0 * v6; fz += 1.0 * v6; ff += v6;
        fx += 0.0 * v7; fy += 1.0 * v7; fz += 1.0 * v7; ff += v7;

        // Store
        double stp = (double)this.step;
        this.v12_x = (float)(this.x + stp * fx / ff);
        this.v12_y = (float)(this.y + stp * fy / ff);
        this.v12_z = (float)(this.z + stp * fz / ff);

        // Also pre-calculate gradient of center
        // note that prepare_for_adding_triangles() must have been called for
        // the gradient data to exist.
        this.v12_xg = (v0 * this.vg[0 * 3 + 0] + v1 * this.vg[1 * 3 + 0] + v2 * this.vg[2 * 3 + 0] + v3 * this.vg[3 * 3 + 0] +
                       v4 * this.vg[4 * 3 + 0] + v5 * this.vg[5 * 3 + 0] + v6 * this.vg[6 * 3 + 0] + v7 * this.vg[7 * 3 + 0]);
        this.v12_yg = (v0 * this.vg[0 * 3 + 1] + v1 * this.vg[1 * 3 + 1] + v2 * this.vg[2 * 3 + 1] + v3 * this.vg[3 * 3 + 1] +
                       v4 * this.vg[4 * 3 + 1] + v5 * this.vg[5 * 3 + 1] + v6 * this.vg[6 * 3 + 1] + v7 * this.vg[7 * 3 + 1]);
        this.v12_zg = (v0 * this.vg[0 * 3 + 2] + v1 * this.vg[1 * 3 + 2] + v2 * this.vg[2 * 3 + 2] + v3 * this.vg[3 * 3 + 2] +
                       v4 * this.vg[4 * 3 + 2] + v5 * this.vg[5 * 3 + 2] + v6 * this.vg[6 * 3 + 2] + v7 * this.vg[7 * 3 + 2]);

        // Set flag that this stuff is calculated
        this.v12Calculated = true;
    }
}
