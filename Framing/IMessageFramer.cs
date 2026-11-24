namespace SharpTCP.Framing;

public interface IMessageFramer
{
    Task WriteFrameAsync(Stream stream, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
    Task<byte[]?> ReadFrameAsync(Stream stream, CancellationToken cancellationToken = default);
}
