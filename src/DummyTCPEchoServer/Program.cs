using Serilog;
using System.Net;
using System.Net.Sockets;
using System.Text;

if (args.Length < 2)
{
    Console.WriteLine("Usage: DummyTCPEchoServer <name> <port> [delayMs]");
    return;
}

string serverName = args[0];
int port = int.Parse(args[1]);
int delayMs = args.Length >= 3 ? int.Parse(args[2]) : 0;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File($"logs/{serverName}-.log", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 5 * 1024 * 1024)
    .CreateLogger();

var listener = new TcpListener(IPAddress.IPv6Any, port);
listener.Server.DualMode = true; // accept both IPv4 and IPv6
listener.Start();

Log.Information("Server {Server} listening on port {Port} with delay {Delay}ms", serverName, port, delayMs);

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = HandleClientAsync(client);
}

async Task HandleClientAsync(TcpClient client)
{
    using var stream = client.GetStream();
    var buffer = new byte[1024];
    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
    if (bytesRead > 0)
    {
        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        if (delayMs > 0)
        {
            Log.Information("Server {Server} delaying {Delay}ms before response", serverName, delayMs);
            await Task.Delay(delayMs);
        }

        string response = $"[{serverName}] Echo: {request}";
        var respBytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(respBytes, 0, respBytes.Length);

        Log.Information("Server {Server} handled request: {Request}", serverName, request.Trim());
    }
    client.Close();
}
