# MiniLayout Implementation Plan (PLAN.md)

This plan outlines the 4 implementation stages for `MiniLayout` following the approved [SPEC.md](file:///Users/jansve/minilayout-pvt/SPEC.md). Each stage is self-contained, incremental, and directly verifiable.

---

## Stage 1: Native C++20 Engine (`src/native-engine`)

### Goal
Implement a minimal C++20 shared library (`libnative_engine.dylib`) exposing a pure C ABI for 3D AABB collision detection, using `glm` for math and `spdlog` for structured logging via `vcpkg` manifest mode.

### Files to Create
- `src/native-engine/vcpkg.json`: Manifest declaring `glm` and `spdlog` dependencies.
- `src/native-engine/CMakeLists.txt`: CMake build configuration targeting C++20 shared library with visibility control and vcpkg toolchain.
- `src/native-engine/include/engine.h`: Pure C ABI header declaring `NativeBox` struct and exported collision functions (`extern "C"`).
- `src/native-engine/src/engine.cpp`: Implementation of 3D AABB overlap logic with GLM and structured collision logging via spdlog.

### Implementation Details & Interfaces
1. **ABI Definition (`engine.h`)**:
   ```c
   #pragma once
   #include <stdint.h>

   #if defined(_WIN32)
       #define ENGINE_API __declspec(dllexport)
   #else
       #define ENGINE_API __attribute__((visibility("default")))
   #endif

   #ifdef __cplusplus
   extern "C" {
   #endif

   typedef struct {
       int32_t id;
       float x, y, z;               // Center coordinates
       float width, height, depth;  // Dimensions along X, Y, Z
   } NativeBox;

   ENGINE_API int32_t engine_check_pair_collision(const NativeBox* a, const NativeBox* b);
   ENGINE_API int32_t engine_evaluate_scene(const NativeBox* boxes, int32_t count, int32_t* out_collision_flags);

   #ifdef __cplusplus
   }
   #endif
   ```
2. **Logic (`engine.cpp`)**:
   - Pairwise overlap: tests whether the absolute distance between centers on X, Y, and Z is strictly less than or equal to the sum of half-extents.
   - Scene evaluation: loops over all pairs $(i, j)$ and marks flags `out_collision_flags[i] = 1` and `out_collision_flags[j] = 1` upon intersection; logs evaluation metrics with `spdlog`.

### Verification Criteria
- CMake configures and builds cleanly using Ninja and vcpkg:
  ```bash
  cmake -B src/native-engine/build -S src/native-engine -G Ninja -DCMAKE_TOOLCHAIN_FILE="$VCPKG_ROOT/scripts/buildsystems/vcpkg.cmake"
  cmake --build src/native-engine/build
  ```
- Output library `libnative_engine.dylib` exists.
- Exported symbols verified:
  ```bash
  nm -gU src/native-engine/build/libnative_engine.dylib | grep engine_
  ```

---

## Stage 2: .NET 10 Core & Python Scripting (`src/MiniLayout.Core`)

### Goal
Implement a single `net10.0` class library providing source-generated P/Invoke bindings to the native engine, a clean `Scene` domain model, and embedded Python scripting via `pythonnet` with automated `libpython` dynamic library discovery for macOS and Windows.

### Files to Create
- `MiniLayout.sln`: Solution file tying managed projects together.
- `src/MiniLayout.Core/MiniLayout.Core.csproj`: .NET 10 project with `pythonnet` NuGet dependency and unsafe code enabled.
- `src/MiniLayout.Core/Native/NativeTypes.cs`: Blittable `NativeBox` struct matching the C ABI.
- `src/MiniLayout.Core/Native/NativeMethods.cs`: Modern `[LibraryImport]` declarations and dynamic `NativeLibrary.SetDllImportResolver` locating `libnative_engine.dylib`.
- `src/MiniLayout.Core/Models/BoundingBox3D.cs`: Value record representing 3D coordinates and dimensions.
- `src/MiniLayout.Core/Models/LayoutComponent.cs`: Component entity with Id, Name, Bounds, and `IsColliding` flag.
- `src/MiniLayout.Core/Models/Scene.cs`: Domain model holding components and executing native collision evaluations.
- `src/MiniLayout.Core/Scripting/PythonEnvironment.cs`: Auto-locator for `libpython` on macOS (Homebrew `/opt/homebrew/lib`, `/opt/homebrew/opt/python@*/lib`, frameworks) and Windows, initializing `pythonnet`.
- `src/MiniLayout.Core/Scripting/ScriptRunner.cs`: Python script execution harness passing `Scene` into scope with thread-safe GIL management.

### Implementation Details & Interfaces
1. **Dynamic Native Resolver**:
   - Uses `NativeLibrary.SetDllImportResolver` to search:
     - `AppContext.BaseDirectory`
     - Relative path `../../../../native-engine/build/libnative_engine.dylib` (for direct development test runs)
     - Standard OS library paths
2. **Dynamic Python Discovery**:
   - Searches for `libpython3.14.dylib`, `libpython3.13.dylib`, `libpython3.12.dylib`, `libpython3.11.dylib` in Homebrew paths (`/opt/homebrew/lib`, `/opt/homebrew/opt/python@*/Frameworks/...`) and system frameworks. Sets `Runtime.PythonDLL` before any `PythonEngine` call.
3. **Domain & Scripting API**:
   - Python scripts can manipulate `scene`:
     ```python
     scene.Clear()
     scene.AddComponent(1, "BoxA", 0.0, 0.0, 0.0, 2.0, 2.0, 2.0)
     scene.AddComponent(2, "BoxB", 1.5, 0.0, 0.0, 2.0, 2.0, 2.0)
     scene.EvaluateCollisions()
     ```

### Verification Criteria
- `MiniLayout.Core` builds cleanly without warnings:
  ```bash
  dotnet build src/MiniLayout.Core/MiniLayout.Core.csproj
  ```

---

## Stage 3: Avalonia UI & Headless Tests (`src/MiniLayout.App` & `tests/MiniLayout.Tests`)

### Goal
Implement a responsive Avalonia UI desktop application with a top-down 2D canvas displaying colliding (Red) and clear (Green) components, plus an interactive button executing sample Python layout scripts. Alongside, implement a headless `xUnit` test suite verifying C++ collision math and Python scripting without UI dependencies.

### Files to Create
- `src/MiniLayout.App/MiniLayout.App.csproj`: Avalonia 11 desktop application project (`net10.0`).
- `src/MiniLayout.App/Program.cs`: Avalonia application entrypoint with ClassicDesktopStyle lifetime.
- `src/MiniLayout.App/App.axaml` & `App.axaml.cs`: Application definition with Fluent/Simple theme.
- `src/MiniLayout.App/ViewModels/ViewModelBase.cs`: Observable base class implementing `INotifyPropertyChanged`.
- `src/MiniLayout.App/ViewModels/MainViewModel.cs`: ViewModel managing `Scene`, component observable list, status bar counts, and Python execution command.
- `src/MiniLayout.App/Views/MainWindow.axaml`: Canvas view rendering top-down 2D projection and toolbar with "Run Python Script", "Add Box", "Reset" buttons.
- `src/MiniLayout.App/Views/MainWindow.axaml.cs`: Code-behind with custom canvas drawing logic for components and collision indicators.
- `tests/MiniLayout.Tests/MiniLayout.Tests.csproj`: `net10.0` xUnit test project referencing `MiniLayout.Core`.
- `tests/MiniLayout.Tests/CollisionTests.cs`: Headless tests verifying overlapping, touching, and separated boxes against the C++ engine.
- `tests/MiniLayout.Tests/ScriptingTests.cs`: Headless tests validating Python script execution, component injection, and scene mutation.

### Implementation Details & Interfaces
1. **Avalonia Canvas Rendering**:
   - Converts 3D component coordinates $(x, y, w, h)$ into 2D canvas coordinates with centering offset and zoom scaling.
   - Fill: Green (`#2ecc71`) when `!component.IsColliding`, Red (`#e74c3c`) when `component.IsColliding`.
   - Overlay: component ID and dimensions.
2. **Headless Unit Tests**:
   - `CollisionTests`: Validates:
     - Non-overlapping boxes -> collision flag 0.
     - Partial & full overlapping boxes -> collision flag 1.
     - Edge-touching boundary conditions.
     - Multi-box scene batch evaluation.
   - `ScriptingTests`: Validates:
     - `PythonEnvironment.EnsureInitialized()` succeeds.
     - Running Python script correctly populates and moves components in `Scene`.

### Verification Criteria
- Run headless tests:
  ```bash
  dotnet test tests/MiniLayout.Tests/MiniLayout.Tests.csproj
  ```
  All tests pass (100% success rate).
- Build desktop app:
  ```bash
  dotnet build src/MiniLayout.App/MiniLayout.App.csproj
  ```

---

## Stage 4: Build/SBOM Script & Architecture Docs (`scripts/build_and_sbom.sh` & `README.md`)

### Goal
Deliver an automated end-to-end build and CycloneDX SBOM script along with comprehensive technical architecture documentation.

### Files to Create
- `scripts/build_and_sbom.sh`: Executable bash script that validates prerequisites, builds native C++ shared library, deploys library to .NET bin directories, builds .NET solution, executes headless tests, and generates CycloneDX SBOMs for NuGet and vcpkg dependencies into `sbom.cdx.json`.
- `README.md`: Concise, professional technical documentation detailing architecture, C ABI design, P/Invoke decisions, embedded Python discovery, build steps, and SBOM generation.

### Implementation Details
1. **Automation Script (`scripts/build_and_sbom.sh`)**:
   - Checks presence of required tools: `clang++`, `cmake`, `ninja`, `vcpkg`, `dotnet`, `dotnet-CycloneDX`.
   - Configures and builds `src/native-engine` via CMake in release mode.
   - Copies `libnative_engine.dylib` to target directories:
     - `src/MiniLayout.App/bin/Debug/net10.0/`
     - `tests/MiniLayout.Tests/bin/Debug/net10.0/`
   - Runs `dotnet test` with headless console logger.
   - Generates CycloneDX SBOM for .NET using `dotnet-CycloneDX` on `MiniLayout.sln`.
   - Merges or links vcpkg dependency metadata into `sbom.cdx.json`.
2. **Technical Documentation (`README.md`)**:
   - Architecture diagram (C++ engine <-> .NET Core <-> Python & Avalonia UI).
   - Cross-platform ABI and marshaling rationale.
   - Step-by-step developer guide for building, running tests, and launching the Avalonia desktop app.

### Verification Criteria
- Execute script end-to-end:
  ```bash
  chmod +x scripts/build_and_sbom.sh
  ./scripts/build_and_sbom.sh
  ```
- Verify:
  - Script completes with exit code 0.
  - All tests pass.
  - `sbom.cdx.json` is generated and contains valid CycloneDX JSON.
  - `README.md` is complete and clear.
