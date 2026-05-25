---
name: uvs-playmode-results-compare
description: >-
  Compares UVS Benchmark Play Mode results between Registry 1.9.11 and enhanced
  (or two XML files). Use when the user asks to compare benchmark runs, delta,
  enhanced vs stock UVS, or interpret A vs C matrix results.
disable-model-invocation: true
---

# UVS Play Mode results — compare

Compares **baseline Registry 1.9.11** vs **candidate enhanced** using the latest matching XML files in `TestResults/`.

## Prerequisites

Both runs must exist:

1. `uvs-playmode-tests-registry` (or `Run-UvsPlayModeBenchmarks.ps1 -UvsSource Registry`)
2. `uvs-playmode-tests-enhanced` (or `-UvsSource EnhancedGit`)

## Compare (default: latest Registry vs latest EnhancedGit)

```powershell
.\tools\Compare-UvsPlayModeResults.ps1
```

Equivalent:

```powershell
.\tools\Compare-UvsPlayModeResults.ps1 -BaselineSource Registry -CandidateSource EnhancedGit
```

## Compare explicit files

```powershell
.\tools\Compare-UvsPlayModeResults.ps1 `
  -BaselineXml "TestResults\playmode-Registry-20260525-100000.xml" `
  -CandidateXml "TestResults\playmode-EnhancedGit-20260525-103000.xml"
```

## Other pairs

```powershell
# Stable promoted fork vs enhanced integration
.\tools\Compare-UvsPlayModeResults.ps1 -BaselineSource StableGit -CandidateSource EnhancedGit
```

## Output

- Markdown printed to the terminal
- Saved to `TestResults/compare-<baseline>-vs-<candidate>-<timestamp>.md`

Sections:

1. **UVS tests** — median ms, Δ ms, Δ % (negative Δ % = candidate faster)
2. **C# sanity check** — should stay near 0% change if only UVS changed
3. **UVS/C# ratio change** — script overhead vs C# ceiling

## How to interpret for the user

| Observation | Meaning |
|---------------|---------|
| UVS Δ % negative at high N | Enhanced fork faster than Registry |
| UVS Δ % near 0 | Little measurable difference in this environment |
| C# Δ % large | Environment unfair or non-UVS change; re-run |
| Ratio drops | UVS closer to C# ceiling |

Lead with **UvsCounter** and **UvsOverhead** at **N=1000** and **5000**.

## Full workflow (agent checklist)

```text
- [ ] Run Registry benchmarks
- [ ] Report Registry (optional): Report-UvsPlayModeResults.ps1 -Source Registry
- [ ] Run enhanced benchmarks
- [ ] Report enhanced (optional): Report-UvsPlayModeResults.ps1 -Source EnhancedGit
- [ ] Compare-UvsPlayModeResults.ps1
- [ ] Summarize comparison markdown for the user
```

## Troubleshooting

| Issue | Action |
|-------|--------|
| No baseline XML | Run Registry tests first |
| No candidate XML | Run enhanced tests first |
| Missing median values | Inspect XML test `output`; re-run tests in Unity 2021.3.45f2 |

See [docs/benchmarking.md](../../../docs/benchmarking.md).
