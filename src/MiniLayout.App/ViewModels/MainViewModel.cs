using System.Collections.ObjectModel;
using MiniLayout.Core.Models;
using MiniLayout.Core.Scripting;

namespace MiniLayout.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    public Scene Scene { get; } = new();
    public ObservableCollection<LayoutComponent> Components { get; } = [];

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private int _collidingCount;
    public int CollidingCount
    {
        get => _collidingCount;
        set => SetProperty(ref _collidingCount, value);
    }

    public int TotalComponents => Components.Count;

    public MainViewModel()
    {
        ResetScene();
    }

    public void ResetScene()
    {
        Scene.Clear();
        // Initial configuration: Components 1 and 2 collide, 3 is clear
        Scene.AddComponent(1, "Component A", -1.0f, 0.0f, 0.0f, 3.0f, 2.0f, 1.0f);
        Scene.AddComponent(2, "Component B", 0.5f, 0.5f, 0.0f, 2.5f, 2.0f, 1.0f);
        Scene.AddComponent(3, "Component C", 5.0f, 2.0f, 0.0f, 2.0f, 2.0f, 1.0f);

        EvaluateAndRefresh("Scene reset to default layout.");
    }

    public void AddComponent()
    {
        int newId = Scene.ComponentCount + 1;
        float offset = (newId - 1) * 1.5f;
        Scene.AddComponent(newId, $"Component {newId}", -3.0f + offset, 2.0f, 0.0f, 1.8f, 1.8f, 1.0f);
        EvaluateAndRefresh($"Added Component {newId}.");
    }

    public void RunPythonScript()
    {
        try
        {
            const string script = """
                # Dynamic layout adjustment script
                for c in scene.Components:
                    if c.Id == 2:
                        # Move component B away to clear collision
                        c.SetPosition(4.5, -2.0, 0.0)
                    elif c.Id == 3:
                        # Move component C near A to introduce a new collision
                        c.SetPosition(-0.5, 0.2, 0.0)

                scene.EvaluateCollisions()
                """;

            ScriptRunner.Execute(script, Scene);
            EvaluateAndRefresh("Python script executed successfully.");
        }
        catch (Exception ex)
        {
            StatusText = $"Python script error: {ex.Message}";
        }
    }

    public void EvaluateAndRefresh(string message)
    {
        CollidingCount = Scene.EvaluateCollisions();
        Components.Clear();
        foreach (var c in Scene.Components)
        {
            Components.Add(c);
        }

        OnPropertyChanged(nameof(TotalComponents));
        OnPropertyChanged(nameof(CollidingCount));
        StatusText = $"{message} Total: {TotalComponents}, Colliding: {CollidingCount}";
    }
}
