Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:RepoRoot = Split-Path -Parent $PSScriptRoot

function Invoke-Checked {
    param([string]$Command, [string[]]$Arguments)
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command exited with code $LASTEXITCODE" }
}

function Initialize-Toolchain {
    $cargoBin = if ($env:CARGO_HOME) { Join-Path $env:CARGO_HOME 'bin' } elseif ($IsWindows) {
        Join-Path $env:USERPROFILE '.cargo/bin'
    } else { Join-Path $env:HOME '.cargo/bin' }
    if (Test-Path -LiteralPath $cargoBin) { $env:PATH = "$cargoBin$([IO.Path]::PathSeparator)$env:PATH" }
    foreach ($tool in @('dotnet', 'cargo', 'rustc')) {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "Missing prerequisite: $tool. See docs/development.md." }
    }
    if ($IsWindows -and -not (Get-Command link.exe -ErrorAction SilentlyContinue)) {
        $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
        $vsRoot = $null
        $candidates = @()
        if ($env:FERRUM_VS_INSTALL) { $candidates += $env:FERRUM_VS_INSTALL }
        if (Test-Path -LiteralPath $vswhere) {
            $candidates += @(& $vswhere -all -prerelease -products '*' -property installationPath)
        }
        foreach ($candidate in $candidates) {
            if (Test-Path -LiteralPath (Join-Path $candidate 'VC/Auxiliary/Build/vcvarsall.bat')) {
                $vsRoot = $candidate
                break
            }
        }
        if (-not $vsRoot) { throw 'MSVC x64 tools not found. Install Desktop development with C++ and a Windows SDK.' }
        $vcvars = Join-Path $vsRoot 'VC/Auxiliary/Build/vcvars64.bat'
        # Import the compiler environment only into this process; do not change global PATH.
        $environment = & $env:COMSPEC /d /c "call `"$vcvars`" >nul && set"
        if ($LASTEXITCODE -ne 0) { throw 'vcvars64.bat failed.' }
        foreach ($line in $environment) {
            if ($line -match '^([^=]+)=(.*)$') { [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], 'Process') }
        }
        if (-not (Get-Command link.exe -ErrorAction SilentlyContinue)) { throw 'MSVC linker not available after vcvars64.' }
    }
    $script:Rid = if ($IsWindows) { 'win-x64' } elseif ($IsLinux) { 'linux-x64' } else { throw 'Windows/Linux x64 required.' }
    $script:RustTarget = if ($IsWindows) { 'x86_64-pc-windows-msvc' } else { 'x86_64-unknown-linux-gnu' }
    $script:NativeFile = if ($IsWindows) { 'ferrum_native.dll' } else { 'libferrum_native.so' }
}
