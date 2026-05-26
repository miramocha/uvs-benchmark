# Benchmarking guide

## What is measured

| Test family | UVS arm | C# arm |
|-------------|---------|--------|
| **Overhead** | `On Update` only (flow dispatch) | Empty `Update()` |
| **Counter** | On Update → get/add/set object variable `counter` | `value++` in `Update()` |
| **Rotate** | On Update → `Random.Range(0, 222)` → `Vector3` → `Transform.Rotate` on This | Same logic in `Update()` (`RotateBehaviour`) |

Object counts: **100**, **1000**, **5000** agents (one `ScriptMachine` or one `MonoBehaviour` per GameObject).

Each run uses **120 warmup frames** and **300 measurement frames** (`Measure.Frames()`).

## Run matrix

| Run | `com.unity.visualscripting` source | Purpose |
|-----|-----------------------------------|---------|
| **A** | Registry `1.9.11` (default manifest) | Baseline / stock UVS |
| **B** | Git `#stable` from [uvs-benchmark](https://github.com/miramocha/uvs-benchmark) (`?path=/Packages/com.unity.visualscripting`) | Compare stable community fork |
| **C** | Git `#enhanced` (optional) | Compare integration branch |

Run **all** Play Mode performance tests in both A and B. Sample group names include agent kind, object count, UVS version, and package source (`registry` vs `git`).

### Switching UVS source

From repo root:

```powershell
# Run A (default)
.\tools\Set-UvsManifestSource.ps1 -Source Registry

# Run B (stable fork — recommended comparison)
.\tools\Set-UvsManifestSource.ps1 -Source StableGit

# Run C (enhanced branch, optional)
.\tools\Set-UvsManifestSource.ps1 -Source EnhancedGit

# Local embedded package (this clone, no Git fetch)
.\tools\Set-UvsManifestSource.ps1 -Source LocalEmbedded
```

After each change, reopen **`Projects/UvsBenchmarkHost`** in Unity (or use **Window → Package Manager → Resolve**) so packages refresh.

## Interpreting results

1. **Test Runner → Performance** — export JSON report after each matrix leg.
2. Compare **Uvs*** vs **CSharp*** at the same N (script ceiling vs Visual Scripting).
3. Compare **Uvs*** at Run A vs Run B at the same N (enhancement impact).
4. **CSharp*** should be similar between A and B if only UVS changed.

## Fairness notes

- Graphs are built in code (`BenchmarkGraphFactory`) with **1.9.11-compatible** nodes only.
- VSync is disabled; `targetFrameRate` is 60 during tests.
- Spawning happens **before** the measured frame window.
- Editor Play Mode numbers differ from standalone players; use a Development build for release-like checks if needed.

## Troubleshooting

| Issue | Check |
|-------|--------|
| Tests missing in Test Runner | Host `manifest.json` must include `"testables": ["com.miraluna.uvs.benchmarks"]`; enable Play Mode Test Runner; reimport package |
| UVS graphs not running | Visual Scripting project settings; ensure `ScriptMachine` + `Variables` on UVS agents |
| Wrong UVS version | `UvsPackageProbe` labels in sample groups; verify manifest / lock file |
| Git UPM checkout failed | URL must include `?path=/Packages/com.unity.visualscripting` |
