using LoadBalancerProject.Backends;
using LoadBalancerProject.Configuration;
using LoadBalancerProject.Draining;
using LoadBalancerProject.Health;
using LoadBalancerProject.LoadBalancing;
using LoadBalancerProject.LoadBalancing.Strategies;
using LoadBalancerProject.Metrics;
using LoadBalancerProject.Options;
using LoadBalancerProject.Proxy;
using LoadBalancerProject.Queue;
using LoadBalancerProject.Strategies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options; // added for validation
using Serilog;

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

builder.Services.AddSingleton<IDynamicConfig>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<InMemoryDynamicConfig>>();
    return new InMemoryDynamicConfig(lbOpts.Strategy, lbOpts.HealthCheckIntervalSeconds, logger);
});

// Hosted services
builder.Services.AddHostedService<TcpLoadBalancerService>();

// TCP forwarder options
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

// TCP forwarder
builder.Services.AddSingleton<IRequestForwarder, TcpRequestForwarder>();

// Safe removal (drain) services
builder.Services.AddSingleton<DrainController>();
builder.Services.AddSingleton<IDrainController>(sp => sp.GetRequiredService<DrainController>());
builder.Services.AddHostedService<DrainReaper>();

// Validators
builder.Services.AddSingleton<IValidateOptions<LoadBalancerOptions>, LoadBalancerOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<TcpForwarderOptions>, TcpForwarderOptionsValidator>();

var app = builder.Build();

// Fail-fast validation
{
    var lbValidator = app.Services.GetRequiredService<IValidateOptions<LoadBalancerOptions>>();
    var lbResult = lbValidator.Validate(Options.DefaultName, lbOpts);
    if (lbResult.Failed)
        throw new OptionsValidationException(nameof(LoadBalancerOptions), typeof(LoadBalancerOptions), lbResult.Failures);

    var tcpValidator = app.Services.GetRequiredService<IValidateOptions<TcpForwarderOptions>>();
    var tcpResult = tcpValidator.Validate(Options.DefaultName, tcpOpts);
    if (tcpResult.Failed)
        throw new OptionsValidationException(nameof(TcpForwarderOptions), typeof(TcpForwarderOptions), tcpResult.Failures);
}

// Seed registry from config
var registry = app.Services.GetRequiredService<IBackendRegistry>();
foreach (var s in lbOpts.Backends)
{
    if (Uri.TryCreate(s, UriKind.Absolute, out var u)) registry.Add(u);
}

/// <summary>
/// Gets current config and backends.
/// </summary>
app.MapGet("/config", (IDynamicConfig cfg, IBackendRegistry reg) =>
{
    return Results.Ok(new { cfg.Strategy, HealthCheckIntervalSeconds = cfg.HealthCheckIntervalSeconds, Backends = reg.List() });
});

/// <summary>
/// Sets the strategy and refreshes the provider.
/// </summary>
app.MapPost("/config/strategy", ([FromBody] string strategy, IDynamicConfig cfg, IStrategyProvider prov) =>
{
    cfg.Strategy = strategy;
    prov.Refresh();
    return Results.Ok(new { Message = "Strategy updated", Strategy = strategy });
});

/// <summary>
/// Sets the health check interval.
/// </summary>
app.MapPost("/config/interval", ([FromBody] int seconds, IDynamicConfig cfg) =>
{
    cfg.HealthCheckIntervalSeconds = seconds;
    return Results.Ok(new { Message = "Health check interval updated", Seconds = seconds });
});

/// <summary>
/// Adds a backend to the registry.
/// </summary>
app.MapPost("/backends/add", ([FromBody] string backendUrl, IBackendRegistry reg) =>
{
    if (!Uri.TryCreate(backendUrl, UriKind.Absolute, out var uri)) return Results.BadRequest("Invalid URI");
    var ok = reg.Add(uri);
    return ok ? Results.Ok(new { Message = "Backend added", Uri = uri }) : Results.Conflict("Already exists");
});

/// <summary>
/// Starts a safe removal (drain) for a backend. New traffic stops. Removal occurs when active connections reach zero or timeout.
/// </summary>
app.MapPost("/backends/safe-remove", (string backendUrl, int? timeoutSeconds, IDrainController drain) =>
{
    if (!Uri.TryCreate(backendUrl, UriKind.Absolute, out var uri)) return Results.BadRequest("Invalid URI");
    drain.BeginDrain(uri, timeoutSeconds is > 0 ? TimeSpan.FromSeconds(timeoutSeconds.Value) : null);
    return Results.Ok(new { Message = "Safe removal started", Backend = uri.ToString(), TimeoutSeconds = timeoutSeconds });
});

/// <summary>
/// Gets a snapshot of metrics.
/// </summary>
app.MapGet("/stats", (IConnectionMetrics metrics) =>
{
    var snap = metrics.Snapshot();
    return Results.Ok(snap);
});

/// <summary>
/// Gets or updates TCP forwarder options.
/// </summary>
app.MapGet("/tcpforwarder/config", (TcpForwarderOptions opts) => Results.Ok(opts));
app.MapPost("/tcpforwarder/config", (TcpForwarderOptions opts, [FromBody] TcpForwarderOptions newOpts) =>
{
    opts.MaxConcurrentConnections = newOpts.MaxConcurrentConnections;
    opts.IdleTimeoutSeconds = newOpts.IdleTimeoutSeconds;
    opts.MaxLifetimeSeconds = newOpts.MaxLifetimeSeconds;
    opts.BufferSize = newOpts.BufferSize;
    return Results.Ok(new { Message = "TCP Forwarder config updated", Options = opts });
});

app.Run();
