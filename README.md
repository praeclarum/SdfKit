# SDF Kit

Tools for manipulating signed distance functions.

Main features include:

* `Sdfs` and `SdfExprs` library of combinable primitive shapes and modifiers.
* Multi-threaded batched sampling of the `Sdf` for the perf perf perf.
* A bounded `Volume` class for creating voxels from your `Sdf`.
* `MarchingCubes` implementation to export your `Sdf` as a solid mesh.
* `Raytracer` to create some sweet sweet 90s CGI of your `Sdf`.

## Creating SDFs

There are four ways to provide SDF data:

1. **Provide a full batching implementation** by writing a function that processes multiple points at once. This is the most customizable solution but is also the most hassle.

    ```csharp
    Sdf sphere = (ps, ds) => {
        int n = ps.Length;
        var p = ps.Span;
        var d = ds.Span;
        for (var i = 0; i < n; ++i)
        {
            d[i] = p[i].Length() - radius;
        }
    };
    ```

2. **Provide the SDF yourself** using `Sdfs.Solid`.

    ```csharp
    Sdf sphere = Sdfs.Solid(p => p.Length() - 1.0);
    ```

3. **Use some of the built-in SDFs** that are in the static class `Sdfs`.

    ```csharp
    Sdf sphere = Sdfs.Sphere(1.0);
    ```

4. **Use SDF Expressions to build the SDF** using the members of `SdfExprs`. This method makes it easy to build varied and efficient SDFs using a fluent syntax.

    ```csharp
    Sdf spheres =
        SdfExprs
        .Sphere(1.0)
        .RepeatXY(2.0)
        .ToSdf();
    ```

