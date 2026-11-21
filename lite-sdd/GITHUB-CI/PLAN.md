# Implementation Plan: GITHUB-CI (lite-sdd/GITHUB-CI/PLAN.md)

This implementation plan defines the staged delivery of the GitHub Actions CI and optional release workflow for `MiniLayout`, based on the approved [lite-sdd/GITHUB-CI/SPEC.md](file:///Users/jansve/minilayout-pvt/lite-sdd/GITHUB-CI/SPEC.md).

---

## Stage 1: Core CI Pipeline (`.github/workflows/ci.yml` - `build-and-test` Job)

### Goal
Implement the primary continuous integration job running on `macos-latest` (ARM64) that restores cached dependencies, compiles the C++20 engine and .NET 10 solution via `scripts/build_and_sbom.sh`, executes headless unit tests, generates `sbom.cdx.json`, and archives artifacts.

### Files to Create / Modify
- `.github/workflows/ci.yml`: Workflow file with trigger definitions, caching, `build-and-test` job, and artifact uploads.
- `scripts/build_and_sbom.sh`: Minor check to seamlessly adapt to GitHub runner's default `$VCPKG_INSTALLATION_ROOT` if `$VCPKG_ROOT` is not explicitly set.

### Implementation Details & Interfaces
1. **Trigger & Concurrency Specification**:
   ```yaml
   name: CI

   on:
     workflow_dispatch:
       inputs:
         create_release:
           description: 'Create a GitHub Release if tests succeed'
           required: false
           type: boolean
           default: false

   concurrency:
     group: ${{ github.workflow }}-${{ github.ref }}
     cancel-in-progress: true
   ```
2. **Step Sequence for `build-and-test`**:
   - `actions/checkout@v4`
   - `actions/setup-dotnet@v4` (targeting .NET 10 via `global.json`)
   - `actions/setup-python@v5` (version: `3.12` to provide guaranteed compatible dynamic library for PythonNet)
   - Ensure tools installed: `ninja` via brew (if missing), `dotnet-CycloneDX` via `dotnet tool install --global CycloneDX`
   - Set up `vcpkg`: if `$VCPKG_INSTALLATION_ROOT` exists on runner, point `VCPKG_ROOT` to it; otherwise clone & bootstrap
   - `actions/cache@v4` for vcpkg binary cache (`~/.cache/vcpkg`) with key: `${{ runner.os }}-vcpkg-${{ hashFiles('src/native-engine/vcpkg.json') }}`
   - `actions/cache@v4` for NuGet packages (`~/.nuget/packages`) with key: `${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', 'global.json') }}`
   - Execute `./scripts/build_and_sbom.sh`
   - `actions/upload-artifact@v4` for:
     - `sbom.cdx.json` (name: `cyclonedx-sbom`)
     - `src/MiniLayout.App/bin/Release/net10.0/` (name: `minilayout-macos-arm64`)

### Verification Criteria
- YAML syntax is valid and adheres to GitHub Actions schema.
- Local verification of script behavior with CI-like environment variables (`VCPKG_INSTALLATION_ROOT` fallback).

---

## Stage 2: Gated Release Step (`.github/workflows/ci.yml` - `release` Job)

### Goal
Implement an isolated, gated release job that packages application binaries and the CycloneDX SBOM into a release archive and creates a GitHub Release when triggered by version tags or manual workflow dispatch.

### Files to Create / Modify
- `.github/workflows/ci.yml`: Append the `release` job block.

### Implementation Details & Interfaces
1. **Gating Condition**:
   ```yaml
   release:
     name: Publish Release
     needs: build-and-test
     if: github.event_name == 'workflow_dispatch' && inputs.create_release == true
     runs-on: macos-latest
     permissions:
       contents: write
   ```
2. **Packaging and Publishing Steps**:
   - Download artifacts `cyclonedx-sbom` and `minilayout-macos-arm64`.
   - Compress application binaries and dynamic library into `MiniLayout-macos-arm64.tar.gz`.
   - Publish via `softprops/action-gh-release@v2`:
     - Files: `MiniLayout-macos-arm64.tar.gz`, `sbom.cdx.json`.
     - Draft: `false`, Prerelease: `false`.
     - Automatically uses tag name or dispatch commit SHA.
3. **Portability / Detachability**:
   - The entire `release` job is decoupled from `build-and-test`. Deleting this block will cleanly remove release behavior without affecting CI.

### Verification Criteria
- Verified YAML syntax with multi-job dependencies (`needs: build-and-test`).
- Confirmed condition logic triggers strictly on `refs/tags/v*` or explicit manual input.

---

## Stage 3: CI Pipeline Validation & Documentation

### Goal
Validate the complete workflow locally using YAML linting/schema checks, verify script execution resilience, and document CI setup and release instructions in `README.md`.

### Files to Create / Modify
- `README.md`: Add a concise "Continuous Integration & Releases" section documenting the GitHub Actions pipeline, caching, and tag/manual release triggers.
- `scripts/build_and_sbom.sh`: Confirm end-to-end execution remains 100% functional and idempotent.

### Verification Criteria
- Parse `.github/workflows/ci.yml` with Python YAML parser to ensure valid structure and types.
- Execute `./scripts/build_and_sbom.sh` to confirm 100% passing tests and valid `sbom.cdx.json`.
- `git status` verifies no unwanted files or modifications to `lite-sdd/base/`.
