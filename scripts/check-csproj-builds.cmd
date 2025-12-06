@echo off
REM Wrapper for Windows users without PowerShell Core (pwsh).
SETLOCAL
set SCRIPT_DIR=%~dp0
set PS_SCRIPT=%SCRIPT_DIR%check-csproj-builds.ps1
if not exist "%PS_SCRIPT%" (
  echo Could not find %PS_SCRIPT%
  exit /b 2
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" %*
exit /b %ERRORLEVEL%
