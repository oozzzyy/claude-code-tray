# Claude Code Tray setup: builds the tray, adds sounds and hooks, turns on autostart and starts it.
# Safe to run again. To remove everything: install.cmd -Uninstall
param(
    [switch]$Uninstall,
    [switch]$NoStart,
    [switch]$NoAutostart,
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'ClaudeCodeTray'),
    [string]$ClaudeHome = $(if ($env:CLAUDE_CONFIG_DIR) { $env:CLAUDE_CONFIG_DIR } else { Join-Path $HOME '.claude' })
)
$ErrorActionPreference = 'Stop'

$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$runName = 'ClaudeTray'
$exe = Join-Path $InstallDir 'ClaudeTray.exe'
$settingsPath = Join-Path $ClaudeHome 'settings.json'
$soundsDir = Join-Path $ClaudeHome 'sounds'

function Step($text) { Write-Host "  $text" }

function Read-Settings {
    if (-not (Test-Path -LiteralPath $settingsPath)) { return [pscustomobject]@{} }
    $text = [IO.File]::ReadAllText($settingsPath)
    if (-not $text.Trim()) { return [pscustomobject]@{} }
    try { return ConvertFrom-Json -InputObject $text }
    catch { throw "$settingsPath is not valid JSON, so it was left unchanged. Fix it and run setup again." }
}

function Write-Settings($settings) {
    # Windows PowerShell wraps some arrays in extra objects unless this type data is removed.
    Remove-TypeData System.Array -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $ClaudeHome | Out-Null
    $backup = "$settingsPath.before-tray"
    if ((Test-Path -LiteralPath $settingsPath) -and -not (Test-Path -LiteralPath $backup)) {
        Copy-Item -LiteralPath $settingsPath -Destination $backup
    }
    $json = ConvertTo-Json -InputObject $settings -Depth 100
    [IO.File]::WriteAllText($settingsPath, $json, (New-Object Text.UTF8Encoding($false)))
}

function Sound-Hook($file) {
    $path = (Join-Path $soundsDir $file).Replace("'", "''")
    [pscustomobject]@{
        type = 'command'; command = 'powershell.exe'
        args = @('-NoProfile', '-NonInteractive', '-Command', "(New-Object Media.SoundPlayer '$path').PlaySync()")
        timeout = 15; async = $true
    }
}

function Signal-Hook($kind) {
    [pscustomobject]@{ type = 'command'; command = $exe; args = @('--signal', $kind); timeout = 15; async = $true }
}

# Adds a hook group unless the event already has one that mentions the marker.
function Add-HookGroup($settings, $event, $group, $marker) {
    if (-not $settings.PSObject.Properties['hooks']) {
        $settings | Add-Member -NotePropertyName hooks -NotePropertyValue ([pscustomobject]@{})
    }
    $list = @()
    if ($settings.hooks.PSObject.Properties[$event]) { $list = @($settings.hooks.$event) }
    if ((ConvertTo-Json -InputObject $list -Depth 100).Contains($marker)) { return }
    $settings.hooks | Add-Member -NotePropertyName $event -NotePropertyValue (@($list) + $group) -Force
}

# True for the hooks this script adds, and for nothing else.
function Is-OwnHook($hook) {
    if ($hook.command -eq $exe) { return $true }
    if ($hook.command -ne 'powershell.exe' -or -not $hook.args) { return $false }
    $own = @('coin-quiet.wav', 'notify-quiet.wav') | ForEach-Object { (Sound-Hook $_).args[3] }
    return $own -contains $hook.args[-1]
}

function Remove-OwnHooks($settings) {
    if (-not $settings.PSObject.Properties['hooks']) { return }
    foreach ($prop in @($settings.hooks.PSObject.Properties)) {
        $kept = @()
        foreach ($group in @($prop.Value)) {
            $rest = @(@($group.hooks) | Where-Object { -not (Is-OwnHook $_) })
            if ($rest.Count -eq 0) { continue }
            $group.hooks = $rest
            $kept += $group
        }
        if ($kept.Count -eq 0) { $settings.hooks.PSObject.Properties.Remove($prop.Name) }
        else { $settings.hooks | Add-Member -NotePropertyName $prop.Name -NotePropertyValue $kept -Force }
    }
    if (@($settings.hooks.PSObject.Properties).Count -eq 0) { $settings.PSObject.Properties.Remove('hooks') }
}

