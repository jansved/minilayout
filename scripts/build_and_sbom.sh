#!/usr/bin/env bash
set -euo pipefail

# MiniLayout Build, Test, and CycloneDX SBOM Automation Script
# Targets macOS (Apple Silicon ARM64) and cross-platform environments.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"

echo "=========================================================="
echo "         MiniLayout Build & CycloneDX SBOM Pipeline       "
echo "=========================================================="

cd "${ROOT_DIR}"

# 1. Environment and prerequisite verification
echo "[1/6] Checking prerequisites..."

command -v cmake >/dev/null 2>&1 || { echo "Error: cmake is required but not installed." >&2; exit 1; }
command -v ninja >/dev/null 2>&1 || { echo "Error: ninja is required but not installed." >&2; exit 1; }
command -v clang++ >/dev/null 2>&1 || { echo "Error: clang++ is required but not installed." >&2; exit 1; }
command -v dotnet >/dev/null 2>&1 || { echo "Error: dotnet SDK is required but not installed." >&2; exit 1; }
command -v dotnet-CycloneDX >/dev/null 2>&1 || { echo "Error: dotnet-CycloneDX is required but not installed." >&2; exit 1; }

# Locate vcpkg root
if [ -z "${VCPKG_ROOT:-}" ]; then
    if [ -d "${HOME}/tools/vcpkg" ]; then
        export VCPKG_ROOT="${HOME}/tools/vcpkg"
    elif [ -n "${VCPKG_INSTALLATION_ROOT:-}" ] && [ -d "${VCPKG_INSTALLATION_ROOT}" ]; then
        export VCPKG_ROOT="${VCPKG_INSTALLATION_ROOT}"
    else
        echo "Error: VCPKG_ROOT is not set and ~/tools/vcpkg not found." >&2
        exit 1
    fi
fi
echo "  Found vcpkg at: ${VCPKG_ROOT}"

# Clear macOS certificate overrides that interfere with native Security.framework
unset SSL_CERT_FILE NODE_EXTRA_CA_CERTS || true
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp/dotnet_home}"

# 2. Build Native C++20 Shared Library
echo "[2/6] Building Native C++20 Engine (CMake + Ninja + vcpkg)..."
cmake -B src/native-engine/build -S src/native-engine -G Ninja \
    -DCMAKE_TOOLCHAIN_FILE="${VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake" \
    -DCMAKE_BUILD_TYPE=Release
cmake --build src/native-engine/build

# Ensure compile_commands.json is accessible at workspace root for IDE language servers
if [ -f src/native-engine/build/compile_commands.json ]; then
    ln -sf src/native-engine/build/compile_commands.json compile_commands.json
fi

# 3. Deploy native shared library to .NET output locations
echo "[3/6] Deploying native engine binary..."
ENGINE_LIB="src/native-engine/build/libnative_engine.dylib"
if [ ! -f "${ENGINE_LIB}" ]; then
    echo "Error: Native library ${ENGINE_LIB} was not produced." >&2
    exit 1
fi

mkdir -p src/MiniLayout.App/bin/Debug/net10.0 tests/MiniLayout.Tests/bin/Debug/net10.0 src/MiniLayout.App/bin/Release/net10.0 tests/MiniLayout.Tests/bin/Release/net10.0
cp "${ENGINE_LIB}" src/MiniLayout.App/bin/Debug/net10.0/
cp "${ENGINE_LIB}" tests/MiniLayout.Tests/bin/Debug/net10.0/
cp "${ENGINE_LIB}" src/MiniLayout.App/bin/Release/net10.0/
cp "${ENGINE_LIB}" tests/MiniLayout.Tests/bin/Release/net10.0/
echo "  Deployed libnative_engine.dylib to App and Tests bin directories."

# 4. Build .NET Solution
echo "[4/6] Building .NET 10 Solution (Core, App, Tests)..."
dotnet build MiniLayout.sln --configuration Release

# 5. Run Headless Unit Tests
echo "[5/6] Running headless xUnit tests (C++ math & Python scripting)..."
dotnet test tests/MiniLayout.Tests/MiniLayout.Tests.csproj --configuration Release --no-build --logger "console;verbosity=normal"

# 6. Generate CycloneDX SBOM (NuGet + vcpkg)
echo "[6/6] Generating CycloneDX SBOM (sbom.cdx.json)..."
dotnet-CycloneDX MiniLayout.sln \
    -o . \
    -fn sbom.cdx.json \
    -F Json \
    --disable-package-restore \
    --set-name "MiniLayout" \
    --set-version "1.0.0"

# Merge vcpkg dependencies into sbom.cdx.json
python3 - <<'EOF'
import json, os

sbom_path = "sbom.cdx.json"
status_path = "src/native-engine/build/vcpkg_installed/vcpkg/status"

if not os.path.exists(sbom_path):
    print("Warning: sbom.cdx.json not found for vcpkg merging.")
    exit(0)

with open(sbom_path, "r", encoding="utf-8") as f:
    bom = json.load(f)

# Collect existing component bom-refs
existing_refs = {c.get("bom-ref") for c in bom.get("components", [])}

# Add native-engine internal component if missing
native_ref = "pkg:generic/native_engine@1.0.0"
if native_ref not in existing_refs:
    bom.setdefault("components", []).append({
        "type": "library",
        "bom-ref": native_ref,
        "name": "native_engine",
        "version": "1.0.0",
        "description": "Native C++20 engine for MiniLayout 3D AABB collision detection",
        "scope": "required",
        "purl": native_ref
    })

# Parse vcpkg installed packages
if os.path.exists(status_path):
    with open(status_path, "r", encoding="utf-8") as f:
        content = f.read()

    packages = {}
    for block in content.strip().split("\n\n"):
        pkg_info = {}
        for line in block.split("\n"):
            if ":" in line:
                k, v = line.split(":", 1)
                pkg_info[k.strip()] = v.strip()
        if "Package" in pkg_info and "Feature" not in pkg_info and pkg_info.get("Status") == "install ok installed":
            packages[pkg_info["Package"]] = pkg_info

    for name, p in packages.items():
        ver = p.get("Version", "1.0.0")
        ref = f"pkg:vcpkg/{name}@{ver}"
        if ref not in existing_refs:
            bom.setdefault("components", []).append({
                "type": "library",
                "bom-ref": ref,
                "name": name,
                "version": ver,
                "description": p.get("Description", f"{name} native vcpkg package"),
                "scope": "required",
                "purl": ref,
                "licenses": [{"license": {"id": "MIT"}}]
            })

with open(sbom_path, "w", encoding="utf-8") as f:
    json.dump(bom, f, indent=2)

print(f"  Successfully enriched sbom.cdx.json (total components: {len(bom.get('components', []))}).")
EOF

echo "=========================================================="
echo " Build, Tests, and CycloneDX SBOM completed successfully! "
echo " Output SBOM: $(pwd)/sbom.cdx.json"
echo "=========================================================="
