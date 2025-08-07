namespace LoadBalancerProject.Options
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System.Collections.Generic;

    /// <summary>
    /// Validates <see cref="TcpForwarderOptions"/> at startup.
    /// </summary>
    public sealed class TcpForwarderOptionsValidator : IValidateOptions<TcpForwarderOptions>
    {
        private readonly ILogger<TcpForwarderOptionsValidator> _logger;

        /// <summary>
        /// Creates a new validator.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public TcpForwarderOptionsValidator(ILogger<TcpForwarderOptionsValidator> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, TcpForwarderOptions options)
        {
            var failures = new List<string>();

            if (options.MaxConcurrentConnections <= 0)
            {
                failures.Add("TcpForwarder:MaxConcurrentConnections must be > 0.");
            }

            if (options.IdleTimeoutSeconds < 1 || options.IdleTimeoutSeconds > 86_400)
            {
                failures.Add("TcpForwarder:IdleTimeoutSeconds must be between 1 and 86400 seconds.");
            }

            if (options.MaxLifetimeSeconds < 1 || options.MaxLifetimeSeconds > 86_400)
            {
                failures.Add("TcpForwarder:MaxLifetimeSeconds must be between 1 and 86400 seconds.");
            }

            if (options.MaxLifetimeSeconds < options.IdleTimeoutSeconds)
            {
                failures.Add("TcpForwarder:MaxLifetimeSeconds should be greater than or equal to IdleTimeoutSeconds.");
            }

            if (options.BufferSize < 1024 || options.BufferSize > 4 * 1024 * 1024)
            {
                failures.Add("TcpForwarder:BufferSize must be between 1024 and 4194304 bytes.");
            }

            if (failures.Count == 0)
            {
                _logger.LogDebug("TcpForwarder options validated successfully");
                return ValidateOptionsResult.Success;
            }

            _logger.LogError("TcpForwarder options validation failed: {Failures}", failures);
            return ValidateOptionsResult.Fail(failures);
        }
    }
}
