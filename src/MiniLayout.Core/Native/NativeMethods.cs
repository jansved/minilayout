using System.Reflection;
using System.Runtime.InteropServices;

namespace MiniLayout.Core.Native;

/// <summary>
/// Source-generated P/Invoke bindings to the native C++ engine.
/// </summary>
public static partial class NativeMethods
{
    public const string LibraryName = "native_engine";

    static NativeMethods()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ResolveDll);
    }

    private static IntPtr ResolveDll(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != LibraryName)
        {
            return IntPtr.Zero;
        }

        string extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dll"
                         : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".dylib"
                         : ".so";

        string prefix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "" : "lib";
        string fileName = $"{prefix}{LibraryName}{extension}";

        // 1. Check in AppContext.BaseDirectory
        string directPath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(directPath) && NativeLibrary.TryLoad(directPath, out IntPtr handle))
        {
            return handle;
        }

        // 2. Search common development and build directories
        string[] searchDirectories =
        [
            Path.Combine(AppContext.BaseDirectory, "../../../../src/native-engine/build"),
            Path.Combine(AppContext.BaseDirectory, "../../../native-engine/build"),
            Path.Combine(AppContext.BaseDirectory, "../../native-engine/build"),
            Path.Combine(AppContext.BaseDirectory, "../native-engine/build")
        ];

        foreach (string dir in searchDirectories)
        {
            string candidate = Path.GetFullPath(Path.Combine(dir, fileName));
            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out handle))
            {
                return handle;
            }
        }

        // 3. Fallback to standard library resolver
        if (NativeLibrary.TryLoad(fileName, assembly, searchPath, out handle))
        {
            return handle;
        }

        return IntPtr.Zero;
    }

    [LibraryImport(LibraryName, EntryPoint = "engine_check_pair_collision")]
    public static partial int CheckPairCollision(in NativeBox a, in NativeBox b);

    [LibraryImport(LibraryName, EntryPoint = "engine_evaluate_scene")]
    public static partial int EvaluateScene(
        [In] NativeBox[] boxes,
        int count,
        [Out] int[] outCollisionFlags);
}
