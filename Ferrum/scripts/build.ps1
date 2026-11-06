#requires -Version 7.0
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug', [switch]$TestHooks)
. "$PSScriptRoot/common.ps1"
Push-Location $RepoRoot
try {
    Initialize-Toolchain
    $release = Get-Content -LiteralPath "$RepoRoot/release.json" -Raw | ConvertFrom-Json
    $flavor = if ($TestHooks) { 'test-hooks' } else { 'production' }
    $targetDir = Join-Path $RepoRoot "artifacts/cargo/$flavor"
    $cargoArgs = @('build', '--locked', '--target', $RustTarget, '--target-dir', $targetDir)
    if ($Configuration -eq 'Release') { $cargoArgs += '--release' }
    if ($TestHooks) { $cargoArgs += @('--features', 'test-hooks') }
    Invoke-Checked cargo $cargoArgs
    Invoke-Checked dotnet @('build', 'Ferrum.slnx', '-c', $Configuration, '-p:Platform=x64', "-p:Version=$($release.version)", '--nologo')
    if (-not $TestHooks) { Invoke-Checked python @('scripts/package.py', '--configuration', $Configuration, '--platform', $Rid) }
} finally { Pop-Location }
