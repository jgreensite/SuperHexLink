Import-Module Pester -ErrorAction SilentlyContinue

Describe 'Get-ProjectsFromChangedFiles mapping' {
    BeforeAll {
        # Create an isolated temp area for tests
        $global:TestDrive = Join-Path $env:TEMP "pester-test-$(Get-Random)"
        New-Item -ItemType Directory -Path $TestDrive -Force | Out-Null
        . "$PSScriptRoot\..\lib\check-csproj-builds-lib.ps1"
    }
    AfterAll { Remove-Item -Recurse -Force $TestDrive -ErrorAction SilentlyContinue }

    It 'Maps Assets/Scripts/Player.cs to Assembly-CSharp.csproj' {
        $root = Join-Path $TestDrive 'repo'
        New-Item -ItemType Directory -Path (Join-Path $root 'Assets\Scripts') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'Assets\Scripts\Player.cs') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'Assembly-CSharp.csproj') -Force | Out-Null

        $res = Get-ProjectsFromChangedFiles -RepoRoot $root -ChangedFiles @('Assets/Scripts/Player.cs')
        $res | Should -Contain (Join-Path $root 'Assembly-CSharp.csproj')
    }

    It 'Maps Assets/Editor/Tool.cs to Assembly-CSharp-Editor.csproj when present' {
        $root = Join-Path $TestDrive 'repo2'
        New-Item -ItemType Directory -Path (Join-Path $root 'Assets\Editor') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'Assets\Editor\Tool.cs') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'Assembly-CSharp-Editor.csproj') -Force | Out-Null

        $res = Get-ProjectsFromChangedFiles -RepoRoot $root -ChangedFiles @('Assets/Editor/Tool.cs')
        $res | Should -Contain (Join-Path $root 'Assembly-CSharp-Editor.csproj')
    }

    It 'Maps CoreLogic/src/Class1.cs to CoreLogic project' {
        $root = Join-Path $TestDrive 'repo3'
        New-Item -ItemType Directory -Path (Join-Path $root 'CoreLogic\src') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'CoreLogic\src\Class1.cs') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'CoreLogic\src\CoreLogic.csproj') -Force | Out-Null

        $res = Get-ProjectsFromChangedFiles -RepoRoot $root -ChangedFiles @('CoreLogic/src/Class1.cs')
        $res | Should -Contain (Join-Path $root 'CoreLogic\src\CoreLogic.csproj')
    }

    It 'Ignores projects under Assets/Plugins/Sirenix and actions-runner' {
        $root = Join-Path $TestDrive 'repo4'
        New-Item -ItemType Directory -Path (Join-Path $root 'Assets\Plugins\Sirenix') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'Assets\Plugins\Sirenix\Some.cs') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $root 'Assets\Plugins\Sirenix\Dummy.csproj') -Force | Out-Null

        $res = Get-ProjectsFromChangedFiles -RepoRoot $root -ChangedFiles @('Assets/Plugins/Sirenix/Some.cs')
        $res | Should -BeEmpty
    }
}

Describe 'CLI wrapper' {
    BeforeAll { . "$PSScriptRoot\..\..\check-csproj-builds-cli.ps1" }
    It 'Forwards -DryRun and -ChangedOnly to inner wrapper via Start-Process' {
        Mock -CommandName Start-Process -MockWith { return @{ ExitCode = 0 } }
        # Call the function that performs the invocation
        Invoke-CheckCsprojBuildsCli -ChangedOnly:$true -DiffRef 'main' -DryRun:$true | Out-Null
        Assert-MockCalled -CommandName Start-Process -Times 1 -Exactly -Scope It
        $calls = (Get-MockCalls -CommandName Start-Process)
        $argList = $calls[0].Arguments[1]
        $argList | Should -Contain '-ChangedOnly'
        $argList | Should -Contain '-DryRun'
    }
}
