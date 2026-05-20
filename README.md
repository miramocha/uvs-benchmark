# UVS Benchmark (`com.miraluna.uvs.benchmarks`)

Play Mode performance benchmarks comparing **Unity Visual Scripting** and equivalent **C# `MonoBehaviour`** workloads at scale.

Repository: [https://github.com/miramocha/uvs-benchmark](https://github.com/miramocha/uvs-benchmark)

## Requirements

- Unity **2021.3.45f2** LTS (security-patched; use **`2021.3.45f2`** in Unity Hub, not f1)
- Unity Registry package **`com.unity.visualscripting` `1.9.11`** (default) or your [enhanced fork](https://github.com/miramocha/unity-visual-scripting-enhancements)

## Quick start

1. Clone this repo.
2. Open **`Projects/UvsBenchmarkHost`** in the Unity Editor.
3. Wait for Package Manager to resolve dependencies.
4. Open **Window → General → Test Runner**, select **PlayMode**, enable **Performance** tests.
5. Run tests (e.g. `UvsCounter_1000`, `CSharpCounter_1000`).

See [docs/benchmarking.md](docs/benchmarking.md) for the Registry vs enhanced comparison matrix.

## Repository layout

```text
Packages/com.miraluna.uvs.benchmarks/   UPM package (benchmarks + tests)
Projects/UvsBenchmarkHost/              Unity host project
tools/Set-UvsManifestSource.ps1         Switch UVS between Registry and enhanced Git
docs/benchmarking.md                    Run matrix and interpretation
```

## Compare against enhanced Visual Scripting

Default host manifest uses Registry **`1.9.11`**. To benchmark the enhanced fork:

```powershell
.\tools\Set-UvsManifestSource.ps1 -Source EnhancedGit
```

Reopen the project in Unity, run the same Play Mode performance tests, and export reports from the Test Runner **Performance** tab.

Restore Registry baseline:

```powershell
.\tools\Set-UvsManifestSource.ps1 -Source Registry
```

## Install package in another project

```json
"com.miraluna.uvs.benchmarks": "https://github.com/miramocha/uvs-benchmark.git?path=/Packages/com.miraluna.uvs.benchmarks"
```

You must also add **`com.unity.visualscripting`** (Registry or Git) and test packages in that project's manifest.

## License

MIT (see LICENSE if present).
