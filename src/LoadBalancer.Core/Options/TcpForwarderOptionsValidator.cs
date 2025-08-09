namespace LoadBalancerProject.Options
{
    using System.Collections.Generic;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Validates <see cref="TcpForwarderOptions"/> and produces a fail-fast error at startup.
    /// </summary>
    public sealed class TcpForwarderOptionsValidator : IValidateOptions<TcpForwarderOptions>
    {
        /// <inheritdoc/>
        public ValidateOptionsResult Validate(string? name, TcpForwarderOptions options)
        {
            var failures = new List<string>();

            if (options.MaxConcurrentConnections <= 0)
                failures.Add("TcpForwarder:MaxConcurrentConnections must be > 0.");

            if (options.IdleTimeoutSeconds < 1 || options.IdleTimeoutSeconds > 86_400)
                failures.Add("TcpForwarder:IdleTimeoutSeconds must be between 1 and 86400 seconds.");

            if (options.MaxLifetimeSeconds < 1 || options.MaxLifetimeSeconds > 86_400)
                failures.Add("TcpForwarder:MaxLifetimeSeconds must be between 1 and 86400 seconds.");

            // Optional guard: lifetime should be >= idle timeout (makes practical sense)
            if (options.MaxLifetimeSeconds < options.IdleTimeoutSeconds)
                failures.Add("TcpForwarder:MaxLifetimeSeconds should be greater than or equal to IdleTimeoutSeconds.");

            if (options.BufferSize < 1024 || options.BufferSize > 4 * 1024 * 1024)
                failures.Add("TcpForwarder:BufferSize must be between 1024 and 4194304 bytes.");

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }
    }
}
