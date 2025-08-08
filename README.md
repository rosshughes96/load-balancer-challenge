# Load Balancer (Layer 4) — .NET 8 Demo

A minimal, **protocol-agnostic (L4)** software load balancer built with .NET 8.  
It exposes a small **HTTP API** for control/observability and runs a **TCP listener** (data plane) that forwards raw TCP streams to healthy backends using a pluggable strategy (Strict **Round Robin** or **Least Queue**).

---

## ✨ Features

- **.NET 8** console/API host
- **TCP proxy** (dual-stack IPv6/IPv4 listener)
- **Strategies**
  - **RoundRobin** (strict, thread-safe, deterministic)
  - **LeastQueue** (picks backend with smallest observed queue length)
- **Health checks** (periodic TCP probe; backends removed when down)
- **Dynamic strategy switch** via API
- **Metrics & stats** (active/total per backend) via API
- **Serilog logging** to console **and** rolling file (date-stamped w/ size-based rollover)
- **Dummy TCP Echo Server** for local testing (optional artificial delay per server)
- **JMeter test plan** to hammer the TCP listener
- **Start/Stop** PowerShell scripts (with PID tracking or process discovery fallback)
- **Tests**
  - **Unit** (NUnit + NSubstitute, AAA style, `Assert.That`)
  - **Integration** (socket round-trip through the forwarder)

---

## 📦 Repository layout

```
/src
  /LoadBalancer.Api               # HTTP control plane + hosts TCP listener service
  /LoadBalancer.Core              # Strategies, health checks, metrics, forwarder
  /DummyTCPEchoServer             # Local echo server (ServerName Port [DelayMs])
  /LoadBalancer.Core.Tests        # Unit tests (no real IO)
  /LoadBalancer.Core.IntegrationTests  # Integration tests (real sockets)
/tools
  /scripts
    Start-LB.ps1                  # Start 5 echo servers + API (optional)
    Stop-LB.ps1                   # Stop them (PID file or process search)
  /JMeterTestPlan
    LB_TCP_Test.jmx               # JMeter plan to hammer TCP listener
/logs                              # Serilog rolling files (gitignored)
README.md
```

---

## 🔧 Prerequisites

- **.NET 8 SDK**
- **PowerShell** (for the helper scripts; optional)
- **JMeter** (optional, for load tests)

---

## 🚀 Quick start (local)

### 1) Start 5 dummy backends (echo servers)

Each takes: `ServerName Port [DelayMs]`

```powershell
dotnet run --project src/DummyTCPEchoServer/DummyTCPEchoServer.csproj Server1 5001
dotnet run --project src/DummyTCPEchoServer/DummyTCPEchoServer.csproj Server2 5002
dotnet run --project src/DummyTCPEchoServer/DummyTCPEchoServer.csproj Server3 5003 2000   # slower on purpose
dotnet run --project src/DummyTCPEchoServer/DummyTCPEchoServer.csproj Server4 5004
dotnet run --project src/DummyTCPEchoServer/DummyTCPEchoServer.csproj Server5 5005
```

> Use multiple terminals, or see **Start/Stop scripts** below to launch them all at once.

### 2) Configure the Load Balancer (appsettings)

`src/LoadBalancer.Api/appsettings.json` (example):
```json
{
  "LoadBalancer": {
    "ListenPort": 6000,
    "Strategy": "RoundRobin",
    "HealthCheckIntervalSeconds": 5,
    "Backends": [
      "tcp://127.0.0.1:5001",
      "tcp://127.0.0.1:5002",
      "tcp://127.0.0.1:5003",
      "tcp://127.0.0.1:5004",
      "tcp://127.0.0.1:5005"
    ]
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/LoadBalancer-.log",
          "rollingInterval": "Day",
          "fileSizeLimitBytes": 10485760,
          "rollOnFileSizeLimit": true,
          "retainedFileCountLimit": 10,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

---

## 🔌 HTTP API (control & observability)

- `GET  /stats` → returns current metrics snapshot (per backend and totals).
- `POST /config/strategy` — change the load balancing strategy:
  ```bash
  curl -X POST http://localhost:5000/config/strategy        -H "Content-Type: application/json"        -d "\"LeastQueue\""
  ```

---

## 🧪 Testing

### Unit tests (no real IO)
```bash
dotnet test src/LoadBalancer.Core.Tests
```

### Integration tests (real sockets)
```bash
dotnet test src/LoadBalancer.Core.IntegrationTests
```

---

## 🧰 Start/Stop scripts (optional)

- **Start everything** (5 servers + API):
  ```powershell
  .\tools\scripts\Start-LB.ps1 -KeepWindowsOpen
  ```

- **Stop everything**:
  ```powershell
  .\tools\scripts\Stop-LB.ps1
  ```

---

## 📈 Load testing (JMeter)

1. Open JMeter → load `tools/JMeterTestPlan/LB_TCP_Test.jmx`.
2. Set **Server** = `127.0.0.1`, **Port** = your listener (e.g., `6000`).
3. Adjust threads and loop count, then run.