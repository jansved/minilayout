namespace MiniLayout.Core.Models;

/// <summary>
/// A layout entity with identifier, name, 3D bounds, and active collision state.
/// </summary>
public sealed class LayoutComponent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BoundingBox3D Bounds { get; set; }
    public bool IsColliding { get; set; }

    public LayoutComponent(int id, string name, BoundingBox3D bounds)
    {
        Id = id;
        Name = name;
        Bounds = bounds;
        IsColliding = false;
    }

    public void Move(float deltaX, float deltaY, float deltaZ)
    {
        Bounds = Bounds with
        {
            X = Bounds.X + deltaX,
            Y = Bounds.Y + deltaY,
            Z = Bounds.Z + deltaZ
        };
    }

    public void SetPosition(float x, float y, float z)
    {
        Bounds = Bounds with { X = x, Y = y, Z = z };
    }
}
