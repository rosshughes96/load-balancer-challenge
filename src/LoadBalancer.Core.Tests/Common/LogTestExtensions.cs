namespace LoadBalancerProject.Tests.Common
{
    using Microsoft.Extensions.Logging;
    using NSubstitute;
    using System;
    using System.Linq;

    /// <summary>
    /// Helpers to assert on ILogger logs produced via Microsoft.Extensions.Logging.
    /// </summary>
    internal static class LogTestExtensions
    {
        public static void AssertLogContains<T>(this ILogger<T> logger, LogLevel level, string expectedSubstring)
        {
            var ok = ((ILogger)logger).ReceivedCalls().Any(call =>
            {
                if (!string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal))
                    return false;

                var args = call.GetArguments();
                var actualLevel = (LogLevel)args[0];
                if (actualLevel != level) return false;

                var state = args[2];
                var message = state?.ToString() ?? string.Empty;
                return message.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase);
            });

            if (!ok)
            {
                var all = string.Join(Environment.NewLine, ((ILogger)logger).ReceivedCalls().Select(c =>
                {
                    var args = c.GetArguments();
                    var lvl = (LogLevel)args[0];
                    var msg = args[2]?.ToString() ?? string.Empty;
                    return $"[{lvl}] {msg}";
                }));
                throw new NUnit.Framework.AssertionException(
                    $"Expected a log at {level} containing '{expectedSubstring}', but none was found.\nLogs:\n{all}");
            }
        }

        public static int CountLogs<T>(this ILogger<T> logger, LogLevel level, string expectedSubstring)
        {
            return ((ILogger)logger).ReceivedCalls().Count(call =>
            {
                if (!string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal))
                    return false;

                var args = call.GetArguments();
                var actualLevel = (LogLevel)args[0];
                if (actualLevel != level) return false;

                var state = args[2];
                var message = state?.ToString() ?? string.Empty;
                return message.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}
