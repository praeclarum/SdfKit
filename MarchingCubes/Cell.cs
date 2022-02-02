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
public class Cell {

    readonly List<Vector3> vertices = new (8);
    readonly List<Vector3> normals = new (8);
    readonly List<double> values = new (8);
    readonly List<int> faces = new (8);

    /// <summary>
    /// Values of cube corners (isovalue subtracted)
    /// </summary>
    double v0, v1, v2, v3,v4,v5,v6,v7;

    readonly double[] vv = new double[8];
    readonly Vector3[] vg = new Vector3[8];

    /// <summary>
    /// Max value of the eight corners
    /// </summary>
    double vmax = 0.0;

    int VertexCount => vertices.Count;

    readonly int nx;
    readonly int ny;
    readonly int nz;

    /// <summary>
    /// The current face layer
    /// </summary>
    int[] faceLayer;
    int[] faceLayer1;
    int[] faceLayer2;

    public Cell(int nx, int ny, int nz) {
        this.nx = nx;
        this.ny = ny;
        this.nz = nz;
        faceLayer1 = new int[nx * ny * 4];
        faceLayer2 = new int[nx * ny * 4];
        for (int i = 0; i < faceLayer1.Length; i++) {
            faceLayer1[i] = -1;
            faceLayer2[i] = -1;
        }
        faceLayer = faceLayer1;
    }

    int AddVertex(float x, float y, float z) {
        vertices.Add(new Vector3(x, y, z));
        normals.Add(Vector3.Zero);
        values.Add(0.0);
        return VertexCount - 1;
    }

    void AddGradient(int vertexIndex, float x, float y, float z) =>
        normals[vertexIndex] = new Vector3(x, y, z);

    void AddGradientFromIndex(int vertexIndex, int i, float strength) =>
        AddGradient(vertexIndex, vg[i].X * strength, vg[i].Y * strength, vg[i].Z * strength);

    void AddFace(int vertexIndex) {
        faces.Add(vertexIndex);
        var v = vertices[vertexIndex];
        if (vmax > values[vertexIndex]) {
            values[vertexIndex] = vmax;
        }
    }

    /// <summary>
    /// This method should be called each time a new z layer is entered.
    /// We will swap the layers with face information and empty the second.
    /// </summary>
    void NewZValue() {
        var tmp = faceLayer1;
        faceLayer1 = faceLayer2;
        faceLayer2 = tmp;
        for (var i = 0; i < faceLayer2.Length; i++) {
            faceLayer2[i] = -1;
        }
    }
}
