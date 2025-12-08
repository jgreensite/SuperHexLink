<#
Runs `dotnet build` for every .csproj under the repository and returns non-zero if any build fails.
This is a lightweight guard that CI and local pre-push hooks can call to catch compile errors quickly.
#>
[CmdletBinding()]
Param(
    [switch]$ChangedOnly,
    [string]$DiffRef
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
Write-Verbose "Repository root: $root"

if ($ChangedOnly)
{
    Write-Host "Running in ChangedOnly mode; building only projects impacted by staged changes."
    # Find staged files or use diff ref
    if ($DiffRef) {
        Write-Verbose "Using diff ref: $DiffRef to identify changed files"
        $staged = git diff --name-only $DiffRef..HEAD 2>$null
    }
    else {
        # staged changes in the index
        $staged = git diff --cached --name-only 2>$null
    }
    if (-not $staged) {
        Write-Host "No staged changes detected; no projects to build." -ForegroundColor Yellow
        exit 0
    }

    $changedFiles = $staged | Where-Object { $_ -ne '' } | ForEach-Object { (Join-Path $root $_) }
    $projSet = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($file in $changedFiles)
    {
        if (-not (Test-Path $file)) { continue }
        # Map common Unity assembly layout: Assets/Editor -> Assembly-CSharp-Editor.csproj; Assets/* -> Assembly-CSharp.csproj
        $rel = [System.IO.Path]::GetFullPath($file).Substring($root.Length).TrimStart('\','/')
        Write-Verbose "Changed file relative path: $rel"
        if ($rel -match '^Assets[\\\/]+Editor[\\\/]+') {
            $editorProj = Join-Path $root 'Assembly-CSharp-Editor.csproj'
            if (Test-Path $editorProj) { $projSet.Add($editorProj) | Out-Null; continue }
        }
        elseif ($rel -match '^Assets[\\\/]') {
            $runtimeProj = Join-Path $root 'Assembly-CSharp.csproj'
            if (Test-Path $runtimeProj) { $projSet.Add($runtimeProj) | Out-Null; continue }
        }
        # If this is a csproj, add it directly
        if ($file -like '*.csproj') { $projSet.Add($file) | Out-Null; continue }
        $dir = Split-Path -Path $file -Parent
        while ($dir -and ($dir -ne $root)) {
            $candidates = Get-ChildItem -Path $dir -Filter *.csproj -File -ErrorAction SilentlyContinue
                if ($candidates) {
                foreach ($c in @($candidates)) { $projSet.Add($c.FullName) | Out-Null }
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

    $csprojs = @($projSet) | Select-Object -Unique
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
