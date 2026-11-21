# MiniLayout Specification (SPEC.md)

## 1. Overview & Goals

`MiniLayout` is a compact, cohesive reference desktop application developed for macOS (Apple Silicon M4 / ARM64) demonstrating clean cross-language polyglot architecture:
- **Performance / Native Layer**: C++20 shared library implementing 3D Axis-Aligned Bounding Box (AABB) collision detection, utilizing `glm` for geometry and `spdlog` for structured logging via `vcpkg` manifest mode.
- **Managed Core & Scripting**: .NET 10 (`net10.0`) class library leveraging modern `[LibraryImport]` source-generated P/Invoke, a clean `Scene` domain model, and embedded Python scripting via `pythonnet` with automatic macOS and Windows `libpython` discovery.
- **Presentation & Testing**: Avalonia UI 11 (`net10.0` MVVM) featuring a top-down 2D canvas dynamically rendering components (Green = clear, Red = colliding) with interactive Python script triggers, accompanied by headless `xUnit` tests.
- **Supply Chain & Build**: Automated build script (`scripts/build_and_sbom.sh`) compiling native C++ and .NET, running test suites, and generating CycloneDX SBOM (`sbom.cdx.json`).

All code, comments, and documentation are strictly written in English.

---

## 2. Target Environment & Stack

| Layer | Technology | Details |
|---|---|---|
| **Host OS & Architecture** | macOS (Apple Silicon / ARM64, Darwin) | Tested on Apple Silicon M-series (M1–M4); cross-platform adaptable for Linux and Windows |
| **Native Compiler & Build** | Apple Clang / Clang++ (C++20), CMake 3.25+, Ninja | Shared library output (`libnative_engine.dylib`) |
| **C++ Package Manager** | vcpkg (Manifest Mode) | Dependencies: `glm` (math/vectors), `spdlog` (structured engine logging) |
| **Managed Runtime** | .NET 10 SDK (`net10.0`), C# 13 | High-performance P/Invoke (`[LibraryImport]`), records, and nullable reference types |
| **Desktop UI** | Avalonia UI (v11.x, MVVM) | Top-down 2D component canvas, reactive/observable UI state |
| **Embedded Scripting** | Python.NET (`pythonnet` v3.0.x) + Python 3.10–3.14 | Automatic `libpython` dynamic library resolution across macOS Homebrew/system paths and Windows |
| **Testing** | xUnit + .NET Test SDK | Headless automated testing for native math and Python scripting without display requirements |
| **Supply Chain / SBOM** | CycloneDX (`dotnet-CycloneDX`) + vcpkg SBOM | Automated JSON SBOM generation for both NuGet packages and vcpkg dependencies |

---

## 3. Repository Layout

```
minilayout-pvt/
├── SPEC.md                           # Formal system specification (this document)
├── PLAN.md                           # Incremental staged implementation plan
├── README.md                         # Architecture overview, ABI design, and build instructions
├── scripts/
│   └── build_and_sbom.sh             # Unified build, test, and CycloneDX SBOM script
├── src/
│   ├── native-engine/                # Native C++20 shared library
│   │   ├── vcpkg.json                # Manifest mode defining glm & spdlog
│   │   ├── CMakeLists.txt            # CMake configuration with vcpkg toolchain
│   │   ├── include/
│   │   │   └── engine.h              # Pure C ABI header (`extern "C"`)
│   │   └── src/
│   │       └── engine.cpp            # 3D AABB collision detection & logging implementation
│   ├── MiniLayout.Core/              # .NET 10 class library
│   │   ├── MiniLayout.Core.csproj
│   │   ├── Native/
│   │   │   ├── NativeTypes.cs        # Struct layouts matching C ABI (NativeBox, CollisionResult)
│   │   │   └── NativeMethods.cs      # Modern [LibraryImport] P/Invoke definitions & DllImportResolver
│   │   ├── Models/
│   │   │   ├── BoundingBox3D.cs      # Managed 3D bounding box value type
│   │   │   ├── LayoutComponent.cs    # Layout entity (Id, Name, Box, CollisionState)
│   │   │   └── Scene.cs              # Scene domain model orchestrating component collection & collision passes
│   │   └── Scripting/
│   │       ├── PythonEnvironment.cs  # Dynamic libpython locator & runtime initialization
│   │       └── ScriptRunner.cs       # Python script executor manipulating Scene instances
│   └── MiniLayout.App/               # Avalonia UI desktop application
│       ├── MiniLayout.App.csproj
│       ├── Program.cs                # App entrypoint with Avalonia initialization
│       ├── App.axaml                 # Application styles and resources
│       ├── App.axaml.cs              # Application lifecycle
│       ├── ViewModels/
│       │   ├── ViewModelBase.cs      # INotifyPropertyChanged base implementation
│       │   └── MainViewModel.cs      # Main window VM managing Scene, component list, and script trigger
│       └── Views/
│           ├── MainWindow.axaml      # Main view: top-down 2D canvas + toolbar
│           └── MainWindow.axaml.cs   # Code-behind with canvas rendering logic
└── tests/
    └── MiniLayout.Tests/             # Headless verification test suite
        ├── MiniLayout.Tests.csproj
        ├── CollisionTests.cs         # Unit tests validating native C++ AABB collision detection
        └── ScriptingTests.cs         # Unit tests validating embedded Python scene manipulation
```

