@echo off
echo ========================================================
echo       Creating Stock-Sync Desktop Shortcut...
echo ========================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command "$WshShell = New-Object -ComObject WScript.Shell; $desktop = [Environment]::GetFolderPath('Desktop'); if (-not $desktop) { $desktop = [IO.Path]::Combine($env:USERPROFILE, 'Desktop') }; $shortcutPath = [IO.Path]::Combine($desktop, 'Stock-Sync.lnk'); $shortcut = $WshShell.CreateShortcut($shortcutPath); $shortcut.TargetPath = '%~dp0Stock_Sync\Stock_Sync\bin\Release\Stock_Sync.exe'; $shortcut.WorkingDirectory = '%~dp0Stock_Sync\Stock_Sync\bin\Release'; $shortcut.Description = 'Stock-Sync — Supermarket Inventory & POS System'; $shortcut.Save(); Write-Host 'Shortcut created at: ' $shortcutPath -ForegroundColor Green"

echo.
echo [SUCCESS] The Stock-Sync shortcut has been added to your Desktop!
echo You can now open Stock-Sync anytime directly from your Desktop.
echo.
pause
