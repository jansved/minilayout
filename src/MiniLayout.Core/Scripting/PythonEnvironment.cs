using System.Diagnostics;
using System.Runtime.InteropServices;
using Python.Runtime;

namespace MiniLayout.Core.Scripting;

/// <summary>
/// Handles automatic discovery of libpython across platforms and initializes the Python.NET runtime.
/// </summary>
public static class PythonEnvironment
{
    private static readonly object Lock = new();
    private static bool _initialized;
    private static string? _resolvedPythonDll;

    public static string? ResolvedPythonDll => _resolvedPythonDll;

    public static void EnsureInitialized()
    {
        lock (Lock)
        {
            if (_initialized)
            {
                return;
            }

            string? pythonDll = DiscoverPythonDll();
            if (!string.IsNullOrEmpty(pythonDll) && File.Exists(pythonDll))
            {
                _resolvedPythonDll = pythonDll;
                Runtime.PythonDLL = pythonDll;
                Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll);
            }

            _initialized = true;
        }
    }

    public static string? DiscoverPythonDll()
    {
        // 1. Explicit environment variable override
        string? envVar = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL");
        if (!string.IsNullOrEmpty(envVar) && File.Exists(envVar))
        {
            return envVar;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return DiscoverMacOsPython();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return DiscoverWindowsPython();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return DiscoverLinuxPython();
        }

        return null;
    }

    private static string? DiscoverMacOsPython()
    {
        // PythonNet 3.0.x supports Python 3.7 through 3.13
        string[] versions = ["3.13", "3.12", "3.11", "3.10", "3.9", "3.8", "3.7"];

        foreach (string ver in versions)
        {
            string[] candidates =
            [
                $"/opt/homebrew/opt/python@{ver}/Frameworks/Python.framework/Versions/{ver}/Python",
                $"/opt/homebrew/opt/python@{ver}/Frameworks/Python.framework/Versions/{ver}/lib/libpython{ver}.dylib",
                $"/opt/homebrew/lib/libpython{ver}.dylib",
                $"/usr/local/opt/python@{ver}/Frameworks/Python.framework/Versions/{ver}/Python",
                $"/Library/Frameworks/Python.framework/Versions/{ver}/Python"
            ];

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        // Xcode Command Line Tools Python fallback
        string xcodePython = "/Applications/Xcode.app/Contents/Developer/Library/Frameworks/Python3.framework/Versions/Current/Python3";
        if (File.Exists(xcodePython))
        {
            return xcodePython;
        }

        // Dynamic query via python3 CLI
        return QueryPythonViaCli("python3");
    }

    private static string? DiscoverWindowsPython()
    {
        string[] versions = ["313", "312", "311", "310", "39", "38", "37"];
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        foreach (string ver in versions)
        {
            string candidate = Path.Combine(localAppData, $"Programs\\Python\\Python{ver}\\python3.dll");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return QueryPythonViaCli("python");
    }

    private static string? DiscoverLinuxPython()
    {
        string[] versions = ["3.13", "3.12", "3.11", "3.10", "3.9", "3.8", "3.7"];
        foreach (string ver in versions)
        {
            string[] candidates =
            [
                $"/usr/lib/aarch64-linux-gnu/libpython{ver}.so",
                $"/usr/lib/x86_64-linux-gnu/libpython{ver}.so",
                $"/usr/lib/libpython{ver}.so",
                $"/usr/local/lib/libpython{ver}.so"
            ];

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return QueryPythonViaCli("python3");
    }

    private static string? QueryPythonViaCli(string pythonCommand)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonCommand,
                Arguments = "-c \"import sysconfig, os; print(os.path.join(sysconfig.get_config_var('LIBDIR') or '', sysconfig.get_config_var('LDLIBRARY') or ''))\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return null;
            }

            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(3000);

            if (process.ExitCode == 0 && File.Exists(output))
            {
                return output;
            }
        }
        catch
        {
            // Ignore process launch failures
        }

        return null;
    }
}
