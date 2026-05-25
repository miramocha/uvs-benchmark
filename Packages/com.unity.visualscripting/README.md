[![codecov](https://codecov.unity3d.com/ghe/unity/com.unity.visualscripting/graph/badge.svg?token=M3WZU9607U)](https://codecov.unity3d.com/ghe/unity/com.unity.visualscripting)

# Visual Scripting (com.unity.visualscripting)

Visual Scripting, previously known as BOLT, is an alternative workflow to design behaviours. Instead of the classic method of writing a C# script, visual scripting offers a way to design behaviours intuitively without code, by connecting events, actions, and data together in a graph. 

Both programmers and non-programmers can use node-based graphs to design final logic or to quickly create prototypes. This package also features an API that programmers can use for more advanced tasks, or to create custom nodes that can be used by other team members.

> **Community fork:** [uvs-benchmark](https://github.com/miramocha/uvs-benchmark) monorepo ΓÇö a customized build of UnityΓÇÖs Visual Scripting. It keeps the official package ID **`com.unity.visualscripting`** so it can replace the Unity Registry version in a drop-in way. Licensed under the [Unity Package Distribution License](LICENSE.md) (not MIT).

## Installing this fork

Newer Unity projects already list **`com.unity.visualscripting`** in **`Packages/manifest.json`** (often as a Registry version such as **`1.9.11`**). To use this package instead, change **only the source** of that dependencyΓÇöthe package name stays **`com.unity.visualscripting`**.

### Git URL in `manifest.json` (recommended)

1. Open **`Packages/manifest.json`** in your Unity project.
2. Replace the Visual Scripting line with a Git URL, **`?path=`**, and **branch or tag**:

```json
"com.unity.visualscripting": "https://github.com/miramocha/uvs-benchmark.git?path=/Packages/com.unity.visualscripting#enhanced"
```

For a **slower-moving** snapshot line, pin **`#stable`** (promoted from `enhanced` via pull request):

```json
"com.unity.visualscripting": "https://github.com/miramocha/uvs-benchmark.git?path=/Packages/com.unity.visualscripting#stable"
```

For reproducible projects, prefer a **tag** (for example `#1.9.11-enhanced.2`) instead of a branch that may move.

3. Save and return to the editor so the Package Manager refreshes. Unity updates **`Packages/packages-lock.json`** automatically.

You can also use **Window ΓåÆ Package Manager ΓåÆ + ΓåÆ Add package from Git URLΓÇª**; that applies the same change to **`manifest.json`**.

### Embedded under `Packages/`

Put this package folder at **`Packages/com.unity.visualscripting`** (submodule, subtree, or copy). Unity resolves it as an **embedded** package with the same id. Avoid keeping both a Registry/git reference and a second copyΓÇöuse **one** source for **`com.unity.visualscripting`**.

### Repo layout

This package lives at **`Packages/com.unity.visualscripting/`** in [miramocha/uvs-benchmark](https://github.com/miramocha/uvs-benchmark). Git UPM installs **must** include **`?path=/Packages/com.unity.visualscripting`**.

### Notes

- **Conflicts:** DonΓÇÖt declare **`com.unity.visualscripting`** twice (for example Registry version plus embedded folder) unless you intentionally know how UPM resolves it.
- **Lock file:** Prefer letting Unity regenerate **`packages-lock.json`** after **`manifest.json`** changes.
- **Unity version:** This packageΓÇÖs **`package.json`** declares **`unity`: `2021.3`**; use a matching or newer Editor when possible.
- **Branches:** **`enhanced`** is the integration line; **`stable`** is the promoted line. Details: **[CONTRIBUTING.md](CONTRIBUTING.md)**.

## Performance benchmarks

Play Mode benchmarks comparing this fork to Registry **1.9.11** and to equivalent C# workloads ship in the same repository:

- **Package:** `com.miraluna.uvs.benchmarks` at `Packages/com.miraluna.uvs.benchmarks/`
- **Host project:** `Projects/UvsBenchmarkHost/`
- **Wiki:** [Test methodology](https://github.com/miramocha/uvs-benchmark/wiki/Test-Methodology) ┬╖ [Test results](https://github.com/miramocha/uvs-benchmark/wiki/Test-Results)

From the repo root, switch the host manifest with `Set-UvsManifestSource.ps1 -Source StableGit` (or `-Source EnhancedGit` / `-Source LocalEmbedded`), then run Play Mode performance tests in **Test Runner**.

# Required Software

Unity: **`package.json`** targets **2021.3** or newer; match or exceed that with your Editor version.

# Documentation

Documentation is available [here](https://docs.unity3d.com/bolt/1.4/manual/index.html).

For further discussion, visit the [Discord](https://discord.com/channels/372898201088426004/372899380367458329) or the [Visual Scripting forum](https://forum.unity.com/forums/visual-scripting.537/).
