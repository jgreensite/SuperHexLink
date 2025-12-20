function MapFileToProjects {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)][string]$FilePath,
        [Parameter(Mandatory=$true)][string]$RepoRoot
    )

    $fp = $FilePath -replace '/','\'
    $fp = $fp.TrimStart('\','/')

    # Ignore certain plugin or runner folders
    $parts = $fp -split '[\\/]+'
    if ($parts.Count -ge 3 -and $parts[0] -ieq 'Assets' -and $parts[1] -ieq 'Plugins' -and $parts[2] -ieq 'Sirenix') { return @() }
    if ($parts.Count -ge 2 -and $parts[0] -ieq 'scripts' -and $parts[1] -ieq 'actions-runner') { return @() }

    $results = [System.Collections.Generic.HashSet[string]]::new()

    # Editor files map to Assembly-CSharp-Editor.csproj
    if ($fp -match '^Assets[\\/]Editor[\\/]') {
        $editorProj = Join-Path $RepoRoot 'Assembly-CSharp-Editor.csproj'
        if (Test-Path $editorProj) { $results.Add((Get-Item $editorProj).FullName) | Out-Null; return $results }
    }

    # Assets runtime files map to Assembly-CSharp.csproj
    if ($fp -match '^Assets[\\/]') {
        $runtimeProj = Join-Path $RepoRoot 'Assembly-CSharp.csproj'
        if (Test-Path $runtimeProj) { $results.Add((Get-Item $runtimeProj).FullName) | Out-Null; return $results }
    }

    # Direct csproj filename
    if ($fp -like '*.csproj') {
        $candidate = Join-Path $RepoRoot $fp
        if (Test-Path $candidate) { $results.Add((Get-Item $candidate).FullName) | Out-Null; return $results }
    }

    # CoreLogic-specific mapping
    if ($fp -match '^CoreLogic[\\/]') {
        $found = Get-ChildItem -Path (Join-Path $RepoRoot 'CoreLogic') -Filter '*.csproj' -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) { $results.Add($found.FullName) | Out-Null; return $results }
    }

    # General upward search for nearest csproj
    $dir = Split-Path -Path $fp -Parent
    if (-not $dir) { $dir = '.' }
    $absDir = Join-Path $RepoRoot $dir
    while ($absDir -and ($absDir -ne $RepoRoot) -and (Test-Path $absDir)) {
        $candidates = Get-ChildItem -Path $absDir -Filter *.csproj -File -ErrorAction SilentlyContinue
        if ($candidates) { foreach ($c in $candidates) { $results.Add($c.FullName) | Out-Null }; break }
        $parent = Split-Path -Path $absDir -Parent
        if ($parent -eq $absDir) { break }
        $absDir = $parent
    }

    return $results | Sort-Object
}

function Get-ProjectsFromChangedFiles {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)][string]$RepoRoot,
        [Parameter(Mandatory=$true)][string[]]$ChangedFiles
    )
    $projSet = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($f in $ChangedFiles) {
        $mapped = MapFileToProjects -FilePath $f -RepoRoot $RepoRoot
        foreach ($m in $mapped) { $projSet.Add($m) | Out-Null }
    }
    return $projSet | Sort-Object
}


<#
Utility functions for mapping changed files to csproj files in the repository.
This module is intentionally small and pure to make it easy to unit test with Pester.
#>

function Get-RepoRoot {
    param()
    $d = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
    while ($d -and -not (Test-Path (Join-Path $d '.git')) -and -not (Test-Path (Join-Path $d 'README.md'))) { $d = Split-Path -Path $d -Parent }
    return $d
}

function MapFileToProjects {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)] [string] $FilePath,
        [Parameter(Mandatory=$false)] [string] $RepoRoot
    )
    if (-not $RepoRoot) { $RepoRoot = Get-RepoRoot }
    $fileFull = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $FilePath))

    # If it's a csproj itself, return it
    if ($fileFull -like '*.csproj') { return @($fileFull) }

    $relative = $fileFull.Substring($RepoRoot.Length).TrimStart('\','/')

    # Editor files map to editor csproj
    if ($relative -match '^Assets[\\\/]+Editor[\\\/]') {
        $editorCandidate = Join-Path $RepoRoot 'Assembly-CSharp-Editor.csproj'
        if (Test-Path $editorCandidate) { return @($editorCandidate) }
        $foundEditor = Get-ChildItem -Path $RepoRoot -Filter '*Editor*.csproj' -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($foundEditor) { return @($foundEditor.FullName) }
    }

    # Runtime assets map to Assembly-CSharp
    if ($relative -match '^Assets[\\\/]') {
        $runtimeCandidate = Join-Path $RepoRoot 'Assembly-CSharp.csproj'
        if (Test-Path $runtimeCandidate) { return @($runtimeCandidate) }
        $foundAssembly = Get-ChildItem -Path $RepoRoot -Filter 'Assembly-CSharp*.csproj' -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($foundAssembly) { return @($foundAssembly.FullName) }
        # fallback to any CoreLogic project
        $core = Get-ChildItem -Path (Join-Path $RepoRoot 'CoreLogic') -Filter '*.csproj' -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($core) { return @($core.FullName) }
    }

    # Otherwise, scan upwards for a csproj
    $dir = Split-Path -Path $fileFull -Parent
    while ($dir -and ($dir -ne $RepoRoot)) {
        $candidates = Get-ChildItem -Path $dir -Filter *.csproj -File -ErrorAction SilentlyContinue
        if ($candidates) { return $candidates | ForEach-Object { $_.FullName } }
        $parent = Split-Path -Path $dir -Parent
        if ($parent -eq $dir) { break }
        $dir = $parent
    }

    return @()
}

# Export-ModuleMember is omitted to allow this file to be dot-sourced in tests
