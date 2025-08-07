# Load Balancer (Layer 4) – Demo & Tests

This repository contains:
- **LoadBalancer.Api** – Minimal management API (dynamic config + `/stats`)
- **LoadBalancer.Core** – Library (strategies, health checks, TCP proxy, metrics)
- **DummyTCPEchoServer** – Local TCP echo server for backend simulation
- **LoadBalancer.Core.Tests** – NUnit + NSubstitute unit tests for Core

## Quick Demo

1) **Start 5 dummy servers** (Server3 slow, others fast):
```powershell
dotnet run --project src/DummyTCPEchoServer/DummyTCPEchoServer.csproj Server1 5001
dotnet run --project src/DummyTCPEchoServer/DummyTCPEchoServer.csproj Server2 5002
dotnet run --project src/DummyTCPEchoServer/DummyTCPEchoServer.csproj Server3 5003 2000
dotnet run --project src/DummyTCPEchoServer/DummyTCPEchoServer.csproj Server4 5004
dotnet run --project src/DummyTCPEchoServer/DummyTCPEchoServer.csproj Server5 5005
```

2) **Run the API** (also hosts the TCP listener):
```powershell
dotnet run --project src/LoadBalancer.Api/LoadBalancer.Api.csproj
```

3) **Hit TCP listener** (set `LoadBalancer:ListenPort`, e.g. `6000`):
```bash
echo "hello" | nc 127.0.0.1 6000
# or on Windows (PowerShell):
# $c = New-Object System.Net.Sockets.TcpClient("127.0.0.1",6000); $s=$c.GetStream(); $b=[Text.Encoding]::UTF8.GetBytes("hello"); $s.Write($b,0,$b.Length); $buf=New-Object byte[] 1024; $n=$s.Read($buf,0,$buf.Length); [Text.Encoding]::UTF8.GetString($buf,0,$n); $c.Close()
```

4) **Change strategy on-the-fly**:
```bash
curl -X POST http://localhost:5000/config/strategy -H "Content-Type: application/json" -d ""RoundRobin""
curl -X POST http://localhost:5000/config/strategy -H "Content-Type: application/json" -d ""LeastQueue""
```

5) **See live stats**:
```bash
curl http://localhost:5000/stats
```

> Use `127.0.0.1` in backend URIs to avoid IPv6 localhost issues.

## Running Unit Tests (with coverage)

```bash
dotnet test src/LoadBalancer.Core.Tests/LoadBalancer.Core.Tests.csproj --collect:"XPlat Code Coverage"
```

The tests use **NUnit** + **NSubstitute** and aim for high coverage of Core components. Assertions use **Assert.That** and include **Arrange/Act/Assert** comments for clarity.

## Project Layout

```
/src
  /LoadBalancer.Api
  /LoadBalancer.Core
  /DummyTCPEchoServer
  /LoadBalancer.Core.Tests
/tools
  /scripts            # (optional) helper scripts like Start/Stop
  /JMeterTestPlan     # LB_TCP_Test.jmx etc.
/logs                 # runtime logs (gitignored)
README.md
```

## Notes & Gotchas

- **Strict Round Robin**: implemented with an atomic counter (Interlocked) and a **stable, sorted** backend list. Ensure `StrategyProvider` injects **concrete** types to avoid DI ambiguity.
- **LeastQueue**: driven by `IBackendQueueTracker`; the TCP forwarder increments/decrements per-connection.
- **Health Checks**: prefer `tcp://127.0.0.1:PORT` backends or bind echo servers dual-stack (`DualMode = true`).
- **Logging**: Library uses `ILogger<T>`; hosts configure Serilog (console + rolling file). Scoped fields: `{Backend}`, `{ConnectionId}`.