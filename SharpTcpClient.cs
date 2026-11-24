using System.Buffers;
using System.Net;
using System.Net.Sockets;
using SharpTCP.Events;
using SharpTCP.Framing;

namespace SharpTCP;

public sealed class SharpTcpClient : IDisposable
{
    private const int DefaultBufferSize = 8192;

    private readonly TcpClient _client;
    private readonly int _bufferSize;
    private readonly IMessageFramer? _framer;
    private CancellationTokenSource? _receiveCts;
    private bool _disposed;

    public Guid Id { get; } = Guid.NewGuid();

    public event EventHandler<DataReceivedEventArgs>? DataReceived;
    public event EventHandler? Disconnected;

    public bool IsConnected => _client.Connected;

    public SharpTcpClient(
        TcpClient client,
        int bufferSize = DefaultBufferSize,
        IMessageFramer? framer = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _bufferSize = bufferSize;
        _framer = framer;
    }

    public static async Task<SharpTcpClient> ConnectAsync(
        IPEndPoint endPoint,
        int bufferSize = DefaultBufferSize,
        IMessageFramer? framer = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        var client = new TcpClient();
        await client.ConnectAsync(endPoint.Address, endPoint.Port, cancellationToken)
            .ConfigureAwait(false);

        return new SharpTcpClient(client, bufferSize, framer);
    }

    public NetworkStream GetStream()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _client.GetStream();
    }

    public async Task StartReceivingAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _receiveCts.Token;

        try
        {
            var stream = _client.GetStream();

            if (_framer is not null)
                await ReceiveFramedAsync(stream, token).ConfigureAwait(false);
            else
                await ReceiveRawAsync(stream, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (IOException) when (_disposed)
        {
        }
    }

    public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var stream = _client.GetStream();

        if (_framer is not null)
            await _framer.WriteFrameAsync(stream, data, cancellationToken).ConfigureAwait(false);
        else
            await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
    }

    public void StopReceiving()
    {
        _receiveCts?.Cancel();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        _client.Dispose();
    }

    private async Task ReceiveRawAsync(NetworkStream stream, CancellationToken token)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
        try
        {
            while (!token.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(), token)
                    .ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    Disconnected?.Invoke(this, EventArgs.Empty);
                    break;
                }

                var data = new byte[bytesRead];
                buffer.AsSpan(0, bytesRead).CopyTo(data);
                DataReceived?.Invoke(this, new DataReceivedEventArgs(data));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task ReceiveFramedAsync(NetworkStream stream, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var message = await _framer!.ReadFrameAsync(stream, token).ConfigureAwait(false);
            if (message is null)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
                break;
            }

            DataReceived?.Invoke(this, new DataReceivedEventArgs(message));
        }
    }
}
