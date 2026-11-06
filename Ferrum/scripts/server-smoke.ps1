# Historical standalone checks; not executed by CI or release workflows. See docs/verification.md.
#requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$ServerPath,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [ValidateRange(1024, 65535)][int]$Port = 7788,
    [ValidateRange(15, 180)][int]$TimeoutSeconds = 90
)
. "$PSScriptRoot/common.ps1"
if (-not $IsWindows) { throw 'This automated dedicated-server check currently targets Windows only. See docs/development.md for Linux.' }
if (-not (Test-Path -LiteralPath (Join-Path $ServerPath 'LocalAdmin.exe'))) { throw 'ServerPath must contain LocalAdmin.exe.' }
$package = Join-Path $RepoRoot "artifacts/package/win-x64/$Configuration/LabAPI"
$ferrumPackage = Join-Path $RepoRoot "artifacts/package/win-x64/$Configuration/.config/Ferrum"
if (-not (Test-Path -LiteralPath (Join-Path $package 'plugins/global/Ferrum.Loader.dll'))) { throw 'Build the runtime package first.' }
if (-not (Test-Path -LiteralPath (Join-Path $ferrumPackage 'Plugins/Dependencies/ferrum_native.dll'))) { throw 'Build the new .config/Ferrum runtime package first.' }
$run = Join-Path $RepoRoot ('artifacts/server-smoke/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
$clone = Join-Path $run 'server'
if (-not ([IO.Path]::GetFullPath($clone).StartsWith([IO.Path]::GetFullPath((Join-Path $RepoRoot 'artifacts/server-smoke')) + [IO.Path]::DirectorySeparatorChar))) {
    throw 'Unexpected smoke-test destination.'
}
New-Item -ItemType Directory -Force -Path $clone | Out-Null
& robocopy $ServerPath $clone /E /XD AppData /XF '*.zip' hoster_policy.txt /R:1 /W:1 /NFL /NDL /NJH /NJS /NP
if ($LASTEXITCODE -ge 8) { throw "Server clone failed: robocopy $LASTEXITCODE" }
# A fresh private working directory and hoster policy keep all game/LabAPI data
# separate from the installed server and from the user's normal roaming config.
Set-Content -LiteralPath (Join-Path $clone 'hoster_policy.txt') -Value 'gamedir_for_configs: true' -Encoding utf8NoBOM
$labApi = Join-Path $clone 'AppData/SCP Secret Laboratory/LabAPI'
New-Item -ItemType Directory -Force -Path $labApi | Out-Null
Get-ChildItem -LiteralPath $package | Copy-Item -Destination $labApi -Recurse -Force
$ferrumRoot = Join-Path $clone '.config/Ferrum'
New-Item -ItemType Directory -Force -Path $ferrumRoot | Out-Null
Get-ChildItem -LiteralPath $ferrumPackage | Copy-Item -Destination $ferrumRoot -Recurse -Force
$config = Join-Path $clone "AppData/config/$Port"
New-Item -ItemType Directory -Force -Path $config | Out-Null
$consentPath = Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'SCP Secret Laboratory/config/localadmin_internal_data.json'
if (-not (Test-Path -LiteralPath $consentPath)) { throw 'LocalAdmin must already have an accepted EULA before unattended server smoke.' }
$consent = Get-Content -LiteralPath $consentPath -Raw | ConvertFrom-Json
if (-not $consent.EulaAccepted) { throw 'LocalAdmin has no prior EULA acceptance record; complete its normal first-run setup first.' }
# Reuse only the existing acceptance timestamp, never copy tokens or other server state.
@{ EulaAccepted = $consent.EulaAccepted } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path (Split-Path -Parent $config) 'localadmin_internal_data.json') -Encoding utf8NoBOM
@'
restart_on_crash: false
enable_heartbeat: false
set_terminal_title: false
enable_true_color: false
la_no_set_cursor: true
'@ | Set-Content -LiteralPath (Join-Path $config 'config_localadmin.txt') -Encoding utf8NoBOM
@'
server_name: Ferrum isolated PoC
server_ip: 127.0.0.1
ipv4_bind_ip: 127.0.0.1
ipv6_bind_ip: ::1
online_mode: false
forward_ports: false
enable_query: false
max_players: 1
'@ | Set-Content -LiteralPath (Join-Path $config 'config_gameplay.txt') -Encoding utf8NoBOM

$info = [Diagnostics.ProcessStartInfo]::new()
$info.FileName = Join-Path $clone 'LocalAdmin.exe'
$info.WorkingDirectory = $clone
$info.UseShellExecute = $false
$info.CreateNoWindow = $true
$info.RedirectStandardInput = $true
$info.RedirectStandardOutput = $true
$info.RedirectStandardError = $true
$info.Environment['FERRUM_HOME'] = $ferrumRoot
# LocalAdmin's Windows console writes in the host OEM codepage when redirected.
# Decode those bytes correctly; the bridge verifies Unicode before console rendering.
$info.StandardOutputEncoding = [Text.Encoding]::GetEncoding([Globalization.CultureInfo]::CurrentCulture.TextInfo.OEMCodePage)
$info.StandardErrorEncoding = $info.StandardOutputEncoding
foreach ($argument in @("$Port", '--skipHomeCheck', '--noSetCursor', '--disableTrueColor', '--noTerminalTitle', '--useDefault', '--', '-logFile', (Join-Path $run 'unity.log'))) {
    $info.ArgumentList.Add($argument)
}
$process = [Diagnostics.Process]::new()
$process.StartInfo = $info
$lines = [Collections.Generic.List[string]]::new()
$log = Join-Path $run 'console.log'
$passed = $false
$started = $false
$errors = $null
try {
    if (-not $process.Start()) { throw 'Could not start isolated LocalAdmin.' }
    $started = $true
    $errors = $process.StandardError.ReadToEndAsync()
    $next = $process.StandardOutput.ReadLineAsync()
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($next.IsCompleted) {
            $line = $next.GetAwaiter().GetResult()
            if ($null -eq $line) { break }
            $lines.Add($line)
            [IO.File]::AppendAllText($log, $line + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
            if ($line.Contains('[Ferrum]') -or $line.StartsWith('Branch:') -or $line.Contains('Contributors:')) { Write-Output $line }
            if ($line.Contains('Contributors:') -and $lines.Exists([Predicate[string]]{param($s) $s.Contains('Thank you for using and supporting the Ferrum framework!')}) -and $lines.Exists([Predicate[string]]{param($s) $s.Contains('Branch:') -and $s.Contains('Version:')})) {
                $passed = $true
                break
            }
            $next = $process.StandardOutput.ReadLineAsync()
        } else { Start-Sleep -Milliseconds 100 }
    }
} finally {
    if ($started -and -not $process.HasExited) {
        try { $process.StandardInput.WriteLine('exit'); $process.StandardInput.Flush() } catch { }
        if (-not $process.WaitForExit(10000)) { $process.Kill($true); $process.WaitForExit() }
    }
    $lines | Set-Content -LiteralPath $log -Encoding utf8NoBOM
    if ($null -ne $errors -and $errors.IsCompleted) {
        $errors.GetAwaiter().GetResult() | Set-Content -LiteralPath (Join-Path $run 'stderr.log') -Encoding utf8NoBOM
    }
    $process.Dispose()
}
if (-not $passed) { throw "Ferrum startup banner not found after plugin initialization. Inspect $log and $(Join-Path $run 'unity.log')." }
Write-Output "PASS: isolated Ferrum startup completed after plugin initialization. Evidence: $run"
