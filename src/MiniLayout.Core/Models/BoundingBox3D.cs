namespace MiniLayout.Core.Models;

/// <summary>
/// Immutable 3D bounding box value representation.
/// </summary>
public readonly record struct BoundingBox3D(
    float X,
    float Y,
    float Z,
    float Width,
    float Height,
    float Depth)
{
    public float MinX => X - (Width * 0.5f);
    public float MaxX => X + (Width * 0.5f);
    public float MinY => Y - (Height * 0.5f);
    public float MaxY => Y + (Height * 0.5f);
    public float MinZ => Z - (Depth * 0.5f);
    public float MaxZ => Z + (Depth * 0.5f);
}
