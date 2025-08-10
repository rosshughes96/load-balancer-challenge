# Load Balancer (Layer 4) — .NET 8

A minimal, protocol‑agnostic (L4) load balancer and a reusable **Core** library, both built on .NET 8.  
The repo ships with:
- a **Core library** that implements strategies, health checks, draining, metrics, and TCP proxy primitives, and
- a **demo host/API** that exposes control/observability and runs the TCP listener.
- a **Dummy TCP Echo Server** for local testing

It also includes a comprehensive test suite (NUnit + NSubstitute).

---

## ✨ Features at a glance

- **TCP proxy** (L4 data plane) with idle timeout, max lifetime, and connection caps
- **Strategies**
  - **RoundRobin** (strict, thread‑safe, deterministic)
  - **LeastQueue** (selects backend with smallest observed queue length)
- **Health checks** (periodic TCP probe; excludes draining backends)
- **Dynamic strategy switching** via configuration (and HTTP API if the demo host is used)
- **Metrics & stats** (active/total per backend)
- **Draining** (mark a backend draining; remove when safe or on timeout)
- **Structured logging** (via `Microsoft.Extensions.Logging`; demo host can use Serilog sinks)
- **Outage handling**
  - **IOutageGate/OutageGate**: transition only logging (enter/exit outage) with refused‑connection counters
  - **ITcpRefuser/TcpRefuser** with **RefusalMode** (RST or graceful FIN) to immediately refuse clients at L4 when **zero healthy backends** are available
  - Tiny accept loop backoff during full outage to avoid hot spinning
- **Tests**
  - **Unit** (AAA, log assertions, boundary cases)

- **Optional demo tooling**
  - **HTTP API** control plane
  - **Dummy TCP Echo Servers** for local testing
  - **JMeter** plan to hammer the listener
  - **Start/Stop** PowerShell scripts
  - **Postman Collection** Collection for updating configuration and getting stats

---

## Requirements

- **.NET 8 SDK** (8.0.x)
- **PowerShell** for helper scripts
- **JMeter** for load testing
- **Postman** updating configuration and getting stats

---

## Testing conventions

- **Frameworks:** NUnit + NSubstitute
- **Naming:** `Method_StateUnderTest_ExpectedBehaviour`
- **Pattern:** AAA (Arrange, Act, Assert)
- **Log assertions:** via `Common/LogTestExtensions.cs`
- **Structure:** tests mirror the production folder tree
- **Mutation resistance:** boundary checks, null/empty/duplicate inputs, exceptions, and concurrency where it matters

---

## Key components (Core)

- **Backends**
  - `BackendRegistry` (thread‑safe map; add/remove/list; normalization + logs)
- **Health**
  - `DynamicHealthChecker` (probes candidates excluding draining; exposes sorted healthy list)
- **Load balancing**
  - `RoundRobinStrategy`, `LeastQueueStrategy`
  - `StrategyProvider` (case‑insensitive name; defaults to RR)
- **Queue**
  - `BackendQueueTracker` (active connection count per backend)
- **Metrics**
  - `ConnectionMetrics` (active/total counters; aggregated snapshots)
- **Draining**
  - `DrainController` (begin/clear drain)
  - `DrainReaper` (removes drained backends when `Active==0` or timeout)
- **Diagnostics (Outage)**
  - `IOutageGate` / `OutageGate` (transition‑only logging for total‑outage state; tracks `RefusedCount` and `OutageSince`)
- **Proxy**
  - `TcpLoadBalancerService` (accepts clients; selects backend via `ILoadBalancer`; delegates to forwarder; integrates `IOutageGate` and `ITcpRefuser` to handle **zero healthy backends** deterministically)
  - `TcpRequestForwarder` (bidirectional relay with idle timeout, max lifetime, connection cap; metrics hooks)
  - **Refusal (L4)**: `ITcpRefuser` / `TcpRefuser` with `RefusalMode` (RST/FIN) for immediate, protocol‑agnostic refusal
- **Options**
  - `LoadBalancerOptions` (Backends, Strategy, HealthCheckIntervalSeconds, ListenPort)
  - `TcpForwarderOptions` (MaxConcurrentConnections, IdleTimeoutSeconds, MaxLifetimeSeconds, BufferSize)
  - Validators enforce ranges; emit debug/errors

---

## Load testing (optional)

Open JMeter → load `tools/JMeterTestPlan/LB_TCP_Test.jmx`, set **Server**=`127.0.0.1`, **Port**=listener (e.g., `6000`), adjust threads/loops, run.

---

## Notes

- The **OutageGate + ITcpRefuser integration** was an **11th‑hour change**, so I didn’t have time to write unit tests for this part yet. Everything else remains covered by the existing test suite.
