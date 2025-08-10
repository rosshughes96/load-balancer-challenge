namespace LoadBalancerProject.Diagnostics.Outage
{
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Default implementation of <see cref="IOutageGate"/> that logs only on state transitions
    /// ("enter outage" / "exit outage") and keeps simple counters for observability.
    /// </summary>
    public sealed class OutageGate : IOutageGate
    {
        private readonly ILogger _logger;
        private readonly object _lock = new();
        private bool _inOutage;
        private long _refused;
        private DateTimeOffset _outageSince;

        /// <summary>
        /// Creates a new <see cref="OutageGate"/>.
        /// </summary>
        public OutageGate(ILogger logger) => _logger = logger;

        /// <inheritdoc />
        public void OnRefusal()
        {
            lock (_lock)
            {
                if (!_inOutage)
                {
                    _inOutage = true;
                    _outageSince = DateTimeOffset.UtcNow;
                    _refused = 0;
                    _logger.LogWarning("No healthy backends; refusing new connections.");
                }
                _refused++;
            }
        }

        /// <inheritdoc />
        public void OnRecovered()
        {
            lock (_lock)
            {
                if (_inOutage)
                {
                    var duration = DateTimeOffset.UtcNow - _outageSince;
                    var refused = Interlocked.Read(ref _refused);
                    _inOutage = false;
                    _refused = 0;
                    _logger.LogInformation("Backends healthy again after {Duration}. Refused {Refused} connection(s) during outage.", duration, refused);
                }
            }
        }

        /// <inheritdoc />
        public bool InOutage
        {
            get { lock (_lock) { return _inOutage; } }
        }

        /// <inheritdoc />
        public DateTimeOffset? OutageSince
        {
            get { lock (_lock) { return _inOutage ? _outageSince : (DateTimeOffset?)null; } }
        }

        /// <inheritdoc />
        public long RefusedCount
        {
            get { lock (_lock) { return _refused; } }
        }
    }
}
