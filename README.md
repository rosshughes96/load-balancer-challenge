
# Load Balancer (Layer 4) — .NET 8

A minimal, protocol‑agnostic (L4) load balancer and a reusable **Core** library, both built on .NET 8.  
The repo ships with:
- a **Core library** that implements strategies, health checks, draining, metrics, and TCP proxy primitives, and
- a **demo host/API** (if present in your checkout) that exposes control/observability and runs the TCP listener.

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
- **Tests**
  - **Unit** (AAA, log assertions, boundary cases)
- **Optional demo tooling**
  - **HTTP API** control plane
  - **Dummy TCP Echo servers** for local testing
  - **JMeter** plan to hammer the listener
  - **Start/Stop** PowerShell scripts

---

## Requirements

- **.NET 8 SDK** (8.0.x)
- **PowerShell** for helper scripts
- **JMeter** for load testing
- **Postman** updating configuration and getting stats

---

## Testing conventions

- **Frameworks:** NUnit + NSubstitute
- **Usings:** kept *inside* each test namespace
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
- **Proxy**
  - `TcpLoadBalancerService` (accepts clients; selects backend via `ILoadBalancer`; delegates to forwarder)
  - `TcpRequestForwarder` (bidirectional relay with idle timeout, max lifetime, connection cap; metrics hooks)
- **Options**
  - `LoadBalancerOptions` (Backends, Strategy, HealthCheckIntervalSeconds, ListenPort)
  - `TcpForwarderOptions` (MaxConcurrentConnections, IdleTimeoutSeconds, MaxLifetimeSeconds, BufferSize)
  - Validators enforce ranges; emit debug/errors

---

## Load testing (optional)

Open JMeter → load `tools/JMeterTestPlan/LB_TCP_Test.jmx`, set **Server**=`127.0.0.1`, **Port**=listener (e.g., `6000`), adjust threads/loops, run.

---