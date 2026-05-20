param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Registry", "EnhancedGit")]
    [string] $Source
)

$ErrorActionPreference = "Stop"

$manifestPath = Join-Path $PSScriptRoot "..\Projects\UvsBenchmarkHost\Packages\manifest.json"
if (-not (Test-Path $manifestPath)) {
    throw "Manifest not found: $manifestPath"
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

$registryValue = "1.9.11"
$enhancedGitValue = "https://github.com/miramocha/unity-visual-scripting-enhancements.git?path=/Packages/com.unity.visualscripting#enhanced"

switch ($Source) {
    "Registry" { $manifest.dependencies."com.unity.visualscripting" = $registryValue }
    "EnhancedGit" { $manifest.dependencies."com.unity.visualscripting" = $enhancedGitValue }
}

$manifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath -Encoding UTF8

Write-Host "Set com.unity.visualscripting to $Source:"
Write-Host "  $($manifest.dependencies.'com.unity.visualscripting')"
Write-Host "Reopen Projects/UvsBenchmarkHost in Unity to refresh packages."
