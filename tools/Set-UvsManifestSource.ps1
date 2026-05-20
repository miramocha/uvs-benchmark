param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Registry", "EnhancedGit", "StableGit")]
    [string] $Source
)

$ErrorActionPreference = "Stop"

$manifestPath = Join-Path $PSScriptRoot "..\Projects\UvsBenchmarkHost\Packages\manifest.json"
if (-not (Test-Path $manifestPath)) {
    throw "Manifest not found: $manifestPath"
}

$values = @{
    Registry    = "1.9.11"
    EnhancedGit = "https://github.com/miramocha/uvs-community-performance-optimization.git#enhanced"
    StableGit   = "https://github.com/miramocha/uvs-community-performance-optimization.git#stable"
}

$newValue = $values[$Source]
$manifestText = Get-Content $manifestPath -Raw

$pattern = '"com\.unity\.visualscripting"\s*:\s*"[^"]*"'
if (-not [regex]::IsMatch($manifestText, $pattern)) {
    throw "Could not find com.unity.visualscripting entry in manifest.json"
}

$replacement = '"com.unity.visualscripting": "' + $newValue + '"'
$updated = [regex]::Replace($manifestText, $pattern, $replacement, 1)

Set-Content $manifestPath $updated -Encoding UTF8 -NoNewline

Write-Host "Set com.unity.visualscripting to ${Source}:"
Write-Host "  $newValue"
Write-Host "Reopen Projects/UvsBenchmarkHost in Unity (or Package Manager refresh) to resolve packages."
