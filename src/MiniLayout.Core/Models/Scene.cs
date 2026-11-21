using MiniLayout.Core.Native;

namespace MiniLayout.Core.Models;

/// <summary>
/// Domain model managing layout components and orchestrating collision passes via native engine.
/// </summary>
public sealed class Scene
{
    private readonly List<LayoutComponent> _components = [];
    private readonly object _lock = new();

    public IReadOnlyList<LayoutComponent> Components
    {
        get
        {
            lock (_lock)
            {
                return _components.ToArray();
            }
        }
    }

    public int ComponentCount
    {
        get
        {
            lock (_lock)
            {
                return _components.Count;
            }
        }
    }

    public LayoutComponent AddComponent(int id, string name, float x, float y, float z, float width, float height, float depth)
    {
        lock (_lock)
        {
            var component = new LayoutComponent(id, name, new BoundingBox3D(x, y, z, width, height, depth));
            _components.Add(component);
            return component;
        }
    }

    public bool RemoveComponent(int id)
    {
        lock (_lock)
        {
            int index = _components.FindIndex(c => c.Id == id);
            if (index >= 0)
            {
                _components.RemoveAt(index);
                return true;
            }
            return false;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _components.Clear();
        }
    }

    public int EvaluateCollisions()
    {
        lock (_lock)
        {
            int count = _components.Count;
            if (count == 0)
            {
                return 0;
            }

            var nativeBoxes = new NativeBox[count];
            for (int i = 0; i < count; i++)
            {
                var c = _components[i];
                nativeBoxes[i] = new NativeBox(
                    c.Id,
                    c.Bounds.X,
                    c.Bounds.Y,
                    c.Bounds.Z,
                    c.Bounds.Width,
                    c.Bounds.Height,
                    c.Bounds.Depth);
            }

            var flags = new int[count];
            int collidingCount = NativeMethods.EvaluateScene(nativeBoxes, count, flags);

            for (int i = 0; i < count; i++)
            {
                _components[i].IsColliding = flags[i] != 0;
            }

            return collidingCount;
        }
    }
}
