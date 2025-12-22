using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ZakYip.Sorting.RuleEngine.Tests.Helpers;

/// <summary>
/// 简单的TCP测试服务器（仅用于测试）
/// Simple TCP test server (for testing only)
/// </summary>
public sealed class SimpleTcpTestServer : IDisposable
{
    private readonly int _port;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private readonly List<TcpClient> _clients = new();
    private readonly object _lock = new();

    public int Port => _port;
    public List<string> ReceivedMessages { get; } = new();
    public bool IsRunning { get; private set; }

    public SimpleTcpTestServer(int port)
    {
        _port = port;
    }

    public Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start();
        _cts = new CancellationTokenSource();
        _acceptTask = AcceptClientsAsync(_cts.Token);
        IsRunning = true;
        return Task.CompletedTask;
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                lock (_lock)
                {
                    _clients.Add(client);
                }
                _ = HandleClientAsync(client, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Ignore errors
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[8192];

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0) break;

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                lock (ReceivedMessages)
                {
                    ReceivedMessages.Add(message.TrimEnd('\n'));
                }
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    public async Task SendToAllClientsAsync(string message)
    {
        var data = Encoding.UTF8.GetBytes(message + "\n");
        lock (_lock)
        {
            foreach (var client in _clients.Where(c => c.Connected))
            {
                try
                {
                    client.GetStream().Write(data);
                }
                catch
                {
                    // Ignore errors
                }
            }
        }
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        
        if (_acceptTask != null)
        {
            try
            {
                await _acceptTask.ConfigureAwait(false);
            }
            catch
            {
                // Ignore
            }
        }

        lock (_lock)
        {
            foreach (var client in _clients)
            {
                client.Close();
                client.Dispose();
            }
            _clients.Clear();
        }

        _listener?.Stop();
        IsRunning = false;
    }

    public void Dispose()
    {
        StopAsync().Wait(TimeSpan.FromSeconds(5));
        _cts?.Dispose();
    }
}
