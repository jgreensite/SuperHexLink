. (Join-Path $PSScriptRoot '..\..\lib\CheckCsprojMapping.ps1')

# Use system temp path in a portable way across OS runners
$tempRoot = [System.IO.Path]::GetTempPath()

Describe 'CheckCsprojMapping' {
    It 'maps Assets/Scripts/Player.cs to Assembly-CSharp.csproj when Assembly-CSharp exists' {
        $tmpPath = Join-Path $tempRoot "test_repo_$([Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $tmpPath -Force | Out-Null
        try {
            New-Item -Path (Join-Path $tmpPath 'Assets\Scripts') -ItemType Directory -Force | Out-Null
            New-Item -Path (Join-Path $tmpPath 'Assets\Scripts\Player.cs') -ItemType File -Force | Out-Null
            New-Item -Path (Join-Path $tmpPath 'Assembly-CSharp.csproj') -ItemType File -Force | Out-Null
            $res = MapFileToProjects -FilePath 'Assets/Scripts/Player.cs' -RepoRoot $tmpPath
            ($res -contains (Join-Path $tmpPath 'Assembly-CSharp.csproj')) | Should Be $true
        } finally { Remove-Item -Recurse -Force -Path $tmpPath }
    }

    It 'maps Assets/Editor/Tool.cs to Assembly-CSharp-Editor.csproj when it exists' {
        $tmpPath = Join-Path $tempRoot "test_repo_$([Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $tmpPath -Force | Out-Null
        try {
            New-Item -Path (Join-Path $tmpPath 'Assets\Editor') -ItemType Directory -Force | Out-Null
            New-Item -Path (Join-Path $tmpPath 'Assets\Editor\Tool.cs') -ItemType File -Force | Out-Null
            New-Item -Path (Join-Path $tmpPath 'Assembly-CSharp-Editor.csproj') -ItemType File -Force | Out-Null
            $res = MapFileToProjects -FilePath 'Assets/Editor/Tool.cs' -RepoRoot $tmpPath
            ($res -contains (Join-Path $tmpPath 'Assembly-CSharp-Editor.csproj')) | Should Be $true
        } finally { Remove-Item -Recurse -Force -Path $tmpPath }
    }

    It 'maps CoreLogic/src/Class1.cs to CoreLogic csproj' {
        $tmpPath = Join-Path $tempRoot "test_repo_$([Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $tmpPath -Force | Out-Null
        try {
            New-Item -Path (Join-Path $tmpPath 'CoreLogic\src') -ItemType Directory -Force | Out-Null
            New-Item -Path (Join-Path $tmpPath 'CoreLogic\src\Class1.cs') -ItemType File -Force | Out-Null
            New-Item -Path (Join-Path $tmpPath 'CoreLogic\CoreLogic.csproj') -ItemType File -Force | Out-Null
            $res = MapFileToProjects -FilePath 'CoreLogic/src/Class1.cs' -RepoRoot $tmpPath
            ($res -contains (Join-Path $tmpPath 'CoreLogic\CoreLogic.csproj')) | Should Be $true
        } finally { Remove-Item -Recurse -Force -Path $tmpPath }
    }

    It 'ignores projects under Assets/Plugins/Sirenix and actions-runner' {
        $tmpPath = Join-Path $tempRoot "test_repo_$([Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $tmpPath -Force | Out-Null
        try {
            New-Item -Path (Join-Path $tmpPath 'Assets\Plugins\Sirenix') -ItemType Directory -Force | Out-Null
            New-Item -Path (Join-Path $tmpPath 'Assets\Plugins\Sirenix\Some.cs') -ItemType File -Force | Out-Null
            # When no projects exist, mapping returns empty
            $res = MapFileToProjects -FilePath 'Assets/Plugins/Sirenix/Some.cs' -RepoRoot $tmpPath
            $res.Count | Should Be 0
        } finally { Remove-Item -Recurse -Force -Path $tmpPath }
    }
}

Describe 'CLI wrapper' {
    It 'forwards -DryRun and -ChangedOnly to underlying wrapper via Start-Process' {
        # Mock Start-Process to write the captured argument list to a temp file
        $mockArgsFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), 'pester-startproc-args.txt')
        if (Test-Path $mockArgsFile) { Remove-Item $mockArgsFile -Force }
        Mock -CommandName Start-Process -MockWith { param($FilePath, $ArgumentList) Set-Content -Path $mockArgsFile -Value ($ArgumentList -join '|'); return @{ ExitCode = 0 } }

        # Run the CLI script with parameters
        $script = Join-Path $PSScriptRoot '..\..\check-csproj-builds-cli.ps1'
        & $script -ChangedOnly -DiffRef 'main' -DryRun

        (Test-Path $mockArgsFile) | Should Be $true
        $raw = Get-Content $mockArgsFile -ErrorAction SilentlyContinue
        ($raw -match '-DryRun') | Should Be $true
        ($raw -match '-ChangedOnly') | Should Be $true
    }
}
Import-Module Pester -ErrorAction SilentlyContinue

