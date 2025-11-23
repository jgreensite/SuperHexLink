# Robust register-runner.ps1
$ErrorActionPreference = 'Stop'

function Write-Info($m) { Write-Host "[INFO] $m" }
function Write-Err($m) { Write-Host "[ERROR] $m" -ForegroundColor Red }

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$dest = Join-Path $scriptRoot 'actions-runner'
$zipPath = Join-Path $scriptRoot 'actions-runner.zip'

# Desired runner name (unique per host)
$runnerName = "selfhost-$(hostname)"

Write-Info "Checking gh CLI and authentication..."
try { gh auth status 2>$null } catch { Write-Err "gh CLI not authenticated or not installed. Run 'gh auth login' first."; exit 2 }

function Get-RegisteredRunners() {
	try {
		$raw = gh api repos/jgreensite/SuperHexLink/actions/runners 2>$null
		if (-not $raw) { return @() }
		$obj = $raw | ConvertFrom-Json
		return $obj.runners
	} catch {
		Write-Err ("Failed to query registered runners: {0}" -f $_.Exception.Message)
		return @()
	}
}

function Delete-RunnerRegistration($id) {
	Write-Info "Deleting runner registration id=$id via GitHub API"
	try {
		gh api -X DELETE "repos/jgreensite/SuperHexLink/actions/runners/$id" 2>$null
		Write-Info "Deleted runner $id"
	} catch { Write-Err ("Failed to delete runner {0}: {1}" -f $id, $_.Exception.Message) }
}

function Get-RunnerProcess {
	Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
		($_.CommandLine -and ($_.CommandLine -match 'Runner.Listener' -or $_.CommandLine -match 'run.cmd')) -or
		($_.ExecutablePath -and ($_.ExecutablePath -like '*actions-runner*'))
	}
}

function Get-LatestDiagFile {
	$diag = Join-Path $dest '_diag'
	if (-not (Test-Path $diag)) { return $null }
	return Get-ChildItem -LiteralPath $diag -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
}

function Wait-ForLogPattern($pattern, $timeoutSec) {
	$start = Get-Date
	while ((Get-Date) -lt $start.AddSeconds($timeoutSec)) {
		$f = Get-LatestDiagFile
		if ($f) {
			try {
				$content = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction SilentlyContinue
				if ($content -match $pattern) { return @{ Found = $true; File = $f; Content = $content } }
			} catch { }
		}
		Start-Sleep -Seconds 2
	}
	return @{ Found = $false; File = $f }
}

# Step 1: Clean up any stale registration on GitHub with the same name
Write-Info "Looking for existing registered runners named '$runnerName'..."
$registered = Get-RegisteredRunners
$matches = $registered | Where-Object { $_.name -eq $runnerName }
if ($matches -and $matches.Count -gt 0) {
	foreach ($m in $matches) {
		Write-Info "Found registered runner id=$($m.id) name=$($m.name) status=$($m.status). Removing to avoid session conflicts."
		Delete-RunnerRegistration $m.id
		Start-Sleep -Seconds 1
	}
} else {
	Write-Info "No existing registration with name '$runnerName'"
}

# Step 2: Ensure actions-runner files exist (download/extract if missing)
if (-not (Test-Path $dest)) {
	Write-Info "Fetching latest actions-runner release info..."
	$release = Invoke-RestMethod -UseBasicParsing 'https://api.github.com/repos/actions/runner/releases/latest'
	$asset = $release.assets | Where-Object { $_.name -like 'actions-runner-win-x64*.zip' } | Select-Object -First 1
	if (-not $asset) { Write-Err "Could not find windows x64 runner asset in GitHub release."; exit 3 }
	$zipUrl = $asset.browser_download_url

	Write-Info "Downloading runner from $zipUrl ..."
	Invoke-WebRequest -UseBasicParsing -Uri $zipUrl -OutFile $zipPath

	Write-Info "Extracting runner to $dest ..."
	Expand-Archive -Path $zipPath -DestinationPath $dest
} else {
	Write-Info "Runner folder already exists; skipping download/extract."
}

Set-Location $dest

# Step 3: Stop any local runner-related processes to avoid duplicates
$local = Get-RunnerProcess
if ($local -and $local.Count -gt 0) {
	Write-Info "Stopping local runner-related processes to avoid duplicates..."
	$local | ForEach-Object {
		try { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue; Write-Info "Stopped PID:$($_.ProcessId)" } catch { }
	}
	Start-Sleep -Seconds 1
}

# Step 4: Obtain registration token (needed for config)
Write-Info "Requesting registration token..."
$token = gh api -X POST /repos/jgreensite/SuperHexLink/actions/runners/registration-token --jq .token 2>$null
if (-not $token) { Write-Err "Failed to obtain registration token from 'gh'."; exit 4 }

# Step 5: Configure the runner
if (-not (Test-Path (Join-Path $dest '.runner'))) {
	Write-Info "Configuring runner (this will not print the token)..."
	& .\config.cmd --unattended --url "https://github.com/jgreensite/SuperHexLink" --token $token --name $runnerName --labels "self-hosted,windows,unity"
} else { Write-Info "Runner already configured (found .runner)." }

# Step 6: Start runner in background and wait for 'Listening for Jobs'
Write-Info "Starting runner (background)..."
$proc = Start-Process -FilePath '.\run.cmd' -WorkingDirectory $dest -WindowStyle Hidden -PassThru
Write-Info "Launched run.cmd (PID: $($proc.Id)). Waiting for runner to report status..."

$pattern = 'Listening for Jobs|The session for this runner already exists|Runner execution has finished with return code 0|Runner execution has finished with return code 5'
$wait = Wait-ForLogPattern $pattern 180
if ($wait.Found) {
	Write-Info "Runner diagnostic indicates ready/connected (or reported session state). Log file: $($wait.File.Name)"
	Write-Host "--- last 80 lines of $($wait.File.Name) ---"
	Get-Content -LiteralPath $wait.File.FullName -Tail 80 | ForEach-Object { Write-Host $_ }
	Write-Info "Check the repository Actions → Settings → Runners page to confirm the runner is 'online'."
	exit 0
} else {
	Write-Err "Timed out waiting for runner diagnostic message. Showing most recent diagnostic file (if any) and current runner processes."
	$f = Get-LatestDiagFile
	if ($f) { Write-Host "--- last 200 lines of $($f.Name) ---"; Get-Content -LiteralPath $f.FullName -Tail 200 | ForEach-Object { Write-Host $_ } }
	Write-Host "Active runner-related processes:"
	Get-RunnerProcess | Select-Object ProcessId, CommandLine | ForEach-Object { Write-Host "  PID:$($_.ProcessId)  Cmd:$($_.CommandLine)" }
	Write-Err "If the runner still isn't visible, run .\run.cmd interactively from the actions-runner folder to see live output, or inspect the _diag folder for detailed errors.";
	exit 5
}
