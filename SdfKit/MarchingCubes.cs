//
// Based on https://github.com/scikit-image/scikit-image/blob/main/skimage/measure/_marching_cubes_lewiner_cy.pyx
// Copyright (C) 2019, the scikit-image team
// All rights reserved.
// Redistribution && use in source && binary forms, with || without
// modification, are permitted provided that the following conditions are
// met:
//  1. Redistributions of source code must retain the above copyright
//     notice, this list of conditions && the following disclaimer.
//  2. Redistributions in binary form must reproduce the above copyright
//     notice, this list of conditions && the following disclaimer in
//     the documentation and/or other materials provided with the
//     distribution.
//  3. Neither the name of skimage nor the names of its contributors may be
//     used to endorse || promote products derived from this software without
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

namespace SdfKit;

/// <summary>
/// Marching cubes algorithm (Lewiner variant).
/// </summary>
public static class MarchingCubes
{
    const double FLT_EPSILON = 0.0000001;

    public static Mesh CreateMesh(Voxels volume, float isoValue = 0.0f, int step = 1, IProgress<float>? progress = null)
    {
        int nx = volume.NX;
        int ny = volume.NY;
        int nz = volume.NZ;
        var values = volume.Values;

        var cell = new Cell(nx, ny, nz);

        var nx_bound = nx - 2*step;
        var ny_bound = ny - 2*step;
        var nz_bound = nz - 2*step;

        int z = -step;
        while (z < nz_bound)
        {
            z += step;
            var z_st = z + step;
            cell.NewZValue ();
            int y = -step;
            while (y < ny_bound)
            {
                y += step;
                var y_st = y + step;
                int x = -step;
                while (x < nx_bound)
                {
                    x += step;
                    var x_st = x + step;
                    cell.SetCube(isoValue, x, y, z, step,
                        values[x, y, z   ], values[x_st, y, z   ], values[x_st, y_st, z   ], values[x, y_st, z   ],
                        values[x, y, z_st], values[x_st, y, z_st], values[x_st, y_st, z_st], values[x, y_st, z_st]);
                    var cas = Luts.cases[cell.Index,  0];
                    if (cas > 0) {
                        var config = Luts.cases[cell.Index,  1];
                        TheBigSwitch(cell, cas, config);
                    }
                }
            }
            progress?.Report((float)z / nz_bound);
        }

        var mesh = new Mesh (cell.Vertices, cell.Normals, cell.Faces);
        var size = volume.Size;
        var transform =
            Matrix4x4.CreateTranslation(-(nx-1)/2f, -(ny-1)/2f, -(nz-1)/2f) *
            Matrix4x4.CreateScale(size.X/(nx-1), size.Y/(ny-1), size.Z/(nz-1)) *
            Matrix4x4.CreateTranslation(volume.Center);
        mesh.Transform(transform);
        return mesh;
    }

