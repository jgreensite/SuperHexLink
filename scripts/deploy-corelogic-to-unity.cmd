@echo off
REM Wrapper for deploy-corelogic-to-unity.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0deploy-corelogic-to-unity.ps1" %*
