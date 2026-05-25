function Get-UvsPlayModeResultFiles {
    param(
        [string] $ResultsDir,
        [string] $Source
    )

    $pattern = if ([string]::IsNullOrWhiteSpace($Source)) {
        "playmode-*.xml"
    } else {
        "playmode-$Source-*.xml"
    }

    Get-ChildItem -Path $ResultsDir -Filter $pattern -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending
}

function Parse-UvsPlayModeResultsXml {
    param([string] $ResultsXmlPath)

    if (-not (Test-Path $ResultsXmlPath)) {
        throw "Results file not found: $ResultsXmlPath"
    }

    [xml] $doc = Get-Content -Path $ResultsXmlPath -Raw -Encoding UTF8
    $testCases = $doc.SelectNodes("//test-case")

    if ($null -eq $testCases -or $testCases.Count -eq 0) {
        throw "No test-case nodes found in: $ResultsXmlPath"
    }

    $rows = New-Object System.Collections.Generic.List[object]

    foreach ($case in $testCases) {
        $name = [string]$case.name
        if ($name -notmatch '^(UvsOverhead|UvsCounter|CSharpOverhead|CSharpCounter)_(\d+)$') {
            continue
        }

        $agentKind = $Matches[1]
        $objectCount = [int]$Matches[2]
        $output = ""
        if ($case.output) {
            $output = [string]$case.output
        }

        $sampleGroup = $null
        $version = $null
        $source = $null
        if ($output -match '(UvsOverhead|UvsCounter|CSharpOverhead|CSharpCounter)_(\d+)_([A-Za-z]+)_(\S+)') {
            $sampleGroup = $Matches[0]
            $source = $Matches[3]
            $version = $Matches[4]
        }

        $median = $null
        $avg = $null
        $min = $null
        $max = $null

        if ($output -match 'Median:\s*([\d.]+)') {
            $median = [double]$Matches[1]
        }
        if ($output -match 'Avg:\s*([\d.]+)') {
            $avg = [double]$Matches[1]
        }
        if ($output -match 'Min:\s*([\d.]+)') {
            $min = [double]$Matches[1]
        }
        if ($output -match 'Max:\s*([\d.]+)') {
            $max = [double]$Matches[1]
        }

        $rows.Add([pscustomobject]@{
            TestName    = $name
            AgentKind   = $agentKind
            ObjectCount = $objectCount
            SampleGroup = $sampleGroup
            Version     = $version
            Source      = $source
            MedianMs    = $median
            AvgMs       = $avg
            MinMs       = $min
            MaxMs       = $max
            Result      = [string]$case.result
            DurationMs  = if ($case.duration) { [double]$case.duration } else { $null }
        })
    }

    if ($rows.Count -eq 0) {
        throw "No UVS benchmark test cases parsed from: $ResultsXmlPath"
    }

    return [pscustomobject]@{
        ResultsPath = (Resolve-Path $ResultsXmlPath).Path
        FileName    = Split-Path $ResultsXmlPath -Leaf
        RunAt       = (Get-Item $ResultsXmlPath).LastWriteTimeUtc
        Tests       = $rows
        Version     = ($rows | Where-Object { $_.Version } | Select-Object -First 1).Version
        Source      = ($rows | Where-Object { $_.Source } | Select-Object -First 1).Source
    }
}

