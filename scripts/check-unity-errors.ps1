<#
.SYNOPSIS
    Displays Unity Editor compilation errors and warnings from the log file.

.DESCRIPTION
    Parses Unity's Editor.log to show compilation errors, warnings, and script compilation status.
    Useful for debugging without opening Unity Editor.

.PARAMETER Lines
    Number of log lines to display. Default: 100

.PARAMETER ErrorsOnly
    Show only errors, skip warnings

.PARAMETER Follow
    Continuously tail the log file (like tail -f)

.PARAMETER TriggerRecompile
    Touch a marker file to trigger Unity recompilation before checking logs

.PARAMETER Wait
    When used with -TriggerRecompile, waits N seconds for Unity to process changes (default: 3)

.EXAMPLE
    .\scripts\check-unity-errors.ps1
    Shows last 100 lines including errors and warnings

.EXAMPLE
    .\scripts\check-unity-errors.ps1 -ErrorsOnly
    Shows only compilation errors

.EXAMPLE
    .\scripts\check-unity-errors.ps1 -Follow
    Continuously monitors Unity log for new errors
    
.EXAMPLE
    .\scripts\check-unity-errors.ps1 -TriggerRecompile -Wait 5
    Triggers Unity recompilation, waits 5 seconds, then checks for errors
#>

