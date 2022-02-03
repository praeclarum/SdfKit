namespace SdfKit;

public interface IVolume
{
    Vector3 Min { get; }
    Vector3 Max { get; }
    Vector3 Center { get; }
    Vector3 Size { get; }
    float Radius { get; }
}

