using System.Runtime.InteropServices;

namespace MiniLayout.Core.Native;

/// <summary>
/// Blittable 3D bounding box representation matching the native C ABI.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeBox
{
    public int Id;
    public float X;
    public float Y;
    public float Z;
    public float Width;
    public float Height;
    public float Depth;

    public NativeBox(int id, float x, float y, float z, float width, float height, float depth)
    {
        Id = id;
        X = x;
        Y = y;
        Z = z;
        Width = width;
        Height = height;
        Depth = depth;
    }
}
