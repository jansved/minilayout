using MiniLayout.Core.Models;
using MiniLayout.Core.Native;
using Xunit;

namespace MiniLayout.Tests;

public class CollisionTests
{
    [Fact]
    public void SeparatedBoxes_ShouldNotCollide()
    {
        var boxA = new NativeBox(1, 0.0f, 0.0f, 0.0f, 2.0f, 2.0f, 2.0f);
        var boxB = new NativeBox(2, 10.0f, 0.0f, 0.0f, 2.0f, 2.0f, 2.0f);

        int result = NativeMethods.CheckPairCollision(in boxA, in boxB);

        Assert.Equal(0, result);
    }

    [Fact]
    public void OverlappingBoxes_ShouldCollide()
    {
        var boxA = new NativeBox(1, 0.0f, 0.0f, 0.0f, 2.0f, 2.0f, 2.0f);
        var boxB = new NativeBox(2, 1.0f, 0.0f, 0.0f, 2.0f, 2.0f, 2.0f);

        int result = NativeMethods.CheckPairCollision(in boxA, in boxB);

        Assert.Equal(1, result);
    }

    [Fact]
    public void TouchingFaces_ShouldCollide()
    {
        // Centers at 0.0 and 2.0 with widths 2.0 and 2.0: boundaries touch at x = 1.0
        var boxA = new NativeBox(1, 0.0f, 0.0f, 0.0f, 2.0f, 2.0f, 2.0f);
        var boxB = new NativeBox(2, 2.0f, 0.0f, 0.0f, 2.0f, 2.0f, 2.0f);

        int result = NativeMethods.CheckPairCollision(in boxA, in boxB);

        Assert.Equal(1, result);
    }

    [Fact]
    public void SceneEvaluate_ShouldFlagCollidingComponents()
    {
        var scene = new Scene();
        // Box 1 and Box 2 overlap
        scene.AddComponent(1, "Box1", 0.0f, 0.0f, 0.0f, 2.0f, 2.0f, 2.0f);
        scene.AddComponent(2, "Box2", 1.0f, 0.0f, 0.0f, 2.0f, 2.0f, 2.0f);
        // Box 3 is isolated
        scene.AddComponent(3, "Box3", 20.0f, 0.0f, 0.0f, 2.0f, 2.0f, 2.0f);

        int collidingCount = scene.EvaluateCollisions();

        Assert.Equal(2, collidingCount);
        Assert.True(scene.Components[0].IsColliding);
        Assert.True(scene.Components[1].IsColliding);
        Assert.False(scene.Components[2].IsColliding);
    }
}
