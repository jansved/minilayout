using MiniLayout.Core.Models;
using Python.Runtime;

namespace MiniLayout.Core.Scripting;

/// <summary>
/// Executes Python scripts manipulating a Scene instance with thread-safe GIL management.
/// </summary>
public sealed class ScriptRunner
{
    private static readonly object InitLock = new();
    private static bool _initialized;

    public static void Initialize()
    {
        lock (InitLock)
        {
            if (!_initialized)
            {
                PythonEnvironment.EnsureInitialized();
                PythonEngine.Initialize();
                PythonEngine.BeginAllowThreads();
                _initialized = true;
            }
        }
    }

    public static void Execute(string scriptCode, Scene scene)
    {
        Initialize();

        using (Py.GIL())
        {
            using var scope = Py.CreateScope();
            scope.Set("scene", scene);
            scope.Exec(scriptCode);
        }
    }

    public static void Shutdown()
    {
        lock (InitLock)
        {
            if (_initialized)
            {
                PythonEngine.Shutdown();
                _initialized = false;
            }
        }
    }
}
