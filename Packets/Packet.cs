using SharpTCP.Encryption;
using System.Text.Json;

namespace SharpTCP.Packets;

public sealed class Packet<T> where T : class
{
    public T Data { get; }
    public byte PacketType { get; }

    public Packet(T data, byte packetType = 0)
    {
        ArgumentNullException.ThrowIfNull(data);
        Data = data;
        PacketType = packetType;
    }

    public byte[] Serialize(IEncryptionProvider? encryption = null)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(Data);

        var framed = new byte[json.Length + 1];
        framed[0] = PacketType;
        json.CopyTo(framed, 1);

        return encryption is not null ? encryption.Encrypt(framed) : framed;
    }

    public static Packet<T> Deserialize(ReadOnlySpan<byte> data, IEncryptionProvider? encryption = null)
    {
        var decrypted = encryption is not null ? encryption.Decrypt(data) : [.. data];

        var packetType = decrypted[0];
        var payload = decrypted.AsSpan(1);

        var obj = JsonSerializer.Deserialize<T>(payload)
            ?? throw new InvalidOperationException($"Failed to deserialize packet as {typeof(T).Name}");

        return new Packet<T>(obj, packetType);
    }
}