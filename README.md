# UVS Benchmark (`com.miraluna.uvs.benchmarks`)

Play Mode performance benchmarks comparing **Unity Visual Scripting** and equivalent **C# `MonoBehaviour`** workloads at scale.

This repository also ships a community **Visual Scripting** fork (`com.unity.visualscripting`) for drop-in performance and correctness improvements over Registry **1.9.11**.

Repository: [https://github.com/miramocha/uvs-benchmark](https://github.com/miramocha/uvs-benchmark)

## Requirements

- Unity **2021.3.45f2** LTS (security-patched; use **`2021.3.45f2`** in Unity Hub, not f1)
- Unity Registry package **`com.unity.visualscripting` `1.9.11`** (default) or this repo’s fork via Git **`#stable`** / **`#enhanced`**

## Quick start

1. Clone this repo.
2. Open **`Projects/UvsBenchmarkHost`** in **Unity 2021.3.45f2** (Unity regenerates `ProjectSettings.asset` on first open if missing).
3. Wait for Package Manager to resolve dependencies (host `manifest.json` lists the benchmark package under **`testables`** so Test Runner discovers its tests).
4. Open **Window → General → Test Runner**, select **PlayMode**, enable **Performance** tests.
5. Run tests (e.g. `UvsCounter_1000`, `CSharpCounter_1000`).

See [docs/benchmarking.md](docs/benchmarking.md) for the Registry vs fork comparison matrix.

**Wiki:** [Test methodology](https://github.com/miramocha/uvs-benchmark/wiki/Test-Methodology) · [Test results](https://github.com/miramocha/uvs-benchmark/wiki/Test-Results)

## Repository layout

```text
Packages/com.miraluna.uvs.benchmarks/     UPM benchmarks + tests (MIT)
Packages/com.unity.visualscripting/       Community UVS fork (UPML)
Projects/UvsBenchmarkHost/                Unity host project
tools/Set-UvsManifestSource.ps1           Switch UVS: Registry, StableGit, EnhancedGit, LocalEmbedded
docs/benchmarking.md                      Run matrix and interpretation
```

## Compare against the community UVS fork

Default host manifest uses Registry **`1.9.11`**. To benchmark the fork from this repo:

```powershell
.\tools\Set-UvsManifestSource.ps1 -Source StableGit
```

For the integration **`enhanced`** branch:

```powershell
.\tools\Set-UvsManifestSource.ps1 -Source EnhancedGit
```

For day-to-day work against the embedded copy in this clone (no network):

```powershell
.\tools\Set-UvsManifestSource.ps1 -Source LocalEmbedded
```

Reopen the project in Unity, run the same Play Mode performance tests, and export reports from the Test Runner **Performance** tab.

Restore Registry baseline:

```powershell
.\tools\Set-UvsManifestSource.ps1 -Source Registry
```

## Install in another Unity project

**Benchmarks only (MIT):**

```json
"com.miraluna.uvs.benchmarks": "https://github.com/miramocha/uvs-benchmark.git?path=/Packages/com.miraluna.uvs.benchmarks"
```

**Visual Scripting fork (UPML — see [LICENSE-THIRD-PARTY.md](LICENSE-THIRD-PARTY.md)):**

```json
"com.unity.visualscripting": "https://github.com/miramocha/uvs-benchmark.git?path=/Packages/com.unity.visualscripting#stable"
```

Integration branch:

```json
"com.unity.visualscripting": "https://github.com/miramocha/uvs-benchmark.git?path=/Packages/com.unity.visualscripting#enhanced"
```

Pinned release tag:

```json
"com.unity.visualscripting": "https://github.com/miramocha/uvs-benchmark.git?path=/Packages/com.unity.visualscripting#1.9.11-enhanced.2"
```

You must also add test packages when using benchmarks. See [Packages/com.unity.visualscripting/README.md](Packages/com.unity.visualscripting/README.md) for fork-only installs.

## License

- **Benchmarks, host project, docs, tools:** [MIT](LICENSE)
- **`Packages/com.unity.visualscripting/`:** [Unity Package Distribution License](LICENSE-THIRD-PARTY.md) — not MIT
