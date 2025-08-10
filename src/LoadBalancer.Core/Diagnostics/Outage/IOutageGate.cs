namespace LoadBalancerProject.Diagnostics.Outage
{
    /// <summary>
    /// Emits concise, state transition logs for total backend outage scenarios and tracks refused counts.
    /// Call <see cref="OnRefusal"/> when backend selection fails due to no healthy targets,
    /// and <see cref="OnRecovered"/> when a selection subsequently succeeds.
    /// </summary>
    public interface IOutageGate
    {
        /// <summary>
        /// Record a refused connection. Should log a single warning on first refusal
        /// if transitioning from healthy to outage.
        /// </summary>
        void OnRefusal();

        /// <summary>
        /// Mark recovery from an outage. Should log once with the outage duration
        /// and how many connections were refused while down.
        /// Safe to call repeatedly; it should only log on the transition.
        /// </summary>
        void OnRecovered();

        /// <summary>
        /// Returns true when the system is currently in a total backend outage state.
        /// </summary>
        bool InOutage { get; }

        /// <summary>
        /// The UTC timestamp when the current outage began, or null if not in outage.
        /// </summary>
        DateTimeOffset? OutageSince { get; }

        /// <summary>
        /// The number of connections refused during the current outage.
        /// </summary>
        long RefusedCount { get; }
    }
}
