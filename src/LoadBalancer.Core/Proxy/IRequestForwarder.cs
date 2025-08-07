using System.Net.Sockets;

namespace LoadBalancerProject.Proxy
{
    public interface IRequestForwarder
    {
        Task ForwardAsync(Uri backend, TcpClient client);
    }
}