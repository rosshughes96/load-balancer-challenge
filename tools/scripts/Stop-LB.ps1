param(
    [string]$PidFile = "RunState_pids.json"
)

Write-Host "Stopping Load Balancer processes..." -ForegroundColor Cyan

function Stop-ByPidFile {
    param($Path)

    if (-not (Test-Path $Path)) {
        return $false
    }

    try {
        $entries = Get-Content $Path | ConvertFrom-Json
    } catch {
        Write-Warning "Failed to read PID file: $Path"
        return $false
    }

    foreach ($entry in $entries) {
        try {
            Stop-Process -Id $entry.pid -Force -ErrorAction Stop
            Write-Host "Stopped $($entry.role) PID $($entry.pid)" -ForegroundColor Green
        } catch {
            Write-Warning "Failed to stop PID $($entry.pid) ($($entry.role))"
        }
    }

    Remove-Item $Path -Force
    return $true
}

function Stop-ByProcessSearch {
    Write-Host "PID file not found — stopping by process search..." -ForegroundColor Yellow

    # Find any dotnet process that has our project names in its command line
    $targets = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" |
        Where-Object {
            $_.CommandLine -match 'DummyTCPEchoServer' -or
            $_.CommandLine -match 'LoadBalancer\.Api'
        }

    if (-not $targets) {
        Write-Warning "No matching processes found."
        return
    }

    foreach ($proc in $targets) {
        try {
            Stop-Process -Id $proc.ProcessId -Force -ErrorAction Stop
            Write-Host "Stopped process PID $($proc.ProcessId): $($proc.CommandLine)" -ForegroundColor Green
        } catch {
            Write-Warning "Failed to stop PID $($proc.ProcessId)"
        }
    }
}

# Try PID file method first, fallback to search
if (-not (Stop-ByPidFile -Path $PidFile)) {
    Stop-ByProcessSearch
}

Write-Host "Done." -ForegroundColor Cyan
