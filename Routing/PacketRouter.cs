using System.Text.Json;
using SharpTCP.Encryption;

namespace SharpTCP.Routing;

public sealed class PacketRouter
{
    private readonly Dictionary<byte, Func<byte[], Task>> _handlers = new();
    private readonly IEncryptionProvider? _encryption;

    public event EventHandler<UnhandledPacketEventArgs>? UnhandledPacket;

    public PacketRouter(IEncryptionProvider? encryption = null)
    {
        _encryption = encryption;
    }

    public PacketRouter On<T>(byte packetType, Func<T, Task> handler) where T : class
    {
        _handlers[packetType] = async payload =>
        {
            var obj = JsonSerializer.Deserialize<T>(payload)
                ?? throw new InvalidOperationException(
                    $"Failed to deserialize packet type {packetType} as {typeof(T).Name}");
            await handler(obj).ConfigureAwait(false);
        };
        return this;
    }

    public PacketRouter On<T>(byte packetType, Action<T> handler) where T : class
    {
        _handlers[packetType] = payload =>
        {
            var obj = JsonSerializer.Deserialize<T>(payload)
                ?? throw new InvalidOperationException(
                    $"Failed to deserialize packet type {packetType} as {typeof(T).Name}");
            handler(obj);
            return Task.CompletedTask;
        };
        return this;
    }

    public async Task RouteAsync(ReadOnlyMemory<byte> rawData)
    {
        var decrypted = _encryption is not null
            ? _encryption.Decrypt(rawData.Span)
            : rawData.ToArray();

        if (decrypted.Length == 0)
            return;

        var packetType = decrypted[0];
        var payload = new byte[decrypted.Length - 1];
        Array.Copy(decrypted, 1, payload, 0, payload.Length);

        if (_handlers.TryGetValue(packetType, out var handler))
        {
            await handler(payload).ConfigureAwait(false);
        }
        else
        {
            UnhandledPacket?.Invoke(this, new UnhandledPacketEventArgs(packetType, payload));
        }
    }
}

public sealed class UnhandledPacketEventArgs : EventArgs
{
    public byte PacketType { get; }
    public ReadOnlyMemory<byte> Payload { get; }

    public UnhandledPacketEventArgs(byte packetType, byte[] payload)
    {
        PacketType = packetType;
        Payload = payload;
    }
}
