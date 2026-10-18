# DIST-06 installer smoke on Windows (CI runner or a tester's PC without WoTR installed): silent install of the
# release, self-check, a damaged package is refused without touching the installation, Update.exe applies the next
# package, self-check, player data unchanged, silent uninstall, player data still unchanged. Inputs from scripts/package_velopack.py: the release folder (installers/windows-x64)
# and the --smoke-output folder. Writes $Out\report.txt and ends with "PASS" or a "FAIL:" line.
param(
    [Parameter(Mandatory = $true)][string]$Release,
    [Parameter(Mandatory = $true)][string]$Next,
    [Parameter(Mandatory = $true)][string]$Out
)
$ErrorActionPreference = 'Stop'
$PackId = 'WotrOnline'
$Root = Join-Path $env:LOCALAPPDATA $PackId
$Exe = Join-Path $Root 'current\Wotr.exe'
$UpdateExe = Join-Path $Root 'Update.exe'
$VelopackLog = Join-Path $env:LOCALAPPDATA "velopack\velopack_$PackId.log"
$UnityLog = Join-Path $env:USERPROFILE 'AppData\LocalLow\WoTR Private\War of the Ring Online\Player.log'
$Shortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'War of the Ring Online.lnk'
$Report = Join-Path $Out 'report.txt'
$Logs = Join-Path $Out 'logs'

function Record([string]$Line) { Write-Host $Line; Add-Content -Path $Report -Value $Line -Encoding utf8 }
function Fail([string]$Line) { Record "FAIL: $Line"; Save-Logs; exit 1 }
function Save-Logs {
    New-Item -ItemType Directory -Force $Logs | Out-Null
    if (Test-Path $VelopackLog) { Copy-Item $VelopackLog $Logs }
}

function Wait-Exit([System.Diagnostics.Process]$Process, [string]$What, [int]$Seconds) {
    if (-not $Process.WaitForExit($Seconds * 1000)) { $Process.Kill(); Fail "$What did not finish within $Seconds s" }
    if ($Process.ExitCode -ne 0) { Fail "$What exit $($Process.ExitCode)" }
}

# Hook lines Update.exe logged since the previous call. Each hook it ran must have exited with 0 (a hook that exits
# before Update.exe starts waiting counts too: Velopack logs it and carries on).
$script:VelopackSeen = 0
function Check-Hooks([string]$Phase, [int]$Expected) {
    $lines = @(Get-Content $VelopackLog -ErrorAction SilentlyContinue)
    $new = @($lines | Select-Object -Skip $script:VelopackSeen | Where-Object { $_ -match 'veloapp|hook|timed out' })
    $script:VelopackSeen = $lines.Count
    Record "${Phase} hooks: $($new -join ' | ')"
    $ran = @($new | Where-Object { $_ -match 'Running --veloapp-' }).Count
    $ok = @($new | Where-Object { $_ -match 'Hook executed successfully|unable to wait for hook' }).Count
    if ($ran -ne $Expected -or $ok -ne $ran) { Fail "$Phase — expected $Expected hooks, ran $ran, succeeded $ok" }
}

# The installed game reports itself (InstallationSelfCheck) and quits before opening a window.
function Self-Check([string]$Name, [string]$Installed, [string]$Build) {
    $json = Join-Path $Out "self-check-$Name.json"
    # SEC-01: the game writes only a new report at an absolute path.
    Remove-Item -LiteralPath $json -ErrorAction SilentlyContinue
    $arguments = '-batchmode', '-nographics', '-logFile', "`"$(Join-Path $Out "player-$Name.log")`"", '--wotr-self-check', "`"$json`""
    Wait-Exit (Start-Process -FilePath $Exe -ArgumentList $arguments -PassThru) "self-check $Name" 120
    $check = Get-Content $json -Raw | ConvertFrom-Json
    Record "self-check ${Name}: $(Get-Content $json -Raw | ConvertFrom-Json | ConvertTo-Json -Compress)"
    if ($check.installedVersion -ne $Installed -or $check.buildVersion -ne $Build -or $check.platform -ne 'windows-x64' -or $check.processArchitecture -ne 'X64' -or
        $check.development -or -not $check.canUpdateItself -or $check.velopackStartup -ne 'ok') {
        Fail "self-check $Name — expected installed $Installed, build $Build, a release that can update itself"
    }
    return $check
}

# Player data outside the installation: every file with its SHA-256.
function Fingerprint([string]$Folder) {
    Get-ChildItem $Folder -Recurse -File | Sort-Object FullName | ForEach-Object {
        "$($_.FullName.Substring($Folder.Length)) $((Get-FileHash $_.FullName -Algorithm SHA256).Hash)"
    } | Out-String
}

