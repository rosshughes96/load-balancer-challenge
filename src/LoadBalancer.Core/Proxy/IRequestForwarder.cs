namespace LoadBalancerProject.Proxy
{
    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Forwards a TCP client connection to a backend server.
    /// </summary>
    public interface IRequestForwarder
    {
        /// <summary>
        /// Forwards traffic between the given client and backend.
        /// </summary>
        /// <param name="backend">The backend URI to forward to.</param>
        /// <param name="client">The incoming TCP client.</param>
        Task ForwardAsync(Uri backend, TcpClient client);
    }
}
