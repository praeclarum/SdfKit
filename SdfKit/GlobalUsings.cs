
global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Linq;
global using System.Numerics;
global using System.Threading;
global using System.Threading.Tasks;

global using SdfInput = System.Numerics.Vector3;
global using SdfOutput = System.Numerics.Vector4;

// The color components of SdfOutput.
global using SdfColor = System.Numerics.Vector3;

global using SdfExpr = System.Linq.Expressions.Expression<SdfKit.SdfFunc>;
global using SdfDistExpr = System.Linq.Expressions.Expression<SdfKit.SdfDistFunc>;

// The index of the sdf when repeating.
global using SdfIndex = System.Numerics.Vector3;
