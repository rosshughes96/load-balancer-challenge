namespace LoadBalancerProject.Options
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Validates <see cref="LoadBalancerOptions"/> at startup.
    /// </summary>
    public sealed class LoadBalancerOptionsValidator : IValidateOptions<LoadBalancerOptions>
    {
        private static readonly HashSet<string> AllowedStrategies =
            new(StringComparer.OrdinalIgnoreCase) { "RoundRobin", "LeastQueue" };

        private readonly ILogger<LoadBalancerOptionsValidator> _logger;

        /// <summary>
        /// Creates a new validator.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public LoadBalancerOptionsValidator(ILogger<LoadBalancerOptionsValidator> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, LoadBalancerOptions options)
        {
            var failures = new List<string>();

            if (options.ListenPort <= 0 || options.ListenPort > 65535)
            {
                failures.Add("LoadBalancer:ListenPort must be between 1 and 65535.");
            }

            if (options.HealthCheckIntervalSeconds < 1 || options.HealthCheckIntervalSeconds > 86400)
            {
                failures.Add("LoadBalancer:HealthCheckIntervalSeconds must be between 1 and 86400 seconds.");
            }

            if (string.IsNullOrWhiteSpace(options.Strategy))
            {
                failures.Add("LoadBalancer:Strategy is required.");
            }
            else if (!AllowedStrategies.Contains(options.Strategy))
            {
                failures.Add($"LoadBalancer:Strategy \"{options.Strategy}\" is not recognized. Allowed: {string.Join(", ", AllowedStrategies)}.");
            }

            if (options.Backends is null || options.Backends.Count == 0)
            {
                failures.Add("LoadBalancer:Backends must contain at least one backend URI.");
            }
            else
            {
                foreach (var (value, idx) in options.Backends.Select((v, i) => (v, i)))
                {
                    if (!TryValidateTcpUri(value, out var reason))
                    {
                        failures.Add($"LoadBalancer:Backends[{idx}] \"{value}\" is invalid: {reason}");
                    }
                }
            }

            if (failures.Count == 0)
            {
                _logger.LogDebug("LoadBalancer options validated successfully");
                return ValidateOptionsResult.Success;
            }

            _logger.LogError("LoadBalancer options validation failed: {Failures}", failures);
            return ValidateOptionsResult.Fail(failures);
        }

        /// <summary>
        /// Checks a single TCP URI value.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <param name="reason">The reason if invalid.</param>
        /// <returns>True if valid; otherwise false.</returns>
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

            if (uri.Port <= 0 || uri.Port > 65535)
            {
                reason = "port must be between 1 and 65535.";
                return false;
            }

            return true;
        }
    }
}
