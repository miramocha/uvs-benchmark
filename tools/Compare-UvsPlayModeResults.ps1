param(
    [string] $BaselineXml,
    [string] $CandidateXml,

    [string] $BaselineSource = "Registry",
    [string] $CandidateSource = "EnhancedGit",

    [string] $ResultsDir = (Join-Path $PSScriptRoot "..\TestResults"),

    [string] $OutputMarkdown
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "UvsPlayModeResults.Parser.ps1")

$ResultsDir = (Resolve-Path $ResultsDir -ErrorAction SilentlyContinue).Path
if (-not $ResultsDir) {
    throw "Results directory not found: $ResultsDir. Run benchmark CLI first."
}

function Resolve-LatestResultsXml {
    param([string] $Dir, [string] $Source)

    $file = Get-UvsPlayModeResultFiles -ResultsDir $Dir -Source $Source | Select-Object -First 1
    if ($null -eq $file) {
        throw "No playmode-$Source-*.xml under $Dir"
    }
    return $file.FullName
}

if ([string]::IsNullOrWhiteSpace($BaselineXml)) {
    $BaselineXml = Resolve-LatestResultsXml -Dir $ResultsDir -Source $BaselineSource
}

if ([string]::IsNullOrWhiteSpace($CandidateXml)) {
    $CandidateXml = Resolve-LatestResultsXml -Dir $ResultsDir -Source $CandidateSource
}

$baseline = Parse-UvsPlayModeResultsXml -ResultsXmlPath $BaselineXml
$candidate = Parse-UvsPlayModeResultsXml -ResultsXmlPath $CandidateXml
$markdown = Format-UvsPlayModeCompareMarkdown -Baseline $baseline -Candidate $candidate

if ([string]::IsNullOrWhiteSpace($OutputMarkdown)) {
    $OutputMarkdown = Join-Path $ResultsDir "compare-$BaselineSource-vs-$CandidateSource-$(Get-Date -Format 'yyyyMMdd-HHmmss').md"
}

Set-Content -Path $OutputMarkdown -Value $markdown -Encoding UTF8

Write-Host $markdown
Write-Host ""
Write-Host "Comparison written: $OutputMarkdown"
