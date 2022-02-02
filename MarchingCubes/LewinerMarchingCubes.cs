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

public class LewinerMarchingCubes : IVolumeMesher
{
    readonly LutProvider luts;

    public LewinerMarchingCubes(LutProvider luts)
    {
        this.luts = luts;
    }

    public Mesh CreateMesh(float[,,] volume, float isovalue, int st)
    {
        int nx = volume.GetLength(0);
        int ny = volume.GetLength(1);
        int nz = volume.GetLength(2);

        var cell = new Cell(luts, nx, ny, nz);

        var nx_bound = nx - 2*st;
        var ny_bound = ny - 2*st;
        var nz_bound = nz - 2*st;

        int z = -st;
        while (z < nz_bound)
        {
            z += st;
            var z_st = z + st;
            cell.NewZValue ();
            int y = -st;
            while (y < ny_bound)
            {
                y += st;
                var y_st = y + st;
                int x = -st;
                while (x < nx_bound)
                {
                    x += st;
                    var x_st = x + st;
                    cell.SetCube(isovalue, x, y, z, st,
                        volume[x, y, z   ], volume[x_st, y, z   ], volume[x_st, y_st, z   ], volume[x, y_st, z   ],
                        volume[x, y, z_st], volume[x_st, y, z_st], volume[x_st, y_st, z_st], volume[x, y_st, z_st]);
                    var cas = luts.CASES.Get2(cell.Index, 0);
                    if (cas > 0) {
                        var config = luts.CASES.Get2(cell.Index, 1);
                        TheBigSwitch(cell, cas, config);
                    }
                }
            }
        }

        return new Mesh (cell.Vertices, cell.Normals, cell.Faces);
    }

    void TheBigSwitch(Cell cell, int cas, int config)
    {
        throw new NotImplementedException();
    }
}
