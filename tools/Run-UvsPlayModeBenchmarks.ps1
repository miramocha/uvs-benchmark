param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Registry", "EnhancedGit", "StableGit", "LocalEmbedded")]
    [string] $UvsSource,

    [string] $UnityPath = $env:UNITY_EDITOR,

    [string] $ProjectPath = (Join-Path $PSScriptRoot "..\Projects\UvsBenchmarkHost"),

    [string] $TestFilter = "",

    [string] $ResultsDir = (Join-Path $PSScriptRoot "..\TestResults"),

    [switch] $SkipPackageSwitch
)

$ErrorActionPreference = "Stop"

function Set-Utf8FileNoBom {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $Value
    )
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Value, $utf8)
}

function Ensure-VisualScriptingManifestEntry {
    param([string] $ManifestPath)

    $text = Get-Content $ManifestPath -Raw
    if ([regex]::IsMatch($text, '"com\.unity\.visualscripting"\s*:')) {
        return
    }

    $insert = '    "com.unity.visualscripting": "1.9.11",' + [Environment]::NewLine
    $updated = [regex]::Replace(
        $text,
        '("dependencies"\s*:\s*\{)',
        ('$1' + [Environment]::NewLine + $insert),
        1
    )

    if ($updated -eq $text) {
        throw "Could not insert com.unity.visualscripting into manifest.json"
    }

    Set-Utf8FileNoBom -Path $ManifestPath -Value $updated
    Write-Host "Added com.unity.visualscripting placeholder (1.9.11) to manifest.json"
}

function Resolve-UnityEditorPath {
    param([string] $PreferredPath)

    if (-not [string]::IsNullOrWhiteSpace($PreferredPath) -and (Test-Path $PreferredPath)) {
        return (Resolve-Path $PreferredPath).Path
    }

    $hubRoots = @(
        "${env:ProgramFiles}\Unity\Hub\Editor",
        "${env:ProgramFiles(x86)}\Unity\Hub\Editor"
    )

    foreach ($hub in $hubRoots) {
        if (-not (Test-Path $hub)) {
            continue
        }

        $match = Get-ChildItem $hub -Directory |
            Where-Object { $_.Name -like "2021.3.45f2*" } |
            Sort-Object Name -Descending |
            Select-Object -First 1

        if ($null -ne $match) {
            $candidate = Join-Path $match.FullName "Editor\Unity.exe"
            if (Test-Path $candidate) {
                return $candidate
            }
        }
    }

    throw @"
Unity Editor not found. Set UNITY_EDITOR to Unity.exe (2021.3.45f2), or install that version via Unity Hub.
Example:
  `$env:UNITY_EDITOR = 'C:\Program Files\Unity\Hub\Editor\2021.3.45f2\Editor\Unity.exe'
"@
}

$ProjectPath = (Resolve-Path $ProjectPath).Path
$manifestPath = Join-Path $ProjectPath "Packages\manifest.json"

if (-not (Test-Path $manifestPath)) {
    throw "Host manifest not found: $manifestPath"
}

Ensure-VisualScriptingManifestEntry -ManifestPath $manifestPath

if (-not $SkipPackageSwitch) {
    $switchScript = Join-Path $PSScriptRoot "Set-UvsManifestSource.ps1"
    & $switchScript -Source $UvsSource
}

$unityExe = Resolve-UnityEditorPath -PreferredPath $UnityPath
New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$resultsFile = Join-Path $ResultsDir "playmode-$UvsSource-$timestamp.xml"
$logFile = Join-Path $ResultsDir "unity-playmode-$UvsSource-$timestamp.log"

$unityArgs = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", "playmode",
    "-assemblyNames", "Miraluna.Uvs.Benchmarks.Tests",
    "-testResults", $resultsFile,
    "-logFile", $logFile
)

if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
    $unityArgs += @("-testFilter", $TestFilter)
}

Write-Host "Unity: $unityExe"
Write-Host "Project: $ProjectPath"
Write-Host "UVS source: $UvsSource"
Write-Host "Results: $resultsFile"

& $unityExe @unityArgs
$exitCode = $LASTEXITCODE

if ($null -ne $exitCode -and $exitCode -ne 0) {
    Write-Error "Unity test run failed with exit code $exitCode. See log: $logFile"
}

Write-Host "Play Mode benchmarks finished successfully."
Write-Host "NUnit results: $resultsFile"
Write-Host "Unity log: $logFile"
Write-Host "Performance reports (if generated): PerformanceTestResults/ under project or repo root"

exit $exitCode
