@echo off
REM Wrapper to run the Unity editmode tests script using Windows PowerShell
SETLOCAL
set SCRIPT_DIR=%~dp0
set PS_SCRIPT=%SCRIPT_DIR%run-unity-editmode-tests.ps1
if not exist "%PS_SCRIPT%" (
  echo Could not find %PS_SCRIPT%
  exit /b 2
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" %*
exit /b %ERRORLEVEL%
