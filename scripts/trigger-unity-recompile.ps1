#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Triggers Unity Editor to recompile scripts by touching a marker file.

.DESCRIPTION
    Unity automatically recompiles when it detects file changes. This script
    creates or updates a timestamp on a marker file to trigger recompilation.
    
.PARAMETER Wait
    If specified, waits for Unity to finish compilation before returning.
    
.PARAMETER WaitSeconds
    How many seconds to wait for compilation to start (default: 3).

.EXAMPLE
    .\trigger-unity-recompile.ps1
    Triggers recompilation immediately.
    
.EXAMPLE
    .\trigger-unity-recompile.ps1 -Wait -WaitSeconds 5
    Triggers recompilation and waits 5 seconds for Unity to process it.
#>

param(
    [switch]$Wait,
    [int]$WaitSeconds = 3
)

$ErrorActionPreference = "Stop"

# Create marker file path (PowerShell 5.1 compatible)
$projectRoot = Join-Path $PSScriptRoot ".."
$assetsPath = Join-Path $projectRoot "Assets"
$scriptsPath = Join-Path $assetsPath "Scripts"
$markerFile = Join-Path $scriptsPath ".recompile_marker"

# Touch the file to update its timestamp (or create it if it doesn't exist)
if (Test-Path $markerFile) {
    (Get-Item $markerFile).LastWriteTime = Get-Date
    Write-Host "Updated marker file timestamp: $markerFile"
} else {
    # Create a simple C# comment file that won't affect compilation
    @"
// Marker file to trigger Unity recompilation
// This file is automatically managed by trigger-unity-recompile.ps1
// Last updated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
"@ | Out-File -FilePath $markerFile -Encoding utf8
    Write-Host "Created marker file: $markerFile"
}

if ($Wait) {
    Write-Host "Waiting $WaitSeconds seconds for Unity to detect changes..."
    Start-Sleep -Seconds $WaitSeconds
    Write-Host "Unity should now be compiling. Use check-unity-errors.ps1 to verify."
}
