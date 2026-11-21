using MiniLayout.Core.Models;
using MiniLayout.Core.Scripting;
using Xunit;

namespace MiniLayout.Tests;

public class ScriptingTests
{
    [Fact]
    public void PythonEnvironment_ShouldDiscoverAndInitialize()
    {
        string? dll = PythonEnvironment.DiscoverPythonDll();
        Assert.NotNull(dll);
        Assert.True(File.Exists(dll));

        PythonEnvironment.EnsureInitialized();
        Assert.NotNull(PythonEnvironment.ResolvedPythonDll);
    }

    [Fact]
    public void PythonScript_ShouldModifySceneAndEvaluateCollisions()
    {
        var scene = new Scene();
        scene.AddComponent(1, "Initial", 0.0f, 0.0f, 0.0f, 2.0f, 2.0f, 2.0f);

        const string script = """
            # Add a second box overlapping the first and evaluate
            scene.AddComponent(2, "FromPython", 1.0, 0.0, 0.0, 2.0, 2.0, 2.0)
            scene.EvaluateCollisions()
            """;

        ScriptRunner.Execute(script, scene);

        Assert.Equal(2, scene.ComponentCount);
        Assert.True(scene.Components[0].IsColliding);
        Assert.True(scene.Components[1].IsColliding);
    }
}
