namespace LoadBalancerProject.Proxy.Refusal
{
    using System.Net.Sockets;

    /// <summary>
    /// Abstraction for refusing/closing client TCP connections at layer 4 without emitting protocol bytes.
    /// </summary>
    public interface ITcpRefuser
    {
        /// <summary>
        /// Default refusal mode used when <see cref="Refuse(TcpClient)"/> is called.
        /// </summary>
        RefusalMode DefaultMode { get; }

        /// <summary>
        /// Refuse a client using <see cref="DefaultMode"/>.
        /// Must close/dispose the client.
        /// </summary>
        void Refuse(TcpClient client);

        /// <summary>
        /// Refuse a client using the provided <paramref name="mode"/>.
        /// Must close/dispose the client.
        /// </summary>
        void Refuse(TcpClient client, RefusalMode mode);
    }
}
