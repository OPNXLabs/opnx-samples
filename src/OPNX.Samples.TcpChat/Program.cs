using OPNX.Lib.Network.Abstractions;
using OPNX.Lib.Network.Abstractions.Events;
using OPNX.Lib.Network.Protocol.Framing;
using OPNX.Lib.Network.Protocol.Tcp;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OPNX.Samples.TcpChat;

internal static class Program
{
    private const string DefaultHost = "127.0.0.1";
    private const int DefaultPort = 9000;
    private const byte TextPayloadType = 1;

    private static readonly PacketHeader TextPacketHeader = new(
        PacketFlags.None,
        PacketType.Notice,
        TextPayloadType);

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return 0;
        }

        using var shutdown = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            shutdown.Cancel();
        };

        string mode = args[0].ToLowerInvariant();
        string host = args.Length > 1 ? args[1] : DefaultHost;
        int port = args.Length > 2 && int.TryParse(args[2], out int parsedPort)
            ? parsedPort
            : DefaultPort;

        return mode switch
        {
            "server" => await RunServerAsync(host, port, shutdown.Token),
            "client" => await RunClientAsync(host, port, shutdown.Token),
            _ => PrintUnknownMode(mode)
        };
    }

    private static async Task<int> RunServerAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var server = new OPNXTcpServer(host, port);
        OPNXTcpClient? peer = null;
        object peerLock = new();

        server.ClientAccepted += (_, e) =>
        {
            OPNXTcpClient? previousPeer;
            lock (peerLock)
            {
                previousPeer = peer;
                peer = CreatePeer(e.Client, "client");
            }

            previousPeer?.Dispose();
            Console.WriteLine("[server] client accepted.");
        };

        server.Start();
        Console.WriteLine($"[server] listening on {host}:{port}");
        Console.WriteLine("[server] type a message and press Enter. Type 'exit' to quit.");

        await RunInputLoopAsync(
            "server",
            async message =>
            {
                OPNXTcpClient? target;
                lock (peerLock)
                    target = peer;

                if (target?.IsConnected != true)
                {
                    Console.WriteLine("[server] no connected client.");
                    return;
                }

                await SendTextAsync(target, message, cancellationToken);
            },
            cancellationToken);

        lock (peerLock)
        {
            peer?.Dispose();
            peer = null;
        }

        server.Stop();
        return 0;
    }

    private static async Task<int> RunClientAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var tcpClient = new TcpClient();

        Console.WriteLine($"[client] connecting to {host}:{port}...");
        await tcpClient.ConnectAsync(IPAddress.Parse(host), port, cancellationToken);

        using var client = CreatePeer(tcpClient, "server");
        Console.WriteLine("[client] connected.");
        Console.WriteLine("[client] type a message and press Enter. Type 'exit' to quit.");

        await RunInputLoopAsync(
            "client",
            message => SendTextAsync(client, message, cancellationToken),
            cancellationToken);

        client.Disconnect(DisconnectReason.Requested);
        return 0;
    }

    private static OPNXTcpClient CreatePeer(TcpClient tcpClient, string peerName)
    {
        var client = new OPNXTcpClient(tcpClient);
        client.PacketReceived += (_, e) => PrintReceivedMessage(peerName, e);
        client.Disconnected += (_, e) => Console.WriteLine($"[{peerName}] disconnected: {e.Reason}");
        return client;
    }

    private static async Task RunInputLoopAsync(
        string senderName,
        Func<string, Task> sendAsync,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await Task.Run(Console.ReadLine, cancellationToken);
            if (line == null)
                break;

            if (string.Equals(line, "exit", StringComparison.OrdinalIgnoreCase))
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            await sendAsync(line);
            Console.WriteLine($"[{senderName}] sent: {line}");
        }
    }

    private static async Task SendTextAsync(OPNXTcpClient client, string message, CancellationToken cancellationToken)
    {
        bool sent = await client.SendDataAsync(TextPacketHeader, message, cancellationToken);
        if (!sent)
            Console.WriteLine("[chat] send failed.");
    }

    private static void PrintReceivedMessage(string peerName, PacketReceivedEventArgs e)
    {
        string text = Encoding.UTF8.GetString(e.Payload.Span);
        Console.WriteLine($"[{peerName}] received: {text}");
    }

    private static bool IsHelp(string value)
    {
        return value is "-h" or "--help" or "/?";
    }

    private static int PrintUnknownMode(string mode)
    {
        Console.WriteLine($"Unknown mode: {mode}");
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("OPNX TCP Chat Sample");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/OPNX.Samples.TcpChat -- server [host] [port]");
        Console.WriteLine("  dotnet run --project src/OPNX.Samples.TcpChat -- client [host] [port]");
        Console.WriteLine();
        Console.WriteLine("Defaults:");
        Console.WriteLine($"  host: {DefaultHost}");
        Console.WriteLine($"  port: {DefaultPort}");
    }
}