function Format-UvsPlayModeReportMarkdown {
    param(
        [Parameter(Mandatory = $true)]
        $Run
    )

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("# UVS Play Mode benchmark report")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("| Field | Value |")
    [void]$sb.AppendLine("|-------|-------|")
    [void]$sb.AppendLine("| Results file | ``$($Run.FileName)`` |")
    [void]$sb.AppendLine("| Path | ``$($Run.ResultsPath)`` |")
    [void]$sb.AppendLine("| Run (UTC) | $($Run.RunAt.ToString('yyyy-MM-dd HH:mm:ss')) |")
    if ($Run.Version) { [void]$sb.AppendLine("| UVS version | $($Run.Version) |") }
    if ($Run.Source) { [void]$sb.AppendLine("| UVS source | $($Run.Source) |") }
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("Frame times from ``Measure.Frames()`` (median / avg in milliseconds).")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("## All tests")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("| Test | N | Median (ms) | Avg (ms) | Min | Max | Result |")
    [void]$sb.AppendLine("|------|---|-------------|----------|-----|-----|--------|")

    foreach ($t in ($Run.Tests | Sort-Object AgentKind, ObjectCount)) {
        $median = if ($null -ne $t.MedianMs) { "{0:N2}" -f $t.MedianMs } else { "—" }
        $avg = if ($null -ne $t.AvgMs) { "{0:N2}" -f $t.AvgMs } else { "—" }
        $min = if ($null -ne $t.MinMs) { "{0:N2}" -f $t.MinMs } else { "—" }
        $max = if ($null -ne $t.MaxMs) { "{0:N2}" -f $t.MaxMs } else { "—" }
        [void]$sb.AppendLine("| $($t.TestName) | $($t.ObjectCount) | $median | $avg | $min | $max | $($t.Result) |")
    }

    [void]$sb.AppendLine()
    [void]$sb.AppendLine("## UVS vs C# (median ms)")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("| Workload | N | UVS | C# | UVS/C# |")
    [void]$sb.AppendLine("|----------|---|-----|----|--------|")

    $counts = $Run.Tests.ObjectCount | Sort-Object -Unique
    foreach ($n in $counts) {
        foreach ($pair in @(
                @{ Uvs = "UvsOverhead"; CSharp = "CSharpOverhead" },
                @{ Uvs = "UvsCounter"; CSharp = "CSharpCounter" }
            )) {
            $uvs = $Run.Tests | Where-Object { $_.AgentKind -eq $pair.Uvs -and $_.ObjectCount -eq $n } | Select-Object -First 1
            $cs = $Run.Tests | Where-Object { $_.AgentKind -eq $pair.CSharp -and $_.ObjectCount -eq $n } | Select-Object -First 1
            if ($null -eq $uvs -or $null -eq $cs -or $null -eq $uvs.MedianMs -or $null -eq $cs.MedianMs -or $cs.MedianMs -eq 0) {
                continue
            }
            $ratio = $uvs.MedianMs / $cs.MedianMs
            [void]$sb.AppendLine("| $($pair.Uvs) | $n | $("{0:N2}" -f $uvs.MedianMs) | $("{0:N2}" -f $cs.MedianMs) | $("{0:N2}" -f $ratio)x |")
        }
    }

    return $sb.ToString()
}