$Out = (New-Item -ItemType Directory -Force $Out).FullName
Remove-Item $Report -ErrorAction SilentlyContinue
if (Test-Path $Root) { Fail "$Root exists — uninstall War of the Ring Online before the smoke test" }
$version = (Get-Content (Join-Path $Release 'build-info.json') -Raw | ConvertFrom-Json).version
$setup = (Get-ChildItem $Release -Filter "$PackId-$version-win-Setup.exe")[0].FullName
$nextPackage = (Get-ChildItem $Next -Filter "$PackId-*-full.nupkg")[0]
$nextVersion = $nextPackage.Name -replace "^$PackId-(.+)-full\.nupkg$", '$1'
Record "Windows $([Environment]::OSVersion.VersionString); release $version, next $nextVersion"

# Unity writes Player.log as soon as UnityMain runs; installation must not start the game.
$unityLogBefore = if (Test-Path $UnityLog) { (Get-Item $UnityLog).LastWriteTimeUtc } else { $null }
$install = Start-Process -FilePath $setup -ArgumentList '--silent', '--log', "`"$(Join-Path $Out 'setup.log')`"" -PassThru
Wait-Exit $install 'Setup.exe --silent' 300
if (-not (Test-Path $Exe)) { Fail "no $Exe after install" }
$hooks = Select-String -Path (Join-Path $Out 'setup.log') -Pattern 'Running --veloapp-|Hook executed successfully|Hook exited|timed out'
Record "install: $($hooks.Line -join ' | ')"
if (-not ($hooks.Line -match 'Hook executed successfully')) { Fail 'install hook did not succeed' }
$unityLogAfter = if (Test-Path $UnityLog) { (Get-Item $UnityLog).LastWriteTimeUtc } else { $null }
if ($unityLogAfter -ne $unityLogBefore) { Fail 'the game started during installation' }
if (Get-Process -Name Wotr -ErrorAction SilentlyContinue) { Fail 'Wotr.exe running after installation' }
if (-not (Test-Path $Shortcut)) { Fail "no desktop shortcut $Shortcut" }
Record "install: $Root, shortcuts on the desktop and in Start, the game did not start"
$script:VelopackSeen = @(Get-Content $VelopackLog -ErrorAction SilentlyContinue).Count

$data = (Self-Check 'release' $version $version).persistentDataPath -replace '/', '\'
New-Item -ItemType Directory -Force $data | Out-Null
Set-Content -Path (Join-Path $data 'installer-smoke-marker.txt') -Value ([guid]::NewGuid()) -Encoding utf8
$before = Fingerprint $data

# A damaged package (as after a broken transfer or a full disk) fails while Velopack extracts it, before any hook
# runs or the installation is backed up: the release keeps working.
$broken = Join-Path $Out "$PackId-broken-full.nupkg"
Copy-Item $nextPackage.FullName $broken
$stream = [IO.File]::Open($broken, 'Open')
$stream.SetLength([long]($stream.Length / 2))
$stream.Close()
$refused = Start-Process -FilePath $UpdateExe -ArgumentList 'apply', '--silent', '--norestart', '--package', "`"$broken`"" -PassThru
if (-not $refused.WaitForExit(300000)) { $refused.Kill(); Fail 'Update.exe apply of a damaged package hung' }
if ($refused.ExitCode -eq 0) { Fail 'Update.exe applied a damaged package' }
Check-Hooks 'damaged package' 0
Self-Check 'after-damaged' $version $version | Out-Null
Record "damaged package: Update.exe exit $($refused.ExitCode), no hook ran, $version still installed"

$apply = Start-Process -FilePath $UpdateExe -ArgumentList 'apply', '--silent', '--norestart', '--package', "`"$($nextPackage.FullName)`"" -PassThru
Wait-Exit $apply 'Update.exe apply' 300
Check-Hooks 'apply' 2
Self-Check 'next' $nextVersion $version | Out-Null
if ((Fingerprint $data) -ne $before) { Fail 'player data changed during the update' }
Record "update: $version -> $nextVersion, player data unchanged"

$uninstall = Start-Process -FilePath $UpdateExe -ArgumentList 'uninstall', '--silent' -PassThru
Wait-Exit $uninstall 'Update.exe uninstall' 300
Check-Hooks 'uninstall' 1
if (Test-Path (Join-Path $Root 'current')) { Fail 'installation left after uninstall' }
if (Test-Path $Shortcut) { Fail 'desktop shortcut left after uninstall' }
if ((Fingerprint $data) -ne $before) { Fail 'player data changed by uninstall' }
Record 'uninstall: installation and shortcuts removed, player data unchanged'
Save-Logs
Record 'PASS'
