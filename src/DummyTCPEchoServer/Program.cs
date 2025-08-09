namespace DummyTCPEchoServer
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Minimal TCP echo server used for local testing. Echoes the request prefixed with the server name.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Entry point. Usage: <c>DummyTCPEchoServer &lt;name&gt; &lt;port&gt; [delayMs]</c>
        /// </summary>
        public static async Task Main(string[] args)
        {
            if (!TryParseArgs(args, out var serverName, out var port, out var delayMs))
            {
                Console.WriteLine("Usage: DummyTCPEchoServer <name> <port> [delayMs]");
                return;
            }

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(
                    path: $"logs/{serverName}-.log",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 5 * 1024 * 1024,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var listener = new TcpListener(IPAddress.IPv6Any, port);
            listener.Server.DualMode = true; // accept IPv4 & IPv6
            listener.Start();

            Log.Information("Server {Server} listening on port {Port} with delay {Delay}ms", serverName, port, delayMs);

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    // AcceptTcpClientAsync(CancellationToken) is available on .NET 8
                    var client = await listener.AcceptTcpClientAsync(cts.Token).ConfigureAwait(false);
                    _ = HandleClientAsync(serverName, client, delayMs, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal error in accept loop");
            }
            finally
            {
                try { listener.Stop(); } catch { /* ignore */ }
                Log.Information("Server {Server} stopped", serverName);
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Handles a single client connection: reads bytes, optional delay, echoes back with server name.
        /// </summary>
        private static async Task HandleClientAsync(string serverName, TcpClient client, int delayMs, CancellationToken ct)
        {
            try
            {
                client.NoDelay = true; // snappier echo for small payloads
                using var stream = client.GetStream();

                var buffer = new byte[1024];

                while (!ct.IsCancellationRequested)
                {
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                    if (read <= 0)
                    {
                        // EOF from client
                        break;
                    }

                    var request = Encoding.UTF8.GetString(buffer, 0, read);

                    if (delayMs > 0)
                    {
                        Log.Information("Server {Server} delaying {Delay}ms before response", serverName, delayMs);
                        await Task.Delay(delayMs, ct).ConfigureAwait(false);
                    }

                    var response = $"[{serverName}] Echo: {request}";
                    var respBytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(respBytes, 0, respBytes.Length, ct).ConfigureAwait(false);

                    Log.Information("Server {Server} handled request: {Request}", serverName, request.Trim());
                }
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error handling client");
            }
            finally
            {
                try { client.Close(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// Validates and parses command-line arguments.
        /// </summary>
        private static bool TryParseArgs(string[] args, out string serverName, out int port, out int delayMs)
        {
            serverName = string.Empty;
            port = 0;
            delayMs = 0;

            if (args.Length < 2) return false;

            serverName = args[0];

            if (!int.TryParse(args[1], out port) || port is < 1 or > 65_535)
            {
                Console.Error.WriteLine("Invalid <port>. Must be between 1 and 65535.");
                return false;
            }

            if (args.Length >= 3)
            {
                if (!int.TryParse(args[2], out delayMs) || delayMs < 0)
                {
                    Console.Error.WriteLine("Invalid [delayMs]. Must be a non-negative integer.");
                    return false;
                }
            }

            return true;
        }
    }
}
