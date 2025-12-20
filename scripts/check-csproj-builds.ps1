<#
Runs `dotnet build` for every .csproj under the repository and returns non-zero if any build fails.
This is a lightweight guard that CI and local pre-push hooks can call to catch compile errors quickly.
#>
[CmdletBinding(SupportsShouldProcess=$true)]
Param(
    [switch]$ChangedOnly,
    [string]$DiffRef,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
Write-Verbose "Repository root: $root"

# Load mapping helpers if present
$lib = Join-Path $root 'scripts\lib\check-csproj-builds-lib.ps1'
if (Test-Path $lib) { . $lib } else { Write-Verbose "Mapping lib not found: $lib" }

if ($ChangedOnly)
{
    Write-Host "Running in ChangedOnly mode; building only projects impacted by staged changes."
    # Find staged files or use diff ref
    if ($DiffRef) {
        Write-Verbose "Using diff ref: $DiffRef to identify changed files"
        # Try using origin/$DiffRef if available; otherwise attempt to fetch it
        $remoteRef = "origin/$DiffRef"
        $refExists = $false
        git show-ref --verify --quiet "refs/remotes/$remoteRef" 2>$null
        if ($LASTEXITCODE -eq 0) { $refExists = $true }
        if (-not $refExists) {
            Write-Verbose "Attempting to fetch origin/$DiffRef so we can diff against it"
            try {
                git fetch origin $DiffRef --depth=1 2>$null
            } catch {
                Write-Verbose "Failed to fetch origin/$DiffRef - proceeding with local ref if available"
            }
        }
        # Prefer origin/$DiffRef to ensure PR base ref resolution
        $staged = git diff --name-only origin/$DiffRef..HEAD 2>$null
        if (-not $staged) {
            # Last-resort fallback to local ref
            $staged = git diff --name-only $DiffRef..HEAD 2>$null
        }
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
    if (Get-Command Get-ProjectsFromChangedFiles -ErrorAction SilentlyContinue) {
        $csprojCandidates = Get-ProjectsFromChangedFiles -RepoRoot $root -ChangedFiles $changedFiles
        $projSet = [System.Collections.Generic.HashSet[string]]::new()
        foreach ($p in $csprojCandidates) { $projSet.Add($p) | Out-Null }
    } else {
        # Fallback to prior inline behavior if helper not available
        $projSet = [System.Collections.Generic.HashSet[string]]::new()
        foreach ($file in $changedFiles) {
            if (-not (Test-Path $file)) { continue }
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
    }

    if ($projSet.Count -eq 0) {
        Write-Host "No project files discovered for staged changes; nothing to build." -ForegroundColor Yellow
        # Create/clear the changed-csprojs.txt output so CI steps downstream can rely on its presence
        $changedFile = Join-Path $root 'changed-csprojs.txt'
        if (Test-Path $changedFile) { Remove-Item $changedFile -Force }
        New-Item -Path $changedFile -ItemType File -Force | Out-Null
        exit 0
    }

    $csprojs = @($projSet) | Select-Object -Unique
    # Persist the detected projects so CI workflows can read them (e.g., for conditional steps)
    try {
        $changedFile = Join-Path $root 'changed-csprojs.txt'
        # Normalize to repository-root relative paths with forward slashes so CI can match against parts
        $relativePaths = $csprojs | ForEach-Object { $_.Substring($root.Length).TrimStart('\','/') -replace '\\','/' }
        $relativePaths | Set-Content -Path $changedFile -Encoding UTF8
        Write-Verbose "Wrote changed project list to $changedFile"
    } catch {
        Write-Verbose "Failed to write changed project list: $($_.Exception.Message)"
    }
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

## Debug and normalize DryRun detection
$failed = @()
$isDryRun = $false
# Treat -WhatIf the same as DryRun so the script prints what it would build but does not execute
if ($PSBoundParameters.ContainsKey('DryRun') -or $DryRun -or $PSBoundParameters.ContainsKey('WhatIf')) { $isDryRun = $true }
if ($env:CHECK_CS_PROJ_DEBUG) {
    Write-Host "[DEBUG] PSBoundParameters: $($PSBoundParameters.Keys -join ', ')"
    Write-Host "[DEBUG] Detected DryRun: $isDryRun"
}
if ($isDryRun) {
    Write-Host "(DryRun) Would build the following projects:" -ForegroundColor Cyan
    foreach ($proj in $csprojs) { Write-Host " - $proj" }
} else {
    foreach ($proj in $csprojs) {
        Write-Host "Building: $proj"
        # Respect ShouldProcess / -WhatIf by wrapping the actual build call.
        if ($PSCmdlet -and $PSCmdlet.ShouldProcess($proj, 'Build')) {
            $proc = Start-Process -FilePath 'dotnet' -ArgumentList @('build', $proj, '/property:GenerateFullPaths=true', '/consoleloggerparameters:NoSummary') -Wait -PassThru -NoNewWindow
            if ($proc.ExitCode -ne 0) {
                Write-Host "Build failed: $proj (exit $($proc.ExitCode))" -ForegroundColor Red
                $failed += $proj
            } else {
                Write-Host "Build succeeded: $proj" -ForegroundColor Green
            }
        } else {
            Write-Host "Skipping build due to -WhatIf or ShouldProcess for: $proj" -ForegroundColor Yellow
        }
    }
}

if ($failed.Count -gt 0) {
    Write-Host "One or more projects failed to build:" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host "All projects built successfully." -ForegroundColor Green
exit 0
