@echo off
REM Wrapper for check-unity-errors.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0check-unity-errors.ps1" %*
