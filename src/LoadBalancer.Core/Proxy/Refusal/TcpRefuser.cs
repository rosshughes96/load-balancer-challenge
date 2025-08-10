namespace LoadBalancerProject.Proxy.Refusal
{
    using System.Net.Sockets;

    /// <summary>
    /// Default implementation of <see cref="ITcpRefuser"/> that performs an RST or graceful FIN.
    /// </summary>
    public sealed class TcpRefuser : ITcpRefuser
    {
        /// <summary>
        /// Creates a new <see cref="TcpRefuser"/> with an optional default mode (defaults to <see cref="RefusalMode.TcpReset"/>).
        /// </summary>
        public TcpRefuser(RefusalMode defaultMode = RefusalMode.TcpReset)
        {
            DefaultMode = defaultMode;
        }

        /// <inheritdoc />
        public RefusalMode DefaultMode { get; }

        /// <inheritdoc />
        public void Refuse(TcpClient client) => Refuse(client, DefaultMode);

        /// <inheritdoc />
        public void Refuse(TcpClient client, RefusalMode mode)
        {
            try
            {
                if (mode == RefusalMode.TcpReset)
                {
                    client.LingerState = new LingerOption(true, 0);
                }
                else
                {
                    try { client.Client?.Shutdown(SocketShutdown.Both); } catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }
            finally
            {
                try { client.Close(); } catch { /* ignore */ }
                client.Dispose();
            }
        }
    }
}
