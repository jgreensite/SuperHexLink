<#
ops-runner.ps1

Operations helper for managing the repository self-hosted GitHub Actions runner.

This script is intentionally non-interactive by default. Pass -Action to pick a command.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('status','list','register','unregister','start','stop','tail','health','list-runs','list-jobs','cancel-run','list-local-jobs','kill-local-job','run-logs','show-runners','ensure-registered')]
    [string]$Action = 'status',

    [string]$Arg = '',
    [string]$RunnerName = 'selfhost-GAMINGPC',
    [string]$RunnerDir = (Join-Path $PSScriptRoot 'actions-runner'),
    [int]$TailLines = 200
)

function Write-Info([string]$m){ Write-Host "[INFO] $m" }
function Write-Err([string]$m){ Write-Host "[ERR] $m" -ForegroundColor Red }

function Get-Repo(){ return 'jgreensite/SuperHexLink' }

function List-RegisteredRunners(){
    Write-Info "Querying GitHub for registered runners..."
    $repo = Get-Repo
    $path = "repos/$repo/actions/runners"
    $raw = gh api $path 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Err "gh api request failed: $raw"
        return $null
    }
    try {
        $resp = $raw | ConvertFrom-Json -ErrorAction Stop
    } catch {
        Write-Err "Failed to parse gh api JSON output: $($_)" 
        return $null
    }
    if ($null -eq $resp) { return $null }
    if ($resp.PSObject.Properties.Name -contains 'message') {
        Write-Err "GitHub API: $($resp.message)"
        return $null
    }
    return $resp.runners
}

function List-WorkflowRuns([int]$limit=10){
    Write-Info "Listing recent workflow runs (limit=$limit)"
    $repo = Get-Repo
    $raw = gh run list --repo $repo --limit $limit 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Err "gh run list failed: $raw"; return $null }
    return $raw
}

function List-RunJobs([string]$runId){
    if (-not $runId){ Write-Err 'Provide run id to list jobs'; return }
    Write-Info "Listing jobs for run $runId"
    $repo = Get-Repo
    $raw = gh api repos/$repo/actions/runs/$runId/jobs 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Err "gh api jobs failed: $raw"; return $null }
    try { $obj = $raw | ConvertFrom-Json -ErrorAction Stop } catch { Write-Err "Failed to parse jobs JSON: $_" ; return $null }
    return $obj.jobs
}

function Cancel-Run([string]$runId){
    if (-not $runId){ Write-Err 'Provide run id to cancel'; return }
    Write-Info "Canceling run $runId"
    $repo = Get-Repo
    $raw = gh run cancel $runId --repo $repo 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Err "Failed to cancel run: $raw"; return $null }
    Write-Info "Cancel request sent for run $runId"
}

function List-LocalWorkProcesses(){
    Write-Info 'Listing local processes created under runner _work directories'
    $workRoot = Join-Path $RunnerDir '_work'
    if (-not (Test-Path $workRoot)) { Write-Info "No _work dir found at $workRoot"; return @() }
    # Find processes whose CommandLine or WorkingDirectory reference the _work folder
    Get-CimInstance Win32_Process | Where-Object { ($_.CommandLine -and $_.CommandLine -match '_work') -or ($_.ExecutablePath -and $_.ExecutablePath -match '\\_work') } | Select-Object ProcessId,Name,CommandLine
}

function Kill-LocalProcess([int]$pid){
    if (-not $pid){ Write-Err 'Provide PID to kill'; return }
    try {
        Stop-Process -Id $pid -Force -ErrorAction Stop
        Write-Info "Killed PID $pid"
    } catch {
        $msg = $_.Exception.Message
        Write-Err ("Failed to kill PID {0}: {1}" -f $pid, $msg)
    }
}

function Get-RunLogs([string]$runId){
    if (-not $runId){ Write-Err 'Provide run id to fetch logs'; return }
    Write-Info "Fetching logs for run $runId"
    $repo = Get-Repo
    $raw = gh run view $runId --repo $repo --log 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Err "Failed to fetch run logs: $raw"; return $null }
    return $raw
}

function Show-Status(){
    Write-Info "Runner dir: $RunnerDir"
    if (Test-Path $RunnerDir){
        Write-Info 'Runner dir exists'
    } else { Write-Info 'Runner dir missing' }

    Write-Info 'Local runner processes:'
    Get-CimInstance Win32_Process -Filter "Name='Runner.Listener.exe' OR Name='cmd.exe'" | Select-Object ProcessId,Name,CommandLine | Format-Table -AutoSize

    Write-Info 'Registered repo runners:'
    $r = List-RegisteredRunners
    if (-not $r){ Write-Info 'No registered runners found or gh api failed' } else { $r | Select-Object id,name,status,busy | Format-Table -AutoSize }
}

function Start-Runner(){
    $runCmd = Join-Path $RunnerDir 'run.cmd'
    if (-not (Test-Path $runCmd)) { Write-Err "run.cmd not found at $runCmd"; return }
    Write-Info "Starting runner via $runCmd"
    Start-Process -FilePath $runCmd -WorkingDirectory $RunnerDir -WindowStyle Hidden -PassThru | ForEach-Object { Write-Info "Launched PID:$($_.Id)" }
}