function Stop-InstalledTray {
    $running = @(Get-Process ClaudeTray -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe })
    $running | Stop-Process -Force
    $running | ForEach-Object { $_.WaitForExit(5000) | Out-Null }
}

function Find-Claude {
    $native = Join-Path $HOME '.local\bin\claude.exe'
    if (Test-Path -LiteralPath $native) { return $native }
    $found = Get-Command claude.exe, claude.cmd -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) { return $found.Source }
    return $null
}

function Install-Tray {
    Write-Host 'Setting up Claude Code Tray...'

    $claude = Find-Claude
    if (-not $claude) { throw 'Claude Code was not found. Install it first, then run setup again.' }
    Step "Claude Code: $claude"

    if (-not (Get-Command wt.exe -ErrorAction SilentlyContinue)) {
        if (-not (Get-Command winget.exe -ErrorAction SilentlyContinue)) {
            throw 'Windows Terminal is required. Install it from https://aka.ms/terminal and run setup again.'
        }
        Step 'Installing Windows Terminal...'
        & winget.exe install --id Microsoft.WindowsTerminal -e --silent --accept-package-agreements --accept-source-agreements
        if ($LASTEXITCODE -ne 0) { throw "Could not install Windows Terminal (winget exit code $LASTEXITCODE)." }
    }
    Step 'Windows Terminal: ready'

    $settings = Read-Settings

    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path -LiteralPath $csc)) { throw "C# compiler not found: $csc" }
    Stop-InstalledTray
    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
    $log = & $csc /nologo /optimize+ /target:winexe /codepage:65001 "/out:$exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll (Join-Path $PSScriptRoot 'ClaudeTray.cs')
    if ($LASTEXITCODE -ne 0) { throw "Build failed:`n$($log -join "`n")" }
    Step "Tray: $exe"

    New-Item -ItemType Directory -Force -Path $soundsDir | Out-Null
    foreach ($file in 'coin-quiet.wav', 'notify-quiet.wav') {
        $target = Join-Path $soundsDir $file
        if (-not (Test-Path -LiteralPath $target)) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot "sounds\$file") -Destination $target }
    }
    Step "Sounds: $soundsDir"

    Add-HookGroup $settings 'Stop' ([pscustomobject]@{ hooks = @(Sound-Hook 'coin-quiet.wav') }) 'coin-quiet.wav'
    Add-HookGroup $settings 'Stop' ([pscustomobject]@{ hooks = @(Signal-Hook 'done') }) 'ClaudeTray.exe'
    Add-HookGroup $settings 'Notification' ([pscustomobject]@{ hooks = @(Sound-Hook 'notify-quiet.wav') }) 'notify-quiet.wav'
    Add-HookGroup $settings 'Notification' ([pscustomobject]@{ matcher = 'permission_prompt|elicitation_dialog'; hooks = @(Signal-Hook 'attention') }) 'ClaudeTray.exe'
    Write-Settings $settings
    Step "Hooks: $settingsPath"

    if (-not $NoAutostart) {
        New-Item -Path $runKey -Force -ErrorAction SilentlyContinue | Out-Null
        Set-ItemProperty -Path $runKey -Name $runName -Value "`"$exe`""
        Step 'Autostart: on'
    }

    if (-not $NoStart) {
        Start-Process -FilePath $exe
        Step 'Tray: started'
    }

    Write-Host ''
    Write-Host 'Done. Press Ctrl+Alt+C for the Claude window and Ctrl+Alt+V for voice typing.'
    Write-Host 'Restart any open Claude Code sessions to turn on the sounds.'
}

function Uninstall-Tray {
    Write-Host 'Removing Claude Code Tray...'
    Stop-InstalledTray
    if (-not $NoAutostart) {
        $value = (Get-ItemProperty -Path $runKey -Name $runName -ErrorAction SilentlyContinue).$runName
        if ($value -eq "`"$exe`"") { Remove-ItemProperty -Path $runKey -Name $runName }
    }
    if (Test-Path -LiteralPath $settingsPath) {
        $settings = Read-Settings
        Remove-OwnHooks $settings
        Write-Settings $settings
    }
    if (Test-Path -LiteralPath $InstallDir) { Remove-Item -LiteralPath $InstallDir -Recurse -Force }
    Write-Host "Done. The sound files stay in $soundsDir."
}

try {
    if ($Uninstall) { Uninstall-Tray } else { Install-Tray }
} catch {
    Write-Host ''
    Write-Host "Setup stopped: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
