namespace SdfKit;

/// <summary>
/// A 3D volume bounded by an axis-aligned box with corners at Min and Max.
/// </summary>
public interface IBoundedVolume
{
    Vector3 Min { get; }
    Vector3 Max { get; }
    Vector3 Size { get; }
    Vector3 Center { get; }
    float Radius { get; }
}

