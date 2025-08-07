Write-Host "Stopping Load Balancer processes..." -ForegroundColor Cyan

function Stop-ByProcessSearch {
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

Stop-ByProcessSearch

Write-Host "Done." -ForegroundColor Cyan
