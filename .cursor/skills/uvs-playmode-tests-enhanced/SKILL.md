---
name: uvs-playmode-tests-enhanced
description: >-
  Runs UVS Benchmark Play Mode performance tests against the community Visual
  Scripting fork on the enhanced branch (Git UPM from uvs-benchmark monorepo).
  Use when the user asks to run benchmark tests on enhanced, Run C, the fork,
  or community UVS optimizations.
disable-model-invocation: true
---

# UVS Play Mode tests — enhanced branch

Runs matrix **Run C**: **`com.unity.visualscripting`** from  
`https://github.com/miramocha/uvs-benchmark.git?path=/Packages/com.unity.visualscripting#enhanced`.

## Run (required)

From the **uvs-benchmark repo root**, execute:

```powershell
.\tools\Run-UvsPlayModeBenchmarks.ps1 -UvsSource EnhancedGit
```

Do not require checking out git branch **`enhanced`** unless the user explicitly wants **`-UvsSource LocalEmbedded`**.

## What the script does

1. Ensures `Projects/UvsBenchmarkHost/Packages/manifest.json` has a `com.unity.visualscripting` entry.
2. Calls `Set-UvsManifestSource.ps1 -Source EnhancedGit`.
3. Runs Unity **2021.3.45f2** batch Play Mode tests in `Miraluna.Uvs.Benchmarks.Tests`.

First run may be slow while UPM clones the fork from GitHub.

## Unity Editor path

- Prefer `$env:UNITY_EDITOR` → `Unity.exe` (**2021.3.45f2**).
- Otherwise Unity Hub `2021.3.45f2*` is auto-detected.

## Local embedded fork (optional)

Only when testing the working tree copy without Git fetch:

```powershell
git checkout enhanced
.\tools\Run-UvsPlayModeBenchmarks.ps1 -UvsSource LocalEmbedded
```

## Optional flags

```powershell
.\tools\Run-UvsPlayModeBenchmarks.ps1 -UvsSource EnhancedGit -TestFilter "UvsCounter"
```

## Outputs

| Artifact | Location |
|----------|----------|
| NUnit XML | `TestResults/playmode-EnhancedGit-<timestamp>.xml` |
| Unity log | `TestResults/unity-playmode-EnhancedGit-<timestamp>.log` |
| Performance reports | `PerformanceTestResults/` (gitignored) |

## Verify package source

Sample groups should show a fork version (e.g. `1.9.11-enhanced.2`) and source **`git`**.

## Report results

```powershell
.\tools\Report-UvsPlayModeResults.ps1 -Source EnhancedGit
```

Or skill **`uvs-playmode-results-report`**.

## Compare to Registry 1.9.11

Run skill **`uvs-playmode-tests-registry`** first, then:

```powershell
.\tools\Compare-UvsPlayModeResults.ps1
```

Or skill **`uvs-playmode-results-compare`**.

## Stable line (not enhanced)

For promoted fork (**Run B**), use:

```powershell
.\tools\Run-UvsPlayModeBenchmarks.ps1 -UvsSource StableGit
```

## Troubleshooting

| Issue | Action |
|-------|--------|
| Git UPM failed | Manifest URL must include `?path=/Packages/com.unity.visualscripting#enhanced` |
| Wrong package | Re-run script; delete `Library/PackageCache` if stale |
| Unity not found | Set `UNITY_EDITOR` for 2021.3.45f2 |

See [docs/benchmarking.md](../../../docs/benchmarking.md).
