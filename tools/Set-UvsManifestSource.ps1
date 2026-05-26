param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Registry", "EnhancedGit", "StableGit", "LocalEmbedded")]
    [string] $Source
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

$manifestPath = Join-Path $PSScriptRoot "..\Projects\UvsBenchmarkHost\Packages\manifest.json"
if (-not (Test-Path $manifestPath)) {
    throw "Manifest not found: $manifestPath"
}

$values = @{
    Registry      = "1.9.11"
    EnhancedGit   = "https://github.com/miramocha/uvs-benchmark.git?path=/Packages/com.unity.visualscripting#enhanced"
    StableGit     = "https://github.com/miramocha/uvs-benchmark.git?path=/Packages/com.unity.visualscripting#stable"
    LocalEmbedded = "file:../../../Packages/com.unity.visualscripting"
}

$newValue = $values[$Source]
$manifestText = Get-Content $manifestPath -Raw

$pattern = '"com\.unity\.visualscripting"\s*:\s*"[^"]*"'
if (-not [regex]::IsMatch($manifestText, $pattern)) {
    throw "Could not find com.unity.visualscripting entry in manifest.json"
}

$replacement = '"com.unity.visualscripting": "' + $newValue + '"'
$updated = [regex]::Replace($manifestText, $pattern, $replacement, 1)

Set-Utf8FileNoBom -Path $manifestPath -Value $updated

Write-Host "Set com.unity.visualscripting to ${Source}:"
Write-Host "  $newValue"
Write-Host "Reopen Projects/UvsBenchmarkHost in Unity (or Package Manager refresh) to resolve packages."
