<#
.SYNOPSIS
  Starts 5 Dummy TCP Echo Servers and the LoadBalancer.Api.

.PARAMETER Root
  Path to the repo root (the folder containing the .sln and /src).

.PARAMETER ApiPort
  HTTP port for the API (default 5000).

.PARAMETER ServerPorts
  Ports for the 5 dummy servers (default 5001..5005).

.PARAMETER Delays
  Optional per-server artificial delay (ms). Array of 5. Defaults to zeroes.

.PARAMETER KeepWindowsOpen
  If set, starts each process in a new PowerShell window that stays open.
#>

param(
  [string]$Root = (Resolve-Path ".").Path,
  [int]$ApiPort = 5000,
  [int[]]$ServerPorts = @(5001,5002,5003,5004,5005),
  [int[]]$Delays = @(1000,2000,3000,4000,5000),
  [switch]$KeepWindowsOpen
)

# --- Project paths (src layout) ---
$DummyProj = Join-Path $Root "src\DummyTCPEchoServer\DummyTCPEchoServer.csproj"
$ApiProj   = Join-Path $Root "src\LoadBalancer.Api\LoadBalancer.Api.csproj"

if (-not (Test-Path $DummyProj)) { throw "Not found: $DummyProj" }
if (-not (Test-Path $ApiProj))   { throw "Not found: $ApiProj" }

if ($ServerPorts.Count -ne 5) { throw "Please provide exactly 5 server ports." }
if ($Delays.Count -ne 5) {
  Write-Warning "Delays length != 5. Using zeros."
  $Delays = @(0,0,0,0,0)
}

# Logs dir (for convenience)
$logsDir = Join-Path $Root "logs"
New-Item -ItemType Directory -Force -Path $logsDir | Out-Null

Write-Host "Restoring & building projects..." -ForegroundColor Cyan
dotnet restore $DummyProj | Out-Null
dotnet restore $ApiProj   | Out-Null
dotnet build $DummyProj -c Release | Out-Null
dotnet build $ApiProj   -c Release | Out-Null

# Helpers
function Start-Windowed {
  param([string]$Title, [string]$CommandLine)
  $psArgs = @("-NoExit","-Command", $CommandLine)
  $proc = Start-Process -FilePath "powershell.exe" -ArgumentList $psArgs -PassThru
  Write-Host "Started: $Title (PID $($proc.Id))"
  return $proc
}

function Start-Headless {
  param([string]$Exe, [string]$Args)
  $proc = Start-Process -FilePath $Exe -ArgumentList $Args -PassThru
  Write-Host "Started headless: $Exe $Args (PID $($proc.Id))"
  return $proc
}

# Start dummy servers
$serverNames = @("ServerA","ServerB","ServerC","ServerD","ServerE")
Write-Host "Starting Dummy TCP Echo Servers..." -ForegroundColor Green

for ($i = 0; $i -lt 5; $i++) {
  $name  = $serverNames[$i]
  $port  = $ServerPorts[$i]
  $delay = $Delays[$i]

  $cmd = "dotnet run --project `"$DummyProj`" -- $name $port $delay"
  if ($KeepWindowsOpen) {
    $proc = Start-Windowed -Title "{$name}:$port (delay $delay ms)" -CommandLine $cmd
  } else {
    $proc = Start-Headless -Exe "dotnet" -Args "run --project `"$DummyProj`" -- $name $port $delay"
  }
  Start-Sleep -m 500
}

Start-Sleep -Seconds 1

# Start API
Write-Host "Starting LoadBalancer.Api on http://localhost:$ApiPort ..." -ForegroundColor Green
$env:ASPNETCORE_URLS = "http://localhost:$ApiPort"

$apiCmd = "dotnet run --project `"$ApiProj`""
if ($KeepWindowsOpen) {
  $apiProc = Start-Windowed -Title "LoadBalancer.Api:$ApiPort" -CommandLine $apiCmd
} else {
  $apiProc = Start-Headless -Exe "dotnet" -Args "run --project `"$ApiProj`""
}

Write-Host "`nAll started:"
for ($i = 0; $i -lt 5; $i++) {
  Write-Host "  $($serverNames[$i]) -> tcp://localhost:$($ServerPorts[$i]) (delay $($Delays[$i]) ms)"
}
Write-Host "  API -> http://localhost:$ApiPort`n"
Write-Host "Stop everything later with: .\Stop-LB.ps1 -Root `"$Root`""
