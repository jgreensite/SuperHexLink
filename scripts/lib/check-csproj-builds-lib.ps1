<#
Library helpers for check-csproj-builds.ps1.
Provides functions for mapping changed files to .csproj files so unit tests can validate mapping logic.
#>

function Resolve-ProjectForFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)] [string]$RepoRoot,
        [Parameter(Mandatory=$true)] [string]$FilePath
    )

    # Accept either absolute or repository-relative paths
    $full = if ([System.IO.Path]::IsPathRooted($FilePath)) { $FilePath } else { Join-Path $RepoRoot $FilePath }
    if (-not (Test-Path $full)) { return $null }

    # compute repo-relative path with forward slashes
    $rel = [System.IO.Path]::GetFullPath($full).Substring($RepoRoot.Length).TrimStart('\','/')

    # Ignore files under third-party plugins that shouldn't trigger builds
    if ($rel -match 'Assets[\/]+Plugins[\/]?Sirenix') { return $null }
    if ($rel -match 'actions-runner') { return $null }

    # Editor files prefer editor csproj
    if ($rel -match '^Assets[\\\/]+Editor[\\\/]') {
        $editorProj = Join-Path $RepoRoot 'Assembly-CSharp-Editor.csproj'
        if (Test-Path $editorProj) { return $editorProj }
        $foundEditor = Get-ChildItem -Path $RepoRoot -Filter '*Editor*.csproj' -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($foundEditor) { return $foundEditor.FullName }
    }

    # Assets runtime files attempt Assembly-CSharp* or nearest CoreLogic
    if ($rel -match '^Assets[\\\/]') {
        $runtimeProj = Join-Path $RepoRoot 'Assembly-CSharp.csproj'
        if (Test-Path $runtimeProj) { return $runtimeProj }
        $foundAssembly = Get-ChildItem -Path $RepoRoot -Filter 'Assembly-CSharp*.csproj' -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($foundAssembly) { return $foundAssembly.FullName }
        $coreLogicProj = Get-ChildItem -Path (Join-Path $RepoRoot 'CoreLogic') -Filter '*.csproj' -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($coreLogicProj) { return $coreLogicProj.FullName }
    }

    # If the file itself is a .csproj, return it
    if ($full -like '*.csproj') { return $full }

    # walk up directories to find a candidate csproj
    $dir = Split-Path -Path $full -Parent
    while ($dir -and ($dir -ne $RepoRoot)) {
        $candidates = Get-ChildItem -Path $dir -Filter *.csproj -File -ErrorAction SilentlyContinue
        if ($candidates) { return $candidates[0].FullName }
        $parent = Split-Path -Path $dir -Parent
        if ($parent -eq $dir) { break }
        $dir = $parent
    }

    return $null
}

function Get-ProjectsFromChangedFiles {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)] [string]$RepoRoot,
        [Parameter(Mandatory=$true)] [string[]]$ChangedFiles
    )

    $projSet = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($f in $ChangedFiles) {
        $resolved = Resolve-ProjectForFile -RepoRoot $RepoRoot -FilePath $f
        if ($resolved) { $projSet.Add($resolved) | Out-Null }
    }
    return @($projSet) | Select-Object -Unique
}
