using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using SharpTCP.Events;
using SharpTCP.Framing;

namespace SharpTCP;

public sealed class SharpTcpListener : IDisposable
{
    private readonly TcpListener _listener;
    private readonly int _clientBufferSize;
    private readonly IMessageFramer? _framer;
    private readonly ConcurrentDictionary<Guid, SharpTcpClient> _clients = new();
    private CancellationTokenSource? _acceptCts;
    private bool _disposed;

    public event EventHandler<ClientConnectedEventArgs>? ClientConnected;
    public event EventHandler<ClientConnectedEventArgs>? ClientDisconnected;

    public EndPoint LocalEndpoint => _listener.LocalEndpoint;

    public IReadOnlyCollection<SharpTcpClient> ConnectedClients =>
        _clients.Values.ToArray();

    public SharpTcpListener(
        IPAddress address,
        int port,
        int clientBufferSize = 8192,
        IMessageFramer? framer = null)
    {
        ArgumentNullException.ThrowIfNull(address);
        _listener = new TcpListener(address, port);
        _listener.Server.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.ReuseAddress,
            true);
        _clientBufferSize = clientBufferSize;
        _framer = framer;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _listener.Start();
    }

    public async Task AcceptClientsAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _acceptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _acceptCts.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(token)
                    .ConfigureAwait(false);

                var client = new SharpTcpClient(tcpClient, _clientBufferSize, _framer);
                _clients.TryAdd(client.Id, client);

                client.Disconnected += (_, _) =>
                {
                    if (_clients.TryRemove(client.Id, out _))
                        ClientDisconnected?.Invoke(this, new ClientConnectedEventArgs(client));
                };

                ClientConnected?.Invoke(this, new ClientConnectedEventArgs(client));
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    public async Task BroadcastAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        var tasks = _clients.Values
            .Select(c => c.SendAsync(data, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task BroadcastAsync(
        ReadOnlyMemory<byte> data,
        Guid excludeClientId,
        CancellationToken cancellationToken = default)
    {
        var tasks = _clients.Values
            .Where(c => c.Id != excludeClientId)
            .Select(c => c.SendAsync(data, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public void DisconnectClient(Guid clientId)
    {
        if (_clients.TryRemove(clientId, out var client))
            client.Dispose();
    }

    public void Stop()
    {
        _acceptCts?.Cancel();
        _listener.Stop();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _acceptCts?.Cancel();
        _acceptCts?.Dispose();

        foreach (var client in _clients.Values)
            client.Dispose();
        _clients.Clear();

        _listener.Stop();
        _listener.Server.Dispose();
    }
}