function Format-UvsPlayModeCompareMarkdown {
    param(
        [Parameter(Mandatory = $true)]
        $Baseline,
        [Parameter(Mandatory = $true)]
        $Candidate
    )

    $sb = New-Object System.Text.StringBuilder
    $baselineLabel = if ($Baseline.Source) { "$($Baseline.Source) ($($Baseline.Version))" } else { "baseline" }
    $candidateLabel = if ($Candidate.Source) { "$($Candidate.Source) ($($Candidate.Version))" } else { "candidate" }
    [void]$sb.AppendLine("# UVS Play Mode comparison: $baselineLabel vs $candidateLabel")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("| Leg | File | UVS version | UVS source |")
    [void]$sb.AppendLine("|-----|------|-------------|------------|")
    [void]$sb.AppendLine("| Baseline | ``$($Baseline.FileName)`` | $($Baseline.Version) | $($Baseline.Source) |")
    [void]$sb.AppendLine("| Candidate | ``$($Candidate.FileName)`` | $($Candidate.Version) | $($Candidate.Source) |")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("**Delta %** = percent change in median frame time (candidate vs baseline). Negative is faster.")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("## UVS tests (median ms)")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("| Test | N | Baseline | Candidate | Δ ms | Δ % |")
    [void]$sb.AppendLine("|------|---|----------|-----------|------|-----|")

    $uvsKinds = @("UvsOverhead", "UvsCounter")
    foreach ($kind in $uvsKinds) {
        $baseTests = $Baseline.Tests | Where-Object { $_.AgentKind -eq $kind }
        foreach ($bt in ($baseTests | Sort-Object ObjectCount)) {
            $ct = $Candidate.Tests | Where-Object {
                $_.AgentKind -eq $kind -and $_.ObjectCount -eq $bt.ObjectCount
            } | Select-Object -First 1

            if ($null -eq $ct -or $null -eq $bt.MedianMs -or $null -eq $ct.MedianMs) {
                continue
            }

            $delta = $ct.MedianMs - $bt.MedianMs
            $deltaPct = if ($bt.MedianMs -ne 0) { ($delta / $bt.MedianMs) * 100.0 } else { 0 }
            [void]$sb.AppendLine("| $kind | $($bt.ObjectCount) | $("{0:N2}" -f $bt.MedianMs) | $("{0:N2}" -f $ct.MedianMs) | $("{0:N2}" -f $delta) | $("{0:N1}" -f $deltaPct)% |")
        }
    }

    [void]$sb.AppendLine()
    [void]$sb.AppendLine("## C# sanity check (should stay similar)")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("| Test | N | Baseline | Candidate | Δ % |")
    [void]$sb.AppendLine("|------|---|----------|-----------|-----|")

    $csKinds = @("CSharpOverhead", "CSharpCounter")
    foreach ($kind in $csKinds) {
        $baseTests = $Baseline.Tests | Where-Object { $_.AgentKind -eq $kind }
        foreach ($bt in ($baseTests | Sort-Object ObjectCount)) {
            $ct = $Candidate.Tests | Where-Object {
                $_.AgentKind -eq $kind -and $_.ObjectCount -eq $bt.ObjectCount
            } | Select-Object -First 1

            if ($null -eq $ct -or $null -eq $bt.MedianMs -or $null -eq $ct.MedianMs -or $bt.MedianMs -eq 0) {
                continue
            }

            $deltaPct = (($ct.MedianMs - $bt.MedianMs) / $bt.MedianMs) * 100.0
            [void]$sb.AppendLine("| $kind | $($bt.ObjectCount) | $("{0:N2}" -f $bt.MedianMs) | $("{0:N2}" -f $ct.MedianMs) | $("{0:N1}" -f $deltaPct)% |")
        }
    }

    [void]$sb.AppendLine()
    [void]$sb.AppendLine("## UVS/C# ratio change (median)")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("| Workload | N | Baseline ratio | Candidate ratio | Δ ratio |")
    [void]$sb.AppendLine("|----------|---|----------------|-----------------|---------|")

    $counts = $Baseline.Tests.ObjectCount | Sort-Object -Unique
    foreach ($n in $counts) {
        foreach ($pair in @(
                @{ Uvs = "UvsOverhead"; CSharp = "CSharpOverhead" },
                @{ Uvs = "UvsCounter"; CSharp = "CSharpCounter" }
            )) {
            $bUvs = $Baseline.Tests | Where-Object { $_.AgentKind -eq $pair.Uvs -and $_.ObjectCount -eq $n } | Select-Object -First 1
            $bCs = $Baseline.Tests | Where-Object { $_.AgentKind -eq $pair.CSharp -and $_.ObjectCount -eq $n } | Select-Object -First 1
            $cUvs = $Candidate.Tests | Where-Object { $_.AgentKind -eq $pair.Uvs -and $_.ObjectCount -eq $n } | Select-Object -First 1
            $cCs = $Candidate.Tests | Where-Object { $_.AgentKind -eq $pair.CSharp -and $_.ObjectCount -eq $n } | Select-Object -First 1

            if ($null -eq $bUvs -or $null -eq $bCs -or $null -eq $cUvs -or $null -eq $cCs) { continue }
            if ($null -eq $bUvs.MedianMs -or $null -eq $bCs.MedianMs -or $bCs.MedianMs -eq 0) { continue }
            if ($null -eq $cUvs.MedianMs -or $null -eq $cCs.MedianMs -or $cCs.MedianMs -eq 0) { continue }

            $bRatio = $bUvs.MedianMs / $bCs.MedianMs
            $cRatio = $cUvs.MedianMs / $cCs.MedianMs
            $ratioDelta = $cRatio - $bRatio
            [void]$sb.AppendLine("| $($pair.Uvs) | $n | $("{0:N2}" -f $bRatio)x | $("{0:N2}" -f $cRatio)x | $("{0:N2}" -f $ratioDelta)x |")
        }
    }

    return $sb.ToString()
}
