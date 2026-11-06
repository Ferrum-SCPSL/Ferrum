#requires -Version 7.0
param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [switch]$TestHooks
)
. "$PSScriptRoot/common.ps1"
Push-Location $RepoRoot
try {
    Initialize-Toolchain
    $flavor = if ($TestHooks) { 'test-hooks' } else { 'production' }
    $targetDir = Join-Path $RepoRoot "artifacts/cargo/$flavor"
    $cargoArgs = @('build', '--locked', '--target', $RustTarget, '--target-dir', $targetDir)
    if ($Configuration -eq 'Release') { $cargoArgs += '--release' }
    if ($TestHooks) { $cargoArgs += @('--features', 'test-hooks') }
    Invoke-Checked cargo $cargoArgs
    Invoke-Checked dotnet @('build', 'Ferrum.slnx', '-c', $Configuration, '-p:Platform=x64', '--nologo')
    if ($TestHooks) {
        Write-Output "Test-hooks native build ready: $targetDir"
        return
    }
    $package = Join-Path $RepoRoot "artifacts/package/$Rid/$Configuration"
    $labApi = Join-Path $package 'LabAPI'
    $ferrum = Join-Path $package '.config/Ferrum'
    $plugins = Join-Path $labApi 'plugins/global'
    $dependencies = Join-Path $labApi 'dependencies/global'
    $native = Join-Path $ferrum 'Plugins/Dependencies'
    $configs = Join-Path $ferrum 'Configs'
    $logs = Join-Path $ferrum 'Logs'
    New-Item -ItemType Directory -Force -Path $plugins, $dependencies, $native, $configs, $logs | Out-Null
    # Remove only the previous generated Ferrum native location when rebuilding an old package.
    $legacy = [IO.Path]::GetFullPath((Join-Path $labApi 'native/Ferrum'))
    $artifactRoot = [IO.Path]::GetFullPath((Join-Path $RepoRoot 'artifacts/package')) + [IO.Path]::DirectorySeparatorChar
    if (-not $legacy.StartsWith($artifactRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Unexpected legacy package directory.'
    }
    if (Test-Path -LiteralPath $legacy) { Remove-Item -LiteralPath $legacy -Recurse -Force }
    foreach ($name in @('dll', 'pdb')) {
        Copy-Item -LiteralPath (Join-Path $RepoRoot "artifacts/managed/Ferrum.Loader/$Configuration/Ferrum.Loader.$name") -Destination $plugins
        Copy-Item -LiteralPath (Join-Path $RepoRoot "artifacts/managed/Ferrum.Interop/$Configuration/Ferrum.Interop.$name") -Destination $dependencies
    }
    $profile = $Configuration.ToLowerInvariant()
    Copy-Item -LiteralPath (Join-Path $targetDir "$RustTarget/$profile/$NativeFile") -Destination $native
    if ($IsWindows) {
        $symbols = Join-Path $targetDir "$RustTarget/$profile/ferrum_native.pdb"
        if (Test-Path -LiteralPath $symbols) { Copy-Item -LiteralPath $symbols -Destination $native }
    }
    Write-Output "Runtime package: $package"
} finally { Pop-Location }
