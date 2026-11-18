$WshShell = New-Object -ComObject WScript.Shell
$desktop = [Environment]::GetFolderPath('Desktop')
if (-not $desktop) {
    $desktop = "C:\Users\hp\Desktop"
}
$shortcutPath = Join-Path $desktop "Stock-Sync.lnk"
$targetPath = "C:\Users\hp\Downloads\Inventory-Management-System-Desktop-Application-main\Supermarket Inventory System\Stock_Sync\Stock_Sync\bin\Release\Stock_Sync.exe"
$workingDir = Split-Path $targetPath -Parent

$shortcut = $WshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetPath
$shortcut.WorkingDirectory = $workingDir
$shortcut.Description = "Stock-Sync — Supermarket Inventory & POS System"
$shortcut.Save()

Write-Output "SUCCESS: Created shortcut at $shortcutPath"
