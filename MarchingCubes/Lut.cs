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

public class Lut {
    public readonly int L0;
    public readonly int L1;
    public readonly int L2;

    sbyte[] values;

    public int Get1(int i0) => values[i0];
    public int Get2(int i0, int i1) => values[i0*L1 + i1];
    public int Get3(int i0, int i1, int i2) => values[i0*L1*L2 + i1*L2 + i2];
}

public class LutProvider {
    public Lut EDGESRELX;
    public Lut EDGESRELY;
    public Lut EDGESRELZ;
    public Lut CASES;
}
