---
name: uvs-playmode-tests-registry
description: >-
  Runs UVS Benchmark Play Mode performance tests against Unity Registry
  Visual Scripting 1.9.11 in uvs-benchmark. Use when the user asks to run
  benchmark tests on stock 1.9.11, Registry baseline, Run A, or compare
  against enhanced without naming the enhanced branch.
disable-model-invocation: true
---

# UVS Play Mode tests — Registry 1.9.11

Runs matrix **Run A**: stock **`com.unity.visualscripting` `1.9.11`** from the Unity Registry.

## Run (required)

From the **uvs-benchmark repo root**, execute:

```powershell
.\tools\Run-UvsPlayModeBenchmarks.ps1 -UvsSource Registry
```

Do not skip package switching. Do not ask the user to switch git branches.

## What the script does

1. Ensures `Projects/UvsBenchmarkHost/Packages/manifest.json` has a `com.unity.visualscripting` entry (adds `1.9.11` if missing).
2. Calls `Set-UvsManifestSource.ps1 -Source Registry`.
3. Runs Unity **6000.4.0f1** in batch mode: Play Mode tests in assembly `Miraluna.Uvs.Benchmarks.Tests`.

## Unity Editor path

- Prefer `$env:UNITY_EDITOR` pointing at `Unity.exe` (**6000.4.0f1**).
- Otherwise the script searches Unity Hub for `6000.4*`.

If resolution fails, set `UNITY_EDITOR` and re-run.

## Optional flags

```powershell
.\tools\Run-UvsPlayModeBenchmarks.ps1 -UvsSource Registry -TestFilter "UvsCounter"
.\tools\Run-UvsPlayModeBenchmarks.ps1 -UvsSource Registry -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.4.0f1\Editor\Unity.exe"
```

## Outputs

| Artifact | Location |
|----------|----------|
| NUnit XML | `TestResults/playmode-Registry-<timestamp>.xml` |
| Unity log | `TestResults/unity-playmode-Registry-<timestamp>.log` |
| Performance reports | `PerformanceTestResults/` (gitignored) |

## Verify package source

Sample groups should show UVS **version** `1.9.11` and source **`registry`** (via `UvsPackageProbe`).

## Tests executed

- `UpdateOverheadTests`: `UvsOverhead_*`, `CSharpOverhead_*` (100 / 1000 / 5000)
- `CounterUpdateTests`: `UvsCounter_*`, `CSharpCounter_*` (100 / 1000 / 5000)

## Report results

```powershell
.\tools\Report-UvsPlayModeResults.ps1 -Source Registry
```

Or skill **`uvs-playmode-results-report`**.

## Compare to enhanced

After this run, use skill **`uvs-playmode-tests-enhanced`**, then **`uvs-playmode-results-compare`** (or `Compare-UvsPlayModeResults.ps1`).

## Editor alternative

**Window → General → Test Runner → PlayMode → Performance**, after `Set-UvsManifestSource.ps1 -Source Registry` and reopening `Projects/UvsBenchmarkHost`.

## Troubleshooting

| Issue | Action |
|-------|--------|
| Manifest script error | Ensure host project path is `Projects/UvsBenchmarkHost` |
| Unity not found | Set `UNITY_EDITOR` to 6000.4.0f1 `Unity.exe` |
| Tests missing | Confirm `testables` includes `com.miraluna.uvs.benchmarks` in host manifest |
| Long first run | UPM resolve/import; wait for batch mode to finish |

See [docs/benchmarking.md](../../../docs/benchmarking.md).
