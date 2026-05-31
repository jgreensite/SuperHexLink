<#
.SYNOPSIS
    Builds CoreLogic and deploys it to Unity's Assets/Plugins folder.

.DESCRIPTION
    This script:
    1. Builds CoreLogic.dll in Release configuration
    2. Copies the DLL and dependencies to Assets/Plugins/CoreLogic/
    3. Ensures Unity can reference the CoreLogic library

.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release

.EXAMPLE
    .\scripts\deploy-corelogic-to-unity.ps1
    Builds and deploys CoreLogic in Release mode

.EXAMPLE
    .\scripts\deploy-corelogic-to-unity.ps1 -Configuration Debug
    Builds and deploys CoreLogic in Debug mode
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Script paths
$scriptRoot = Split-Path -Parent $PSCommandPath
$repoRoot = Split-Path -Parent $scriptRoot
$coreLogicProject = Join-Path $repoRoot "CoreLogic\src\CoreLogic\CoreLogic.csproj"
$buildOutput = Join-Path $repoRoot "CoreLogic\src\CoreLogic\bin\$Configuration\netstandard2.0"
$unityPluginsDir = Join-Path $repoRoot "Assets\Plugins\CoreLogic"

Write-Host "=== Deploying CoreLogic to Unity ===" -ForegroundColor Cyan

# Step 1: Build CoreLogic
Write-Host "`n[1/3] Building CoreLogic ($Configuration)..." -ForegroundColor Yellow
$buildArgs = @(
    "build",
    $coreLogicProject,
    "-c", $Configuration,
    "--nologo"
)

& dotnet $buildArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "CoreLogic build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Build succeeded: $buildOutput" -ForegroundColor Green

# Step 2: Create Unity Plugins directory
Write-Host "`n[2/3] Creating Unity Plugins directory..." -ForegroundColor Yellow
if (-not (Test-Path $unityPluginsDir)) {
    New-Item -ItemType Directory -Path $unityPluginsDir -Force | Out-Null
    Write-Host "Created: $unityPluginsDir" -ForegroundColor Green
} else {
    Write-Host "Directory exists: $unityPluginsDir" -ForegroundColor Gray
}

# Step 3: Copy DLLs and dependencies
Write-Host "`n[3/3] Copying DLLs to Unity..." -ForegroundColor Yellow

# Copy CoreLogic.dll and .pdb
$filesToCopy = @(
    @{ Name = "CoreLogic.dll"; Required = $true },
    @{ Name = "CoreLogic.pdb"; Required = $false }
)

foreach ($file in $filesToCopy) {
    $sourcePath = Join-Path $buildOutput $file.Name
    $destPath = Join-Path $unityPluginsDir $file.Name
    
    if (Test-Path $sourcePath) {
        Copy-Item $sourcePath -Destination $destPath -Force
        Write-Host "  Copied: $($file.Name)" -ForegroundColor Green
    } elseif ($file.Required) {
        Write-Error "Required file not found: $sourcePath"
        exit 1
    } else {
        Write-Host "  Skipped: $($file.Name) (not found)" -ForegroundColor Gray
    }
}

# Copy Newtonsoft.Json dependency
$newtonsoftVersion = "13.0.3"
$newtonsoftPath = Join-Path $env:USERPROFILE ".nuget\packages\newtonsoft.json\$newtonsoftVersion\lib\netstandard2.0\Newtonsoft.Json.dll"

if (Test-Path $newtonsoftPath) {
    $destPath = Join-Path $unityPluginsDir "Newtonsoft.Json.dll"
    Copy-Item $newtonsoftPath -Destination $destPath -Force
    Write-Host "  Copied: Newtonsoft.Json.dll" -ForegroundColor Green
} else {
    Write-Warning "Newtonsoft.Json not found in NuGet cache: $newtonsoftPath"
    Write-Warning "CoreLogic may not work in Unity without this dependency."
    Write-Host "  Run 'dotnet restore' on CoreLogic to download dependencies" -ForegroundColor Yellow
}

# Copy MoonSharp dependency (Lua interpreter for RulesEngine)
$moonsharpVersion = "2.0.0"
# MoonSharp 2.0.0 ships netstandard1.6 as its highest netstandard target (compatible with ns2.0)
$moonsharpPath = Join-Path $env:USERPROFILE ".nuget\packages\moonsharp\$moonsharpVersion\lib\netstandard1.6\MoonSharp.Interpreter.dll"

if (Test-Path $moonsharpPath) {
    $destPath = Join-Path $unityPluginsDir "MoonSharp.Interpreter.dll"
    Copy-Item $moonsharpPath -Destination $destPath -Force
    Write-Host "  Copied: MoonSharp.Interpreter.dll" -ForegroundColor Green
} else {
    Write-Warning "MoonSharp not found in NuGet cache: $moonsharpPath"
    Write-Warning "RulesEngine Lua support will not work in Unity without this dependency."
    Write-Host "  Run 'dotnet restore' on CoreLogic to download dependencies" -ForegroundColor Yellow
}

# Summary
Write-Host "`n=== Deployment Complete ===" -ForegroundColor Cyan
Write-Host "CoreLogic deployed to: $unityPluginsDir" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "  1. Open Unity Editor" -ForegroundColor White
Write-Host "  2. Unity will import the new DLLs" -ForegroundColor White
Write-Host "  3. Verify Assets\Scripts\CoreLogicAdapter.cs compiles without errors" -ForegroundColor White
