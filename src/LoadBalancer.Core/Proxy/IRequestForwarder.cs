namespace LoadBalancerProject.Proxy
{
    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for forwarding TCP requests from a client to a backend server.
    /// </summary>
    public interface IRequestForwarder
    {
        /// <summary>
        /// Forwards an incoming TCP client connection to the specified backend server.
        /// </summary>
        /// <param name="backend">The backend server URI.</param>
        /// <param name="client">The connected TCP client.</param>
        Task ForwardAsync(Uri backend, TcpClient client);
    }
}