    static void TheBigSwitch(Cell cell, int cas, int config)
    {
        int subconfig = 0;

        if (cas == 1) {
            cell.AddTriangles(Luts.tiling1, config, 1);

        } else if (cas == 2) {
            cell.AddTriangles(Luts.tiling2, config, 2);

        } else if (cas == 3) {
            if (TestFace(cell, Luts.test3[config])) {
                cell.AddTriangles(Luts.tiling3_2, config, 4);
            } else {
                cell.AddTriangles(Luts.tiling3_1, config, 2);
            }

        } else if (cas == 4 ) {
            if (TestInternal(cell, cas, config, subconfig, Luts.test4[config])) {
                cell.AddTriangles(Luts.tiling4_1, config, 2);
            } else {
                cell.AddTriangles(Luts.tiling4_2, config, 6);
            }

        } else if (cas == 5 ) {
            cell.AddTriangles(Luts.tiling5, config, 3);

        } else if (cas == 6 ) {
            if (TestFace(cell, Luts.test6[config, 0])) {
                cell.AddTriangles(Luts.tiling6_2, config, 5);
            } else {
                if (TestInternal(cell, cas, config, subconfig, Luts.test6[config, 1])) {
                    cell.AddTriangles(Luts.tiling6_1_1, config, 3);
                } else {
                    //cell.CalculateCenterVertex() // v12 needed
                    cell.AddTriangles(Luts.tiling6_1_2, config, 9);
                }
            }

        } else if (cas == 7 ) {
            // Get subconfig
            if (TestFace(cell, Luts.test7[config, 0])) subconfig += 1;
            if (TestFace(cell, Luts.test7[config, 1])) subconfig += 2;
            if (TestFace(cell, Luts.test7[config, 2])) subconfig += 4;
            // Behavior depends on subconfig
            if (subconfig == 0) { cell.AddTriangles(Luts.tiling7_1, config, 3);
            } else if (subconfig == 1) { cell.AddTriangles2(Luts.tiling7_2, config, 0, 5);
            } else if (subconfig == 2) { cell.AddTriangles2(Luts.tiling7_2, config, 1, 5);
            } else if (subconfig == 3) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling7_3, config, 0, 9);
            } else if (subconfig == 4) { cell.AddTriangles2(Luts.tiling7_2, config, 2, 5);
            } else if (subconfig == 5) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling7_3, config, 1, 9);
            } else if (subconfig == 6) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling7_3, config, 2, 9);
            } else if (subconfig == 7) {
                if (TestInternal(cell, cas, config, subconfig, Luts.test7[config, 3])) {
                    cell.AddTriangles(Luts.tiling7_4_2, config, 9);
                } else {
                    cell.AddTriangles(Luts.tiling7_4_1, config, 5);
                }
            }

        } else if (cas == 8 ) {
            cell.AddTriangles(Luts.tiling8, config, 2);

        } else if (cas == 9 ) {
            cell.AddTriangles(Luts.tiling9, config, 4);

        } else if (cas == 10 ) {
            if (TestFace(cell, Luts.test10[config, 0])) {
                if (TestFace(cell, Luts.test10[config, 1])) {
                    cell.AddTriangles(Luts.tiling10_1_1_, config, 4);
                } else {
                    //cell.CalculateCenterVertex() // v12 needed
                    cell.AddTriangles(Luts.tiling10_2, config, 8);
                }
            } else {
                if (TestFace(cell, Luts.test10[config, 1])) {
                    //cell.CalculateCenterVertex() // v12 needed
                    cell.AddTriangles(Luts.tiling10_2_, config, 8);
                } else {
                    if (TestInternal(cell, cas, config, subconfig, Luts.test10[config, 2])) {
                        cell.AddTriangles(Luts.tiling10_1_1, config, 4);
                    } else {
                        cell.AddTriangles(Luts.tiling10_1_2, config, 8);
                    }
                }
            }

        } else if (cas == 11 ) {
            cell.AddTriangles(Luts.tiling11, config, 4);

        } else if (cas == 12 ) {
            if (TestFace(cell, Luts.test12[config, 0])) {
                if (TestFace(cell, Luts.test12[config, 1])) {
                    cell.AddTriangles(Luts.tiling12_1_1_, config, 4);
                } else {
                    //cell.CalculateCenterVertex() // v12 needed
                    cell.AddTriangles(Luts.tiling12_2, config, 8);
                }
            } else {
                if (TestFace(cell, Luts.test12[config, 1])) {
                    //cell.CalculateCenterVertex() // v12 needed
                    cell.AddTriangles(Luts.tiling12_2_, config, 8);
                } else {
                    if (TestInternal(cell, cas, config, subconfig, Luts.test12[config, 2])) {
                        cell.AddTriangles(Luts.tiling12_1_1, config, 4);
                    } else {
                        cell.AddTriangles(Luts.tiling12_1_2, config, 8);
                    }
                }
            }

        } else if (cas == 13 ) {
            // Calculate subconfig
            if (TestFace(cell, Luts.test13[config, 0])) subconfig += 1;
            if (TestFace(cell, Luts.test13[config, 1])) subconfig += 2;
            if (TestFace(cell, Luts.test13[config, 2])) subconfig += 4;
            if (TestFace(cell, Luts.test13[config, 3])) subconfig += 8;
            if (TestFace(cell, Luts.test13[config, 4])) subconfig += 16;
            if (TestFace(cell, Luts.test13[config, 5])) subconfig += 32;

            // Map via LUT
            subconfig = Luts.subconfig13[subconfig];

            // Behavior depends on subconfig
            if (subconfig==0) {    cell.AddTriangles(Luts.tiling13_1, config, 4);
            } else if (subconfig==1) {  cell.AddTriangles2(Luts.tiling13_2, config, 0, 6);
            } else if (subconfig==2) {  cell.AddTriangles2(Luts.tiling13_2, config, 1, 6);
            } else if (subconfig==3) {  cell.AddTriangles2(Luts.tiling13_2, config, 2, 6);
            } else if (subconfig==4) {  cell.AddTriangles2(Luts.tiling13_2, config, 3, 6);
            } else if (subconfig==5) {  cell.AddTriangles2(Luts.tiling13_2, config, 4, 6);
            } else if (subconfig==6) {  cell.AddTriangles2(Luts.tiling13_2, config, 5, 6);
            //
            } else if (subconfig==7) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3, config, 0, 10);
            } else if (subconfig==8) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3, config, 1, 10);
            } else if (subconfig==9) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3, config, 2, 10);
            } else if (subconfig==10) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3, config, 3, 10);
            } else if (subconfig==11) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3, config, 4, 10);
            } else if (subconfig==12) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3, config, 5, 10);
            } else if (subconfig==13) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3, config, 6, 10);
            } else if (subconfig==14) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3, config, 7, 10);
            } else if (subconfig==15) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3, config, 8, 10);
            } else if (subconfig==16) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3, config, 9, 10);
            } else if (subconfig==17) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3, config, 10, 10);
            } else if (subconfig==18) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3, config, 11, 10);
            //
            } else if (subconfig==19) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_4, config, 0, 12);
            } else if (subconfig==20) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_4, config, 1, 12);
            } else if (subconfig==21) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_4, config, 2, 12);
            } else if (subconfig==22) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_4, config, 3, 12);
            //
            } else if (subconfig==23) {
                subconfig = 0; // Note: the original source code sets the subconfig, without apparent reason
                if (TestInternal(cell, cas, config, subconfig, Luts.test13[config, 6])) {
                    cell.AddTriangles2(Luts.tiling13_5_1, config, 0, 6);
                } else {
                    cell.AddTriangles2(Luts.tiling13_5_2, config, 0, 10);
                }
            } else if (subconfig==24) {
                subconfig = 1;
                if (TestInternal(cell, cas, config, subconfig, Luts.test13[config, 6])) {
                    cell.AddTriangles2(Luts.tiling13_5_1, config, 1, 6);
                } else {
                    cell.AddTriangles2(Luts.tiling13_5_2, config, 1, 10);
                }
            } else if (subconfig==25) {
                subconfig = 2 ;
                if (TestInternal(cell, cas, config, subconfig, Luts.test13[config, 6])) {
                    cell.AddTriangles2(Luts.tiling13_5_1, config, 2, 6);
                } else {
                    cell.AddTriangles2(Luts.tiling13_5_2, config, 2, 10);
                }
            } else if (subconfig==26) {
                subconfig = 3 ;
                if (TestInternal(cell, cas, config, subconfig, Luts.test13[config, 6])) {
                    cell.AddTriangles2(Luts.tiling13_5_1, config, 3, 6);
                } else {
                    cell.AddTriangles2(Luts.tiling13_5_2, config, 3, 10);
                }
            //
            } else if (subconfig==27) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3_, config, 0, 10);
            } else if (subconfig==28) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3_, config, 1, 10);
            } else if (subconfig==29) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3_, config, 2, 10);
            } else if (subconfig==30) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3_, config, 3, 10);
            } else if (subconfig==31) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3_, config, 4, 10);
            } else if (subconfig==32) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3_, config, 5, 10);
            } else if (subconfig==33) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3_, config,6, 10);
            } else if (subconfig==34) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3_, config, 7, 10);
            } else if (subconfig==35) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3_, config, 8, 10);
            } else if (subconfig==36) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3_, config, 9, 10);
            } else if (subconfig==37) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3_, config, 10, 10);
            } else if (subconfig==38) {
                //cell.CalculateCenterVertex() // v12 needed
                cell.AddTriangles2(Luts.tiling13_3_, config, 11, 10);
            //
            } else if (subconfig==39) {
                cell.AddTriangles2(Luts.tiling13_2_, config, 0, 6);
            } else if (subconfig==40) {
                cell.AddTriangles2(Luts.tiling13_2_, config, 1, 6);
            } else if (subconfig==41) {
                cell.AddTriangles2(Luts.tiling13_2_, config, 2, 6);
            } else if (subconfig==42) {
                cell.AddTriangles2(Luts.tiling13_2_, config, 3, 6);
            } else if (subconfig==43) {
                cell.AddTriangles2(Luts.tiling13_2_, config, 4, 6);
            } else if (subconfig==44) {
                cell.AddTriangles2(Luts.tiling13_2_, config, 5, 6);
            //
            } else if (subconfig==45) {
                cell.AddTriangles(Luts.tiling13_1_, config, 4);
            //
            } else {
                Console.WriteLine("Marching Cubes: Impossible case 13?");
            }

        } else if (cas == 14 ) {
            cell.AddTriangles(Luts.tiling14, config, 4);
        }
    }

    /// <summary>
    /// Return True if the face contains part of the surface.
    /// </summary>
    static bool TestFace(Cell cell, int face)
    {
        // Get face absolute value
        int absFace = face;
        if (face < 0) {
            absFace *= -1;
        }

        // Get values of corners A B C D
        double A=0.0, B=0.0, C=0.0, D=0.0;
        if (absFace == 1) {
            (A, B, C, D) = (cell.v0, cell.v4, cell.v5, cell.v1);
        } else if (absFace == 2) {
            (A, B, C, D) = (cell.v1, cell.v5, cell.v6, cell.v2);
        } else if (absFace == 3) {
            (A, B, C, D) = (cell.v2, cell.v6, cell.v7, cell.v3);
        } else if (absFace == 4) {
            (A, B, C, D) = (cell.v3, cell.v7, cell.v4, cell.v0);
        } else if (absFace == 5) {
            (A, B, C, D) = (cell.v0, cell.v3, cell.v2, cell.v1);
        } else if (absFace == 6) {
            (A, B, C, D) = (cell.v4, cell.v7, cell.v6, cell.v5);
        }

        // Return sign
        double AC_BD = A*C - B*D;
        if (AC_BD > - FLT_EPSILON && AC_BD < FLT_EPSILON) {
            return face >= 0;
        } else {
            return face * A * AC_BD >= 0;  // face && A invert signs
        }
    }

    /// <summary>
    // Return True of the face contains part of the surface.
    /// </summary>
    static bool TestInternal(Cell cell, int cas, int config, int subconfig, int s)
    {
        double t, At=0.0, Bt=0.0, Ct=0.0, Dt=0.0, a, b;
        int test = 0;
        int edge = -1; // reference edge of the triangulation

        // Calculate At Bt Ct Dt a b
        // Select case 4, 10,  7, 12, 13

        if (cas==4 || cas==10) {
            a = ( cell.v4 - cell.v0 ) * ( cell.v6 - cell.v2 ) - ( cell.v7 - cell.v3 ) * ( cell.v5 - cell.v1 );
            b =  cell.v2 * ( cell.v4 - cell.v0 ) + cell.v0 * ( cell.v6 - cell.v2 ) - cell.v1 * ( cell.v7 - cell.v3 ) - cell.v3 * ( cell.v5 - cell.v1 );
            t = - b / (2*a + FLT_EPSILON);
            if (t<0 || t>1) return s>0;

            At = cell.v0 + ( cell.v4 - cell.v0 ) * t;
            Bt = cell.v3 + ( cell.v7 - cell.v3 ) * t;
            Ct = cell.v2 + ( cell.v6 - cell.v2 ) * t;
            Dt = cell.v1 + ( cell.v5 - cell.v1 ) * t;

        } else if (cas==6 || cas==7 || cas==12 || cas==13) {
            // Define edge
            if (cas == 6) { edge = Luts.test6[config,  2];
            } else if (cas == 7) { edge = Luts.test7[config,  4];
            } else if (cas == 12) { edge = Luts.test12[config,  3];
            } else if (cas == 13) { edge = Luts.tiling13_5_1[config,  subconfig,  0];
            }

            if (edge==0) {
                t  = cell.v0 / ( cell.v0 - cell.v1 + FLT_EPSILON );
                At = 0;
                Bt = cell.v3 + ( cell.v2 - cell.v3 ) * t;
                Ct = cell.v7 + ( cell.v6 - cell.v7 ) * t;
                Dt = cell.v4 + ( cell.v5 - cell.v4 ) * t;
            } else if (edge==1) {
                t  = cell.v1 / ( cell.v1 - cell.v2 + FLT_EPSILON );
                At = 0;
                Bt = cell.v0 + ( cell.v3 - cell.v0 ) * t;
                Ct = cell.v4 + ( cell.v7 - cell.v4 ) * t;
                Dt = cell.v5 + ( cell.v6 - cell.v5 ) * t;
            } else if (edge==2) {
                t  = cell.v2 / ( cell.v2 - cell.v3 + FLT_EPSILON );
                At = 0;
                Bt = cell.v1 + ( cell.v0 - cell.v1 ) * t;
                Ct = cell.v5 + ( cell.v4 - cell.v5 ) * t;
                Dt = cell.v6 + ( cell.v7 - cell.v6 ) * t;
            } else if (edge==3) {
                t  = cell.v3 / ( cell.v3 - cell.v0 + FLT_EPSILON );
                At = 0;
                Bt = cell.v2 + ( cell.v1 - cell.v2 ) * t;
                Ct = cell.v6 + ( cell.v5 - cell.v6 ) * t;
                Dt = cell.v7 + ( cell.v4 - cell.v7 ) * t;
            } else if (edge==4) {
                t  = cell.v4 / ( cell.v4 - cell.v5 + FLT_EPSILON );
                At = 0;
                Bt = cell.v7 + ( cell.v6 - cell.v7 ) * t;
                Ct = cell.v3 + ( cell.v2 - cell.v3 ) * t;
                Dt = cell.v0 + ( cell.v1 - cell.v0 ) * t;
            } else if (edge==5) {
                t  = cell.v5 / ( cell.v5 - cell.v6 + FLT_EPSILON );
                At = 0;
                Bt = cell.v4 + ( cell.v7 - cell.v4 ) * t;
                Ct = cell.v0 + ( cell.v3 - cell.v0 ) * t;
                Dt = cell.v1 + ( cell.v2 - cell.v1 ) * t;
            } else if (edge==6) {
                t  = cell.v6 / ( cell.v6 - cell.v7 + FLT_EPSILON );
                At = 0;
                Bt = cell.v5 + ( cell.v4 - cell.v5 ) * t;
                Ct = cell.v1 + ( cell.v0 - cell.v1 ) * t;
                Dt = cell.v2 + ( cell.v3 - cell.v2 ) * t;
            } else if (edge==7) {
                t  = cell.v7 / ( cell.v7 - cell.v4 + FLT_EPSILON );
                At = 0;
                Bt = cell.v6 + ( cell.v5 - cell.v6 ) * t;
                Ct = cell.v2 + ( cell.v1 - cell.v2 ) * t;
                Dt = cell.v3 + ( cell.v0 - cell.v3 ) * t;
            } else if (edge==8) {
                t  = cell.v0 / ( cell.v0 - cell.v4 + FLT_EPSILON );
                At = 0;
                Bt = cell.v3 + ( cell.v7 - cell.v3 ) * t;
                Ct = cell.v2 + ( cell.v6 - cell.v2 ) * t;
                Dt = cell.v1 + ( cell.v5 - cell.v1 ) * t;
            } else if (edge==9) {
                t  = cell.v1 / ( cell.v1 - cell.v5 + FLT_EPSILON );
                At = 0;
                Bt = cell.v0 + ( cell.v4 - cell.v0 ) * t;
                Ct = cell.v3 + ( cell.v7 - cell.v3 ) * t;
                Dt = cell.v2 + ( cell.v6 - cell.v2 ) * t;
            } else if (edge==10) {
                t  = cell.v2 / ( cell.v2 - cell.v6 + FLT_EPSILON );
                At = 0;
                Bt = cell.v1 + ( cell.v5 - cell.v1 ) * t;
                Ct = cell.v0 + ( cell.v4 - cell.v0 ) * t;
                Dt = cell.v3 + ( cell.v7 - cell.v3 ) * t;
            } else if (edge==11) {
                t  = cell.v3 / ( cell.v3 - cell.v7 + FLT_EPSILON );
                At = 0;
                Bt = cell.v2 + ( cell.v6 - cell.v2 ) * t;
                Ct = cell.v1 + ( cell.v5 - cell.v1 ) * t;
                Dt = cell.v0 + ( cell.v4 - cell.v0 ) * t;
            } else {
                Console.WriteLine("Marching Cubes: Invalid edge {0}.",  edge);
            }
        } else {
            Console.WriteLine("Marching Cubes: Invalid ambiguous case {0}.", cas );
        }

        // Process results
        if (At >= 0) test += 1;
        if (Bt >= 0) test += 2;
        if (Ct >= 0) test += 4;
        if (Dt >= 0) test += 8;

        // Determine what to return
        if (test==0) { return s>0;
        } else if (test==1) { return s>0;
        } else if (test==2) { return s>0;
        } else if (test==3) { return s>0;
        } else if (test==4) { return s>0;
        } else if (test==5) {
            if (At * Ct - Bt * Dt <  FLT_EPSILON) return s>0;
        } else if (test==6) { return s>0;
        } else if (test==7) { return s<0;
        } else if (test==8) { return s>0;
        } else if (test==9) { return s>0;
        } else if (test==10) {
            if (At * Ct - Bt * Dt >= FLT_EPSILON) return s>0;
        } else if (test==11) { return s<0;
        } else if (test==12) { return s>0;
        } else if (test==13) { return s<0;
        } else if (test==14) { return s<0;
        } else if (test==15) { return s<0;
        }
        return s<0;
    }
}
