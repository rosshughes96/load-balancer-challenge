namespace LoadBalancerProject.Options
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Validates <see cref="LoadBalancerOptions"/> and produces a fail-fast error at startup.
    /// </summary>
    public sealed class LoadBalancerOptionsValidator : IValidateOptions<LoadBalancerOptions>
    {
        private static readonly HashSet<string> AllowedStrategies =
            new(StringComparer.OrdinalIgnoreCase) { "RoundRobin", "LeastQueue" };

        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, LoadBalancerOptions options)
        {
            var failures = new List<string>();

            // ListenPort: must be 1..65535
            if (options.ListenPort <= 0 || options.ListenPort > 65_535)
                failures.Add("LoadBalancer:ListenPort must be between 1 and 65535.");

            // HealthCheckIntervalSeconds: >= 1 and sensible upper bound
            if (options.HealthCheckIntervalSeconds < 1 || options.HealthCheckIntervalSeconds > 86_400)
                failures.Add("LoadBalancer:HealthCheckIntervalSeconds must be between 1 and 86400 seconds.");

            // Strategy: non-empty and (optionally) one of known values
            if (string.IsNullOrWhiteSpace(options.Strategy))
            {
                failures.Add("LoadBalancer:Strategy is required.");
            }
            else if (!AllowedStrategies.Contains(options.Strategy))
            {
                failures.Add($"LoadBalancer:Strategy \"{options.Strategy}\" is not recognized. Allowed: {string.Join(", ", AllowedStrategies)}.");
            }

            // Backends: must be non-empty and valid tcp:// URIs with host + port
            if (options.Backends is null || options.Backends.Count == 0)
            {
                failures.Add("LoadBalancer:Backends must contain at least one backend URI.");
            }
            else
            {
                foreach (var (value, idx) in options.Backends.Select((v, i) => (v, i)))
                {
                    if (!TryValidateTcpUri(value, out var reason))
                        failures.Add($"LoadBalancer:Backends[{idx}] \"{value}\" is invalid: {reason}");
                }
            }

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }

        // Validates tcp://host:port format
        private static bool TryValidateTcpUri(string? s, out string reason)
        {
            reason = string.Empty;

            if (string.IsNullOrWhiteSpace(s))
            {
                reason = "value is null/empty.";
                return false;
            }

            if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
            {
                reason = "not a valid absolute URI.";
                return false;
            }

            if (!uri.Scheme.Equals("tcp", StringComparison.OrdinalIgnoreCase))
            {
                reason = "scheme must be tcp://.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(uri.Host))
            {
                reason = "host is missing.";
                return false;
            }

            if (uri.Port <= 0 || uri.Port > 65_535)
            {
                reason = "port must be between 1 and 65535.";
                return false;
            }

            return true;
        }
    }
}
