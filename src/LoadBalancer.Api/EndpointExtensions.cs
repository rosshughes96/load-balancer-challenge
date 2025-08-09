namespace LoadBalancerProject.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LoadBalancerProject.Backends;
    using LoadBalancerProject.Configuration;
    using LoadBalancerProject.LoadBalancing.Strategies;
    using LoadBalancerProject.Metrics;
    using LoadBalancerProject.Options;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Minimal-API endpoint mapping helpers for the LoadBalancer host.
    /// </summary>
    public static class EndpointExtensions
    {
        /// <summary>
        /// Maps only the required endpoints:
        /// - GET/PUT load balancer config
        /// - GET/PUT TCP forwarder config
        /// - GET stats
        /// </summary>
        /// <param name="app">The endpoint route builder (WebApplication).</param>
        public static void MapLoadBalancerApi(this IEndpointRouteBuilder app)
        {
            if (app is null) throw new ArgumentNullException(nameof(app));

            MapLoadBalancerConfig(app);
            MapTcpForwarderConfig(app);
            MapStats(app);
        }

        /// <summary>
        /// GET/PUT endpoints for the load balancer configuration.
        /// </summary>
        private static void MapLoadBalancerConfig(IEndpointRouteBuilder app)
        {
            // GET current LB config
            app.MapGet("/config/lb", (IDynamicConfig cfg, IBackendRegistry reg) =>
            {
                return Results.Ok(new LoadBalancerConfigDto
                {
                    Strategy = cfg.Strategy,
                    HealthCheckIntervalSeconds = cfg.HealthCheckIntervalSeconds,
                    Backends = reg.List().Select(u => u.ToString()).ToList()
                });
            });

            // PUT update LB config in one shot (strategy + interval + backends)
            app.MapPut("/config/lb", (
                [FromBody] LoadBalancerConfigDto dto,
                IDynamicConfig cfg,
                IBackendRegistry reg,
                IStrategyProvider provider) =>
            {
                // Basic validation (you also have startup validators for options)
                var errors = ValidateLbDto(dto);
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                // Apply updates
                cfg.Strategy = dto.Strategy!;
                cfg.HealthCheckIntervalSeconds = dto.HealthCheckIntervalSeconds;

                // Convert and replace all backends together
                var uris = dto.Backends!.Select(s => new Uri(s, UriKind.Absolute)).ToList();
                reg.SetAll(uris);

                // Refresh the active strategy after config change
                provider.Refresh();

                return Results.Ok(new
                {
                    Message = "Load balancer configuration updated.",
                    Applied = dto
                });
            });
        }

        /// <summary>
        /// GET/PUT endpoints for the TCP forwarder configuration.
        /// </summary>
        private static void MapTcpForwarderConfig(IEndpointRouteBuilder app)
        {
            app.MapGet("/config/tcp", (TcpForwarderOptions opts) => Results.Ok(opts));

            app.MapPut("/config/tcp", (TcpForwarderOptions opts, [FromBody] TcpForwarderOptions dto) =>
            {
                var errors = ValidateTcpDto(dto);
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                // Apply updates (in-place since it’s a singleton options instance)
                opts.MaxConcurrentConnections = dto.MaxConcurrentConnections;
                opts.IdleTimeoutSeconds = dto.IdleTimeoutSeconds;
                opts.MaxLifetimeSeconds = dto.MaxLifetimeSeconds;
                opts.BufferSize = dto.BufferSize;

                return Results.Ok(new { Message = "TCP forwarder configuration updated.", Options = opts });
            });
        }

        /// <summary>
        /// GET endpoint for live metrics.
        /// </summary>
        private static void MapStats(IEndpointRouteBuilder app)
        {
            app.MapGet("/stats", (IConnectionMetrics metrics) =>
            {
                var snap = metrics.Snapshot();
                return Results.Ok(snap);
            });
        }

        // -----------------------
        // D T O s  &  Validation
        // -----------------------

        /// <summary>
        /// Request/response contract for the load balancer configuration.
        /// </summary>
        public sealed class LoadBalancerConfigDto
        {
            public string? Strategy { get; set; }
            public int HealthCheckIntervalSeconds { get; set; }
            public List<string>? Backends { get; set; }
        }

        private static Dictionary<string, string[]> ValidateLbDto(LoadBalancerConfigDto dto)
        {
            var errors = new Dictionary<string, string[]>();

            if (dto is null)
            {
                errors["body"] = new[] { "Request body is required." };
                return errors;
            }

            if (string.IsNullOrWhiteSpace(dto.Strategy))
            {
                errors[nameof(dto.Strategy)] = new[] { "Strategy is required." };
            }

            if (dto.HealthCheckIntervalSeconds < 1 || dto.HealthCheckIntervalSeconds > 86_400)
            {
                errors[nameof(dto.HealthCheckIntervalSeconds)] = new[] { "Must be between 1 and 86400 seconds." };
            }

            if (dto.Backends is null || dto.Backends.Count == 0)
            {
                errors[nameof(dto.Backends)] = new[] { "At least one backend is required." };
            }
            else
            {
                var bad = new List<string>();
                for (var i = 0; i < dto.Backends.Count; i++)
                {
                    var s = dto.Backends[i];
                    if (!Uri.TryCreate(s, UriKind.Absolute, out var u) ||
                        !u.Scheme.Equals("tcp", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(u.Host) ||
                        u.Port <= 0 || u.Port > 65_535)
                    {
                        bad.Add($"Backends[{i}] \"{s}\" is not a valid tcp://host:port URI.");
                    }
                }

                if (bad.Count > 0)
                {
                    errors[nameof(dto.Backends)] = bad.ToArray();
                }
            }

            return errors;
        }

        private static Dictionary<string, string[]> ValidateTcpDto(TcpForwarderOptions dto)
        {
            var errors = new Dictionary<string, string[]>();

            if (dto.MaxConcurrentConnections <= 0)
                errors[nameof(dto.MaxConcurrentConnections)] = new[] { "Must be > 0." };

            if (dto.IdleTimeoutSeconds < 1 || dto.IdleTimeoutSeconds > 86_400)
                errors[nameof(dto.IdleTimeoutSeconds)] = new[] { "Must be between 1 and 86400 seconds." };

            if (dto.MaxLifetimeSeconds < 1 || dto.MaxLifetimeSeconds > 86_400)
                errors[nameof(dto.MaxLifetimeSeconds)] = new[] { "Must be between 1 and 86400 seconds." };

            if (dto.MaxLifetimeSeconds < dto.IdleTimeoutSeconds)
                errors[nameof(dto.MaxLifetimeSeconds)] = new[] { "Should be >= IdleTimeoutSeconds." };

            if (dto.BufferSize < 1024 || dto.BufferSize > 4 * 1024 * 1024)
                errors[nameof(dto.BufferSize)] = new[] { "Must be between 1024 and 4194304 bytes." };

            return errors;
        }
    }
}
