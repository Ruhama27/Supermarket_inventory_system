# Adds Stock-Sync Mobile Server to Windows Startup
# When the PC turns on, the Mobile Server starts silently in the background
# The Admin can check sales on phone without touching or opening the PC desktop app.

$startupFolder = [Environment]::GetFolderPath('Startup')
$shortcutPath = Join-Path $startupFolder "Stock-Sync-MobileServer.lnk"

$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$targetBat = Join-Path $scriptDir "Start-MobileServer-Background.bat"

$shortcut.TargetPath = "cmd.exe"
$shortcut.Arguments = "/c `"$targetBat`""
$shortcut.WorkingDirectory = $scriptDir
$shortcut.WindowStyle = 7 # Minimized / Silent
$shortcut.Description = "Stock-Sync Background Mobile Server for Admin"
$shortcut.Save()

Write-Host "SUCCESS: Stock-Sync Mobile Server has been added to Windows Startup!" -ForegroundColor Green
Write-Host "Whenever this PC turns on, the mobile server runs automatically in the background." -ForegroundColor Cyan
