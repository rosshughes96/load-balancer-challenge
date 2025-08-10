using Serilog;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Simple TCP echo server for testing.  
/// Accepts a client connection, reads a message, and echoes it back.
/// </summary>
internal static class Program
{
    private static string _serverName = string.Empty;
    private static int _port;
    private static int _delayMs;

    private static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: DummyTCPEchoServer <name> <port> [delayMs]");
            return;
        }

        _serverName = args[0];
        if (!int.TryParse(args[1], out _port) || _port <= 0 || _port > 65535)
        {
            Console.WriteLine("Invalid port number.");
            return;
        }

        _delayMs = args.Length >= 3 && int.TryParse(args[2], out var delay) ? delay : 0;

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File($"logs/{_serverName}-.log",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 5 * 1024 * 1024)
            .CreateLogger();

        var listener = new TcpListener(IPAddress.IPv6Any, _port)
        {
            Server = { DualMode = true }
        };

        listener.Start();
        Log.Information("Server {Server} listening on port {Port} with delay {Delay}ms", _serverName, _port, _delayMs);

        try
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }
        finally
        {
            try { listener.Stop(); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// Handles a single client connection, optionally delaying the echo response.
    /// </summary>
    private static async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (_delayMs > 0)
                {
                    Log.Information("Server {Server} delaying {Delay}ms before response", _serverName, _delayMs);
                    await Task.Delay(_delayMs);
                }

                var response = $"[{_serverName}] Echo: {request}";
                var respBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(respBytes, 0, respBytes.Length);

                Log.Information("Server {Server} handled request: {Request}", _serverName, request.Trim());
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling client connection");
        }
        finally
        {
            try { client.Close(); } catch { /* ignore */ }
        }
    }
}
