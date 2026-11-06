# Historical standalone checks; not executed by CI or release workflows. See docs/verification.md.
#requires -Version 7.0
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug')
. "$PSScriptRoot/common.ps1"
Push-Location $RepoRoot
try {
    Initialize-Toolchain
    Invoke-Checked cargo @('fmt', '--check')
    Invoke-Checked cargo @('clippy', '--locked', '--all-targets', '--all-features', '--target-dir', 'artifacts/cargo/checks', '--', '-D', 'warnings')
    Invoke-Checked cargo @('test', '--locked', '--all-features', '--target-dir', 'artifacts/cargo/checks', '--', '--test-threads=1')
    & "$PSScriptRoot/build.ps1" -Configuration $Configuration
    & "$PSScriptRoot/build.ps1" -Configuration $Configuration -TestHooks
    $fixtures = Join-Path $RepoRoot "artifacts/fixtures/$Rid/$Configuration"
    New-Item -ItemType Directory -Force -Path $fixtures | Out-Null
    $extension = if ($IsWindows) { '.dll' } else { '.so' }
    foreach ($fixture in @('missing', 'abi')) {
        $fixtureArgs = @('tests/native-fixture.rs', '--edition', '2024', '--crate-type', 'cdylib', '--crate-name', "fixture_$fixture",
            '--target', $RustTarget, '-o', (Join-Path $fixtures "fixture_$fixture$extension"))
        if ($fixture -eq 'abi') { $fixtureArgs += @('--cfg', 'incompatible_abi') }
        Invoke-Checked rustc $fixtureArgs
    }
    $profile = $Configuration.ToLowerInvariant()
    $native = Join-Path $RepoRoot "artifacts/cargo/test-hooks/$RustTarget/$profile/$NativeFile"
    $production = Join-Path $RepoRoot "artifacts/cargo/production/$RustTarget/$profile/$NativeFile"
    $runner = Join-Path $RepoRoot "artifacts/managed/Ferrum.Interop.SmokeTests/$Configuration/Ferrum.Interop.SmokeTests.exe"
    if ($IsWindows) { Invoke-Checked $runner @($native, $fixtures, $production) }
    else {
        if (-not (Get-Command mono -ErrorAction SilentlyContinue)) { throw 'Install Mono to run the net48 Linux smoke tests.' }
        Invoke-Checked mono @($runner, $native, $fixtures, $production)
    }
} finally { Pop-Location }
