<#
Runs `dotnet build` for every .csproj under the repository and returns non-zero if any build fails.
This is a lightweight guard that CI and local pre-push hooks can call to catch compile errors quickly.
#>
Param(
    [switch]$ChangedOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
Write-Host "Repository root: $root"

if ($ChangedOnly)
{
    Write-Host "Running in ChangedOnly mode; building only projects impacted by staged changes."
    # Find staged files
    $staged = git diff --cached --name-only 2>$null
    if (-not $staged) {
        Write-Host "No staged changes detected; no projects to build." -ForegroundColor Yellow
        exit 0
    }

    $changedFiles = $staged | Where-Object { $_ -ne '' } | ForEach-Object { (Join-Path $root $_) }
    $projSet = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($file in $changedFiles)
    {
        if (-not (Test-Path $file)) { continue }
        # If this is a csproj, add it directly
        if ($file -like '*.csproj') { $projSet.Add($file) | Out-Null; continue }
        $dir = Split-Path -Path $file -Parent
        while ($dir -and ($dir -ne $root) -and ($dir -like "*\\*")) {
            $candidates = Get-ChildItem -Path $dir -Filter *.csproj -File -ErrorAction SilentlyContinue
            if ($candidates -and $candidates.Count -gt 0) {
                foreach ($c in $candidates) { $projSet.Add($c.FullName) | Out-Null }
                break
            }
            $parent = Split-Path -Path $dir -Parent
            if ($parent -eq $dir) { break }
            $dir = $parent
        }
    }

    if ($projSet.Count -eq 0) {
        Write-Host "No project files discovered for staged changes; nothing to build." -ForegroundColor Yellow
        exit 0
    }

    $csprojs = $projSet.ToArray()
} else {
    $csprojs = Get-ChildItem -Path $root -Recurse -Filter *.csproj -ErrorAction SilentlyContinue | 
    Where-Object { 
        $_.FullName -notmatch '\\Sirenix\\' -and 
        $_.FullName -notmatch 'actions-runner' -and
        $_.Name -notmatch '^Sirenix\.'
    } |
    Select-Object -ExpandProperty FullName

}

if (-not $csprojs) {
    Write-Host "No .csproj files found under repository. Nothing to build." -ForegroundColor Yellow
    exit 0
}

$failed = @()
foreach ($proj in $csprojs) {
    Write-Host "Building: $proj"
    $proc = Start-Process -FilePath 'dotnet' -ArgumentList @('build', $proj, '/property:GenerateFullPaths=true', '/consoleloggerparameters:NoSummary') -Wait -PassThru -NoNewWindow
    if ($proc.ExitCode -ne 0) {
        Write-Host "Build failed: $proj (exit $($proc.ExitCode))" -ForegroundColor Red
        $failed += $proj
    } else {
        Write-Host "Build succeeded: $proj" -ForegroundColor Green
    }
}

if ($failed.Count -gt 0) {
    Write-Host "One or more projects failed to build:" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host "All projects built successfully." -ForegroundColor Green
exit 0
