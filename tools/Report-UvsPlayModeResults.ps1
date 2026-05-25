param(
    [string] $ResultsXml,

    [ValidateSet("Registry", "EnhancedGit", "StableGit", "LocalEmbedded")]
    [string] $Source,

    [string] $ResultsDir = (Join-Path $PSScriptRoot "..\TestResults"),

    [string] $OutputMarkdown
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "UvsPlayModeResults.Parser.ps1")

$ResultsDir = (Resolve-Path $ResultsDir -ErrorAction SilentlyContinue).Path
if (-not $ResultsDir) {
    $ResultsDir = (New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot "..\TestResults") -Force).FullName
}

if ([string]::IsNullOrWhiteSpace($ResultsXml)) {
    if ([string]::IsNullOrWhiteSpace($Source)) {
        throw "Specify -ResultsXml or -Source (Registry, EnhancedGit, StableGit, LocalEmbedded)."
    }

    $latest = Get-UvsPlayModeResultFiles -ResultsDir $ResultsDir -Source $Source | Select-Object -First 1
    if ($null -eq $latest) {
        throw "No results found under $ResultsDir matching playmode-$Source-*.xml. Run Run-UvsPlayModeBenchmarks.ps1 first."
    }

    $ResultsXml = $latest.FullName
}

$run = Parse-UvsPlayModeResultsXml -ResultsXmlPath $ResultsXml
$markdown = Format-UvsPlayModeReportMarkdown -Run $run

if ([string]::IsNullOrWhiteSpace($OutputMarkdown)) {
    $label = if ($Source) { $Source } else { "run" }
    $OutputMarkdown = Join-Path $ResultsDir "report-$label-$(Get-Date -Format 'yyyyMMdd-HHmmss').md"
}

Set-Content -Path $OutputMarkdown -Value $markdown -Encoding UTF8

Write-Host $markdown
Write-Host ""
Write-Host "Report written: $OutputMarkdown"