param(
    [int]$Lines = 100,
    [switch]$ErrorsOnly,
    [switch]$Follow,
    [switch]$TriggerRecompile,
    [switch]$ActionLog,
    [switch]$All,
    [int]$Wait = 3
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Trigger recompilation if requested
if ($TriggerRecompile) {
    $triggerScript = Join-Path $PSScriptRoot "trigger-unity-recompile.ps1"
    if (Test-Path $triggerScript) {
        Write-Host "Triggering Unity recompilation..." -ForegroundColor Yellow
        & $triggerScript -Wait -WaitSeconds $Wait
        Write-Host ""
    } else {
        Write-Warning "trigger-unity-recompile.ps1 not found, skipping recompilation trigger"
    }
}

$unityLogPath = "$env:LOCALAPPDATA\Unity\Editor\Editor.log"

if (-not (Test-Path $unityLogPath)) {
    Write-Error "Unity Editor log not found at: $unityLogPath`nMake sure Unity has been run at least once."
    exit 1
}

    Write-Host "=== Unity Editor Log Analysis ===" -ForegroundColor Cyan
    Write-Host "Log file: $unityLogPath" -ForegroundColor Gray
    Write-Host ""

if ($Follow) {
    Write-Host "Following Unity log (Ctrl+C to stop)..." -ForegroundColor Yellow
    Write-Host ""
    Get-Content $unityLogPath -Wait -Tail $Lines
}
else {
    $logContent = Get-Content $unityLogPath -Tail $Lines

        if ($All) {
            Write-Host "=== LAST $Lines LOG LINES ===" -ForegroundColor Cyan
            foreach ($line in $logContent) {
                Write-Host $line
            }
            Write-Host ""
            Write-Host "For errors/warnings run without -All or use -ErrorsOnly after viewing the log." -ForegroundColor Gray
            exit 0
        }

        if ($ActionLog) {
            $actionLines = @($logContent | Where-Object { $_ -match '\[ActionLog/' })
            Write-Host "=== ACTION LOG ENTRIES ===" -ForegroundColor Cyan
            if ($actionLines.Count -eq 0) {
                Write-Host "No [ActionLog/] entries detected in the last $Lines lines." -ForegroundColor Gray
            } else {
                foreach ($entry in $actionLines) {
                    Write-Host $entry
                }
            }
            Write-Host ""
        }

    # Extract compilation errors
    $compileErrors = @($logContent | Where-Object { $_ -match '\): error CS\d+:' })
    
    # Extract compilation warnings
    $compileWarnings = @($logContent | Where-Object { $_ -match '\): warning CS\d+:' })
    
    # Extract runtime exceptions (get the exception line itself)
    $runtimeExceptions = @()
    for ($i = 0; $i -lt $logContent.Count; $i++) {
        if ($logContent[$i] -match '(NullReferenceException|ArgumentException|ArgumentOutOfRangeException|IndexOutOfRangeException|InvalidOperationException|KeyNotFoundException|MissingReferenceException):') {
            # Capture the exception and next 2 lines for context
            $context = @($logContent[$i])
            if ($i + 1 -lt $logContent.Count) { $context += $logContent[$i + 1] }
            if ($i + 2 -lt $logContent.Count) { $context += $logContent[$i + 2] }
            $runtimeExceptions += ($context -join "`n  ")
        }
    }
    
    # Extract log errors and warnings (with actual message content)
    $logErrors = @()
    $logWarnings = @()
    $seenLogEntries = [System.Collections.Generic.HashSet[string]]::new()

    function Get-LogLocation($lines, [int]$startIndex) {
        for ($k = $startIndex; $k -lt [Math]::Min($startIndex + 10, $lines.Count); $k++) {
            if ($lines[$k] -match '\(at Assets/') {
                return $lines[$k].Trim()
            }
        }
        return "Location not found"
    }

    function Get-LogMessageBefore($lines, [int]$index) {
        for ($k = $index - 1; $k -ge 0 -and ($index - $k) -le 5; $k--) {
            $candidate = $lines[$k].Trim()
            if ($candidate -and $candidate -notmatch '^UnityEngine') {
                return $candidate
            }
        }
        return ""
    }

    function Add-LogEntry($message, $location, $severity) {
        if (-not $message) { return }
        $locationText = if ($location) { $location } else { "Location not found" }
        $entryKey = "$severity|$message|$locationText"
        if ($seenLogEntries.Contains($entryKey)) { return }
        $null = $seenLogEntries.Add($entryKey)
        $entry = "$message`n  At: $locationText"
        if ($severity -eq 'error') {
            $logErrors += $entry
        } else {
            $logWarnings += $entry
        }
    }

    for ($i = 0; $i -lt $logContent.Count; $i++) {
        $line = $logContent[$i]

        if ($line -match 'UnityEngine\.StackTraceUtility:ExtractStackTrace') {
            $message = Get-LogMessageBefore $logContent $i
            $location = Get-LogLocation $logContent ($i + 1)
            $severity = 'warning'
            for ($j = $i; $j -lt [Math]::Min($i + 5, $logContent.Count); $j++) {
                if ($logContent[$j] -match 'Debug:LogError') {
                    $severity = 'error'
                    break
                }
                if ($logContent[$j] -match 'Debug:LogWarning') {
                    $severity = 'warning'
                    break
                }
            }
            Add-LogEntry $message $location $severity
        }
        elseif ($line -match 'UnityEngine\.Debug:Log(Error|Warning)') {
            $message = Get-LogMessageBefore $logContent $i
            $location = Get-LogLocation $logContent ($i + 1)
            $severity = if ($line -match 'LogError') { 'error' } else { 'warning' }
            Add-LogEntry $message $location $severity
        }
    }

    # Check for compilation status
    $compileStart = $logContent | Where-Object { $_ -match 'Compilation Start' }
    $compileFinish = $logContent | Where-Object { $_ -match 'CompilationPipeline::Finished' }
    $compileFailed = $logContent | Where-Object { $_ -match 'CompilationPipeline: Failed' }

    # Display compilation status
    if ($compileStart) {
        Write-Host "[STATUS] Compilation detected in log" -ForegroundColor Yellow
    }
    
    if ($compileFailed) {
        Write-Host "[STATUS] Compilation FAILED" -ForegroundColor Red
    }
    elseif ($compileFinish) {
        Write-Host "[STATUS] Compilation completed successfully" -ForegroundColor Green
    }

    Write-Host ""

    # Display errors
    if ($compileErrors.Count -gt 0) {
        Write-Host "=== COMPILATION ERRORS ($($compileErrors.Count)) ===" -ForegroundColor Red
        foreach ($err in $compileErrors) {
            Write-Host $err -ForegroundColor Red
        }
        Write-Host ""
    }
    
    if ($runtimeExceptions.Count -gt 0) {
        Write-Host "=== RUNTIME EXCEPTIONS ($($runtimeExceptions.Count)) ===" -ForegroundColor Red
        foreach ($err in $runtimeExceptions) {
            Write-Host $err -ForegroundColor Red
        }
        Write-Host ""
    }
    
    if ($logErrors.Count -gt 0) {
        Write-Host "=== LOG ERRORS ($($logErrors.Count)) ===" -ForegroundColor Red
        foreach ($err in $logErrors) {
            Write-Host $err -ForegroundColor Red
        }
        Write-Host ""
    }
    
    if ($compileErrors.Count -eq 0 -and $runtimeExceptions.Count -eq 0 -and $logErrors.Count -eq 0) {
        Write-Host "No errors found" -ForegroundColor Green
        Write-Host ""
    }

    # Display warnings (unless ErrorsOnly)
    if (-not $ErrorsOnly) {
        if ($compileWarnings.Count -gt 0) {
            Write-Host "=== COMPILATION WARNINGS ($($compileWarnings.Count)) ===" -ForegroundColor Yellow
            foreach ($warn in $compileWarnings) {
                Write-Host $warn -ForegroundColor Yellow
            }
            Write-Host ""
        }
        
        if ($logWarnings.Count -gt 0) {
            Write-Host "=== LOG WARNINGS ($($logWarnings.Count)) ===" -ForegroundColor Yellow
            foreach ($warn in $logWarnings) {
                Write-Host $warn -ForegroundColor Yellow
            }
            Write-Host ""
        }
        
        if ($compileWarnings.Count -eq 0 -and $logWarnings.Count -eq 0) {
            Write-Host "No warnings found" -ForegroundColor Green
            Write-Host ""
        }
    }

    # Provide summary
    Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
    Write-Host "Compilation Errors: $($compileErrors.Count)" -ForegroundColor $(if ($compileErrors.Count -gt 0) { "Red" } else { "Green" })
    Write-Host "Runtime Exceptions: $($runtimeExceptions.Count)" -ForegroundColor $(if ($runtimeExceptions.Count -gt 0) { "Red" } else { "Green" })
    Write-Host "Log Errors:         $($logErrors.Count)" -ForegroundColor $(if ($logErrors.Count -gt 0) { "Red" } else { "Green" })
    if (-not $ErrorsOnly) {
        Write-Host "Compilation Warnings: $($compileWarnings.Count)" -ForegroundColor $(if ($compileWarnings.Count -gt 0) { "Yellow" } else { "Green" })
        Write-Host "Log Warnings:         $($logWarnings.Count)" -ForegroundColor $(if ($logWarnings.Count -gt 0) { "Yellow" } else { "Green" })
    }

    # Exit code
    $totalErrors = $compileErrors.Count + $runtimeExceptions.Count + $logErrors.Count
    if ($totalErrors -gt 0) {
        Write-Host "`nUnity has $totalErrors error(s). Check details above." -ForegroundColor Red
        exit 1
    }
    else {
        Write-Host "`nNo errors found. Unity should be running cleanly." -ForegroundColor Green
        exit 0
    }
}