Describe 'Get-ProjectsFromChangedFiles mapping' {
    BeforeAll {
        # Create an isolated temp area for tests
        $global:TestDrive = Join-Path $tempRoot "pester-test-$(Get-Random)"
        New-Item -ItemType Directory -Path $TestDrive -Force | Out-Null
        # Dot-source the library from scripts/lib
        . "$PSScriptRoot\..\..\lib\check-csproj-builds-lib.ps1"
    }
    AfterAll { Remove-Item -Recurse -Force $TestDrive -ErrorAction SilentlyContinue }

    It 'Maps Assets/Scripts/Player.cs to Assembly-CSharp.csproj' {
        $root = Join-Path $TestDrive 'repo'
        New-Item -ItemType Directory -Path (Join-Path $root 'Assets\Scripts') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'Assets\Scripts\Player.cs') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'Assembly-CSharp.csproj') -Force | Out-Null

        $res = Get-ProjectsFromChangedFiles -RepoRoot $root -ChangedFiles @('Assets/Scripts/Player.cs')
        ($res -contains (Join-Path $root 'Assembly-CSharp.csproj')) | Should Be $true
    }

    It 'Maps Assets/Editor/Tool.cs to Assembly-CSharp-Editor.csproj when present' {
        $root = Join-Path $TestDrive 'repo2'
        New-Item -ItemType Directory -Path (Join-Path $root 'Assets\Editor') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'Assets\Editor\Tool.cs') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'Assembly-CSharp-Editor.csproj') -Force | Out-Null

        $res = Get-ProjectsFromChangedFiles -RepoRoot $root -ChangedFiles @('Assets/Editor/Tool.cs')
        ($res -contains (Join-Path $root 'Assembly-CSharp-Editor.csproj')) | Should Be $true
    }

    It 'Maps CoreLogic/src/Class1.cs to CoreLogic project' {
        $root = Join-Path $TestDrive 'repo3'
        New-Item -ItemType Directory -Path (Join-Path $root 'CoreLogic\src') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'CoreLogic\src\Class1.cs') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'CoreLogic\src\CoreLogic.csproj') -Force | Out-Null

        $res = Get-ProjectsFromChangedFiles -RepoRoot $root -ChangedFiles @('CoreLogic/src/Class1.cs')
        ($res -contains (Join-Path $root 'CoreLogic\src\CoreLogic.csproj')) | Should Be $true
    }

    It 'Ignores projects under Assets/Plugins/Sirenix and actions-runner' {
        $root = Join-Path $TestDrive 'repo4'
        New-Item -ItemType Directory -Path (Join-Path $root 'Assets\Plugins\Sirenix') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'Assets\Plugins\Sirenix\Some.cs') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'Assets\Plugins\Sirenix\Dummy.csproj') -Force | Out-Null

        $res = Get-ProjectsFromChangedFiles -RepoRoot $root -ChangedFiles @('Assets/Plugins/Sirenix/Some.cs')
        # Ensure plugin file does not map to the local Dummy.csproj
        ($res -contains (Join-Path $root 'Assets\Plugins\Sirenix\Dummy.csproj')) | Should Be $false
    }
}

Describe 'CLI wrapper' {
    BeforeAll { . "$PSScriptRoot\..\..\check-csproj-builds-cli.ps1" }
    It 'Forwards -DryRun and -ChangedOnly to inner wrapper via Start-Process' {
        $mockArgsFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), 'pester-startproc-args.txt')
        if (Test-Path $mockArgsFile) { Remove-Item $mockArgsFile -Force }
        Mock -CommandName Start-Process -MockWith { param($FilePath, $ArgumentList); Set-Content -Path $mockArgsFile -Value ($ArgumentList -join '|'); return @{ ExitCode = 0 } }
        Invoke-CheckCsprojBuildsCli -ChangedOnly:$true -DiffRef 'main' -DryRun:$true | Out-Null
        Assert-MockCalled -CommandName Start-Process -Times 1 -Exactly -Scope It
        $raw = Get-Content $mockArgsFile -ErrorAction SilentlyContinue
        ($raw -match '-ChangedOnly') | Should Be $true
        ($raw -match '-DryRun') | Should Be $true
    }
}
