@echo off
:: Launches the Stock-Sync Mobile Telemetry Server silently in the background
:: Admin can view live supermarket sales on phone without opening the desktop GUI window.
cd /d "%~dp0\Stock_Sync\Stock_Sync\bin\Release"
start "" "Stock_Sync.exe" --background