---

## 4. Functional Requirements

### 4.1 Native C++20 Engine (`src/native-engine`)
1. **ABI Contract**:
   - Expose an unmanaged `extern "C"` C-compatible ABI with fixed-size types (`float`, `int32_t`).
   - Define `NativeBox`:
     ```c
     typedef struct {
         int32_t id;
         float x, y, z;          // Center coordinates
         float width, height, depth; // Full dimensions along X, Y, Z
     } NativeBox;
     ```
   - Define `engine_check_pair_collision(const NativeBox* a, const NativeBox* b)` returning `int32_t` (1 for collision, 0 for clear).
   - Define `engine_evaluate_scene(const NativeBox* boxes, int32_t count, int32_t* out_collision_flags)` evaluating all-pairs collision and populating `out_collision_flags[i]` with 1 if component `i` intersects any other component, else 0.
2. **Mathematical Correctness**:
   - Two 3D AABBs collide if and only if they overlap along all three orthogonal axes:
     $$\Delta x \le \frac{A.w + B.w}{2} \land \Delta y \le \frac{A.h + B.h}{2} \land \Delta z \le \frac{A.d + B.d}{2}$$
3. **Structured Logging**:
   - Utilize `spdlog` to log initialization and collision events with concise output formatting.

### 4.2 .NET 10 Core & Python Scripting (`src/MiniLayout.Core`)
1. **P/Invoke Architecture**:
   - Use source-generated `[LibraryImport("native_engine")]` with `[StructLayout(LayoutKind.Sequential)]`.
   - Implement `NativeLibrary.SetDllImportResolver` to automatically locate `libnative_engine.dylib` on macOS, `native_engine.dll` on Windows, or `libnative_engine.so` on Linux from application runtime paths.
2. **Domain Model**:
   - `Scene`: Manages collection of `LayoutComponent`. Provides `EvaluateCollisions()` which calls into the native engine and updates component states.
3. **Python Scripting Engine**:
   - `PythonEnvironment`: Detects the installed `libpython` (supporting macOS Homebrew paths like `/opt/homebrew/lib/libpython3.*.dylib`, Framework paths, and Windows registry/path) and calls `Runtime.PythonDLL = ...` before `PythonEngine.Initialize()`.
   - `ScriptRunner`: Executes Python script strings/files passing the `Scene` instance to the Python scope, allowing scripts to query, add, modify, or translate layout components dynamically.

### 4.3 Avalonia UI Desktop Application (`src/MiniLayout.App`)
1. **Canvas Visualization**:
   - Provide a 2D top-down projection $(X, Y)$ of 3D components on an Avalonia canvas.
   - Render each component as a rectangle:
     - **Green (`#2ecc71`)**: Clear / non-colliding.
     - **Red (`#e74c3c`)**: In collision with another component.
   - Display component ID and coordinates as text overlay.
2. **Interactive Controls**:
   - "Run Python Layout Script" button: Executes a predefined Python script that moves components (e.g., dynamically adjusting coordinates to trigger and resolve collisions).
   - "Add Component" and "Reset Scene" convenience actions.
   - Status bar displaying total component count, active collisions, and engine status.

### 4.4 Headless Unit Tests (`tests/MiniLayout.Tests`)
1. **Native Engine Tests (`CollisionTests.cs`)**:
   - Test non-colliding distant boxes.
   - Test overlapping boxes (full penetration and partial corner/edge overlap).
   - Test touching boundary edges/faces.
   - Verify scene-level batch collision resolution matches expected flags.
2. **Python Scripting Tests (`ScriptingTests.cs`)**:
   - Verify `PythonEnvironment` initializes successfully.
   - Execute a Python script that injects new boxes into `Scene` and validates state mutations.
   - Confirm headless execution without initializing Avalonia UI or window handles.

### 4.5 Build, Verification & SBOM Script (`scripts/build_and_sbom.sh`)
1. **Unified Execution**:
   - Validate environment prerequisites: CMake, Ninja, vcpkg (`VCPKG_ROOT`), .NET 10 SDK, `dotnet-CycloneDX`.
   - Configure and build `src/native-engine` using CMake and Ninja with vcpkg manifest mode.
   - Deploy compiled native shared library to the .NET output directory.
   - Build .NET solution (`dotnet build`).
   - Execute test suite (`dotnet test`).
   - Generate CycloneDX SBOM for .NET NuGet packages (`dotnet-CycloneDX`) and vcpkg dependencies, outputting `sbom.cdx.json`.

---

## 5. Constraints & Non-Goals

1. **Simplicity & Cohesion**:
   - Avoid heavy 3D rendering frameworks (OpenTK, Silk.NET, Vulkan/Metal wrappers); top-down 2D canvas in Avalonia is sufficient to verify and interact with 3D math and layout states.
   - Keep P/Invoke minimal, clean, and directly mapped without complex marshaling layers.
2. **Language & Documentation**:
   - All source code, XML documentation, code comments, commit messages, and documentation files must be written in English.
3. **Execution Safety**:
   - The embedded Python runtime must be guarded with thread-state management (`Py.GIL()`) to prevent concurrency deadlocks or memory corruption.
4. **Phasing & Approvals**:
   - Implementation follows strictly the 4 sequential stages defined in `PLAN.md` after approval.