function Stop-Runner(){
    Write-Info 'Stopping local runner processes (Runner.Listener.exe and wrapper cmd.exe)'
    Get-CimInstance Win32_Process -Filter "Name='Runner.Listener.exe' OR Name='cmd.exe'" | ForEach-Object {
        try { Stop-Process -Id $_.ProcessId -Force -ErrorAction Stop; Write-Info "Stopped PID:$($_.ProcessId)" } catch { Write-Err "Failed stopping PID:$($_.ProcessId) - $($_.Exception.Message)" }
    }
}

function Tail-Diag(){
    $diag = Join-Path $RunnerDir '_diag'
    if (-not (Test-Path $diag)) { Write-Err "No _diag dir at $diag"; return }
    $latest = Get-ChildItem -Path $diag -Filter 'Runner_*.log' | Sort-Object LastWriteTime | Select-Object -Last 1
    if (-not $latest) { Write-Err 'No runner diag logs found' ; return }
    Write-Info "Tailing $($latest.FullName) -- last $TailLines lines"
    Get-Content -LiteralPath $latest.FullName -Tail $TailLines
}

function Register-Runner(){
    Write-Info 'Registering runner (wrapper around existing register script)'
    $regScript = Join-Path $PSScriptRoot 'register-runner.ps1'
    if (-not (Test-Path $regScript)) { Write-Err "register-runner.ps1 not found at $regScript"; return }
    & $regScript
}

function Unregister-Runner(){
    Write-Info "Looking up runner by name '$RunnerName'"
    $r = List-RegisteredRunners | Where-Object { $_.name -eq $RunnerName }
    if (-not $r){ Write-Info 'No matching runner found to unregister' ; return }
    foreach ($x in $r){
        Write-Info "Deleting runner id $($x.id)"
        gh api -X DELETE repos/$(Get-Repo)/actions/runners/$($x.id) 2>$null
    }
}

function Health-Check(){
    Write-Info 'Checking GH CLI auth'
    gh auth status 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) { Write-Err 'gh auth not configured or insufficient scopes' ; return }

    Write-Info 'Checking that runner is configured and listening (tailing last logs)'
    Tail-Diag
}

function Ensure-Registered(){
    Write-Info 'Ensuring runner is registered for this repository'
    $r = List-RegisteredRunners
    if ($r -and $r.Count -gt 0) {
        Write-Info "Found $($r.Count) registered runner(s)."
        $r | Select-Object id,name,status,busy | Format-Table -AutoSize
        return
    }
    Write-Info 'No registered runner found; invoking registration script.'
    Register-Runner
    Write-Info 'Waiting up to 180s for runner to report Listening for Jobs...'
    $diag = Join-Path $RunnerDir '_diag'
    $waitStart = Get-Date
    while ((Get-Date) -lt $waitStart.AddSeconds(180)) {
        $latest = Get-ChildItem -Path $diag -Filter 'Runner_*.log' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
        if ($latest) {
            $tail = Get-Content -LiteralPath $latest.FullName -Tail 80 -ErrorAction SilentlyContinue | Out-String
            if ($tail -match 'Listening for Jobs') { Write-Info 'Runner is listening for jobs.'; return }
            if ($tail -match 'Registration was not found|The session for this runner already exists|Runner execution has finished') { Write-Err "Runner reported: `n$tail"; break }
        }
        Start-Sleep -Seconds 5
    }
    Write-Err 'Timed out waiting for runner to become ready. See _diag logs for details.'
}

switch ($Action) {
    'status'    { Show-Status }
    'list'      { List-RegisteredRunners | Select-Object id,name,status,busy }
    'list-runs' { List-WorkflowRuns 20 }
    'list-jobs' { if ($Arg) { List-RunJobs $Arg } else { Write-Err 'Provide run id as additional parameter (e.g. -Arg 18627502922)' } }
    'start'     { Start-Runner }
    'stop'      { Stop-Runner }
    'tail'      { Tail-Diag }
    'cancel-run'{ if ($Arg) { Cancel-Run $Arg } else { Write-Err 'Provide run id as additional parameter (e.g. -Arg 18627502922)' } }
    'list-local-jobs' { List-LocalWorkProcesses }
    'kill-local-job' { if ($Arg -and ($Arg -as [int])) { Kill-LocalProcess ([int]$Arg) } else { Write-Err 'Provide PID as -Arg <pid>' } }
    'run-logs'   { if ($Arg) { Get-RunLogs $Arg } else { Write-Err 'Provide run id as -Arg <runId>' } }
    'show-runners' { List-RegisteredRunners | Select-Object id,name,status,busy }
    'ensure-registered' { Ensure-Registered }
    'register'  { Register-Runner }
    'unregister'{ Unregister-Runner }
    'health'    { Health-Check }
    default     { Write-Err "Unknown action: $Action" }
}

