using LoadBalancerProject.Backends;
using LoadBalancerProject.Configuration;
using LoadBalancerProject.Health;
using LoadBalancerProject.LoadBalancing;
using LoadBalancerProject.LoadBalancing.Strategies;
using LoadBalancerProject.Metrics;
using LoadBalancerProject.Options;
using LoadBalancerProject.Proxy;
using LoadBalancerProject.Queue;
using LoadBalancerProject.Strategies;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Serilog console + file
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 5 * 1024 * 1024, retainedFileCountLimit: 7)
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Host.UseSerilog();

// Options & dynamic config
builder.Services.Configure<LoadBalancerOptions>(builder.Configuration.GetSection("LoadBalancer"));
var lbOpts = builder.Configuration.GetSection("LoadBalancer").Get<LoadBalancerOptions>() ?? new LoadBalancerOptions();
builder.Services.AddSingleton<IDynamicConfig>(_ => new InMemoryDynamicConfig(lbOpts.Strategy, lbOpts.HealthCheckIntervalSeconds));

// Add hosted services
builder.Services.AddHostedService<TcpLoadBalancerService>();

// Bind TCP forwarder options
var tcpOpts = builder.Configuration.GetSection("TcpForwarder").Get<TcpForwarderOptions>() ?? new TcpForwarderOptions();
builder.Services.AddSingleton(tcpOpts);

// Registries, trackers, metrics
builder.Services.AddSingleton<IBackendRegistry, BackendRegistry>();
builder.Services.AddSingleton<IBackendQueueTracker, BackendQueueTracker>();
builder.Services.AddSingleton<IConnectionMetrics, ConnectionMetrics>();

// Strategies & provider
builder.Services.AddSingleton<RoundRobinStrategy>();
builder.Services.AddSingleton<LeastQueueStrategy>();
builder.Services.AddSingleton<IStrategyProvider, StrategyProvider>();


// Health checker + LB
builder.Services.AddSingleton<DynamicHealthChecker>();
builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<DynamicHealthChecker>());
builder.Services.AddSingleton<IHealthChecker>(sp => sp.GetRequiredService<DynamicHealthChecker>());
builder.Services.AddSingleton<ILoadBalancer, LoadBalancerProject.LoadBalancing.LoadBalancer>();

// TCP forwarder (if you host TCP listener separately)
builder.Services.AddSingleton<IRequestForwarder, TcpRequestForwarder>();

var app = builder.Build();

// Initialize backend registry from config
var registry = app.Services.GetRequiredService<IBackendRegistry>();
foreach (var s in lbOpts.Backends)
{
    if (Uri.TryCreate(s, UriKind.Absolute, out var u)) registry.Add(u);
}

// --- Config endpoints ---
app.MapGet("/config", (IDynamicConfig cfg, IBackendRegistry reg) =>
{
    return Results.Ok(new { cfg.Strategy, HealthCheckIntervalSeconds = cfg.HealthCheckIntervalSeconds, Backends = reg.List() });
});

app.MapPost("/config/strategy", ([FromBody] string strategy, IDynamicConfig cfg, IStrategyProvider prov) =>
{
    cfg.Strategy = strategy;
    prov.Refresh();
    return Results.Ok(new { Message = "Strategy updated", Strategy = strategy });
});

app.MapPost("/config/interval", ([FromBody] int seconds, IDynamicConfig cfg) =>
{
    cfg.HealthCheckIntervalSeconds = seconds;
    return Results.Ok(new { Message = "Health check interval updated", Seconds = seconds });
});

app.MapPost("/backends/add", ([FromBody] string backendUrl, IBackendRegistry reg) =>
{
    if (!Uri.TryCreate(backendUrl, UriKind.Absolute, out var uri)) return Results.BadRequest("Invalid URI");
    var ok = reg.Add(uri);
    return ok ? Results.Ok(new { Message = "Backend added", Uri = uri }) : Results.Conflict("Already exists");
});

app.MapPost("/backends/remove", ([FromBody] string backendUrl, IBackendRegistry reg) =>
{
    if (!Uri.TryCreate(backendUrl, UriKind.Absolute, out var uri)) return Results.BadRequest("Invalid URI");
    var ok = reg.Remove(uri);
    return ok ? Results.Ok(new { Message = "Backend removed", Uri = uri }) : Results.NotFound("Not found");
});

// TCP Forwarder options endpoints
app.MapGet("/tcpforwarder/config", (TcpForwarderOptions opts) => Results.Ok(opts));
app.MapPost("/tcpforwarder/config", (TcpForwarderOptions opts, [FromBody] TcpForwarderOptions newOpts) =>
{
    opts.MaxConcurrentConnections = newOpts.MaxConcurrentConnections;
    opts.IdleTimeoutSeconds = newOpts.IdleTimeoutSeconds;
    opts.MaxLifetimeSeconds = newOpts.MaxLifetimeSeconds;
    opts.BufferSize = newOpts.BufferSize;
    return Results.Ok(new { Message = "TCP Forwarder config updated", Options = opts });
});

// --- New: metrics /stats ---
app.MapGet("/stats", (IConnectionMetrics metrics) =>
{
    var snap = metrics.Snapshot();
    return Results.Ok(snap);
});

// using LoadBalancerProject.Metrics;
app.MapPost("/test/send", async ([FromBody] string message,
                                 ILoadBalancer lb,
                                 IConnectionMetrics metrics,
                                 ILoggerFactory logFactory) =>
{
    var backend = lb.SelectBackend();
    var log = logFactory.CreateLogger("TestSend");
    using (log.BeginScope(new Dictionary<string, object>
    {
        ["Backend"] = backend.ToString(),
        ["ConnectionId"] = Guid.NewGuid().ToString("N")
    }))
    {
        metrics.OnConnectionStart(backend);
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(backend.Host, backend.Port);
            var stream = client.GetStream();
            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(bytes, 0, bytes.Length);

            var buf = new byte[1024];
            var n = await stream.ReadAsync(buf, 0, buf.Length);
            var resp = System.Text.Encoding.UTF8.GetString(buf, 0, n);

            log.LogInformation("Test send completed");
            return Results.Ok(new { Backend = backend.ToString(), Response = resp });
        }
        finally
        {
            metrics.OnConnectionEnd(backend);
        }
    }
});


app.Run();