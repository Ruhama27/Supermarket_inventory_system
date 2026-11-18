<#
.SYNOPSIS
    Packages Stock-Sync into a standalone portable distribution.
#>

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$releaseDir = Join-Path $scriptDir "Stock_Sync\Stock_Sync\bin\Release"
$distDir = Join-Path $scriptDir "dist\StockSync_Portable_v1.0"
$zipPath = Join-Path $scriptDir "dist\StockSync_Portable_v1.0.zip"

Write-Host "==> Ensuring Release build is up to date..." -ForegroundColor Cyan
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" "$scriptDir\Stock_Sync\Stock_Sync.sln" /t:Build /p:Configuration=Release /p:UseSharedCompilation=false | Out-Null

if (-not (Test-Path $releaseDir)) {
    throw "Release directory not found at $releaseDir"
}

Write-Host "==> Creating distribution folder at $distDir..." -ForegroundColor Cyan
if (Test-Path $distDir) {
    Remove-Item -Recurse -Force $distDir
}
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

Write-Host "==> Copying application binaries and configuration..." -ForegroundColor Cyan
Copy-Item (Join-Path $releaseDir "Stock_Sync.exe") -Destination $distDir
Copy-Item (Join-Path $releaseDir "Stock_Sync.exe.config") -Destination $distDir
Copy-Item (Join-Path $scriptDir "setup_database.sql") -Destination $distDir
Copy-Item (Join-Path $scriptDir "upgrade_database.sql") -Destination $distDir
Copy-Item (Join-Path $scriptDir "README.md") -Destination $distDir

# Create launcher batch file in dist
$launcherBat = @"
@echo off
start "" "%~dp0Stock_Sync.exe"
"@
Set-Content -Path (Join-Path $distDir "Start-StockSync.bat") -Value $launcherBat

Write-Host "==> Creating standalone ZIP archive at $zipPath..." -ForegroundColor Cyan
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}
Compress-Archive -Path "$distDir\*" -DestinationPath $zipPath -Force

Write-Host "==> Distribution packaging completed successfully!" -ForegroundColor Green
Write-Host "    Portable Folder: $distDir"
Write-Host "    Portable ZIP:    $zipPath"
