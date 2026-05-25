---
name: uvs-playmode-results-report
description: >-
  Reports UVS Benchmark Play Mode performance test results from TestResults XML
  (Registry, enhanced, or stable). Use when the user asks for a benchmark report,
  test results summary, performance numbers, or after running uvs-playmode-tests
  skills.
disable-model-invocation: true
---

# UVS Play Mode results — report

Summarizes one CLI test run from `TestResults/playmode-<source>-*.xml`.

## Prerequisites

A completed run from `Run-UvsPlayModeBenchmarks.ps1` or skills `uvs-playmode-tests-registry` / `uvs-playmode-tests-enhanced`.

## Report latest run by source

```powershell
# After Registry 1.9.11 run
.\tools\Report-UvsPlayModeResults.ps1 -Source Registry

# After enhanced run
.\tools\Report-UvsPlayModeResults.ps1 -Source EnhancedGit
```

## Report a specific XML file

```powershell
.\tools\Report-UvsPlayModeResults.ps1 -ResultsXml "TestResults\playmode-Registry-20260525-120000.xml"
```

## Output

- Markdown printed to the terminal
- Saved to `TestResults/report-<source>-<timestamp>.md`

Sections:

1. Run metadata (file, UVS version/source from sample groups)
2. All tests table (median/avg/min/max ms)
3. **UVS vs C#** ratio table per workload and N

## Present results to the user

1. Run the script (do not guess numbers).
2. Paste or summarize the generated markdown.
3. Call out **UvsCounter** and **UvsOverhead** at **1000** and **5000** as primary signals.
4. Note `Result` column if any test failed.

## Parser notes

Metrics are parsed from Unity Performance Testing output in NUnit XML (`Median`, `Avg`, etc.). Sample groups look like `UvsCounter_1000_registry_1.9.11`.

If parsing fails, open the XML `output` for a `UvsCounter_*` test and fix regex in `tools/UvsPlayModeResults.Parser.ps1`.

## Next step

For **1.9.11 vs enhanced**, use skill **`uvs-playmode-results-compare`** after both runs exist.

See [docs/benchmarking.md](../../../docs/benchmarking.md).
