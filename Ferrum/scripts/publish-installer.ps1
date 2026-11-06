#requires -Version 7.0
param([ValidateSet('win-x64', 'linux-x64')][string]$Platform)
. "$PSScriptRoot/common.ps1"
$release = Get-Content -LiteralPath "$RepoRoot/release.json" -Raw | ConvertFrom-Json
$output = Join-Path $RepoRoot "artifacts/installer/$Platform"
Invoke-Checked dotnet @('publish', "$RepoRoot/Ferrum.Installer/Ferrum.Installer.csproj", '-c', 'Release', '-r', $Platform, '--self-contained', 'true', "-p:Version=$($release.version)", '-o', $output, '--nologo')
