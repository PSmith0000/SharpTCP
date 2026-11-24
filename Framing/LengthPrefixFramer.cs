namespace SharpTCP.Framing;

public sealed class LengthPrefixFramer : IMessageFramer
{
    private readonly int _maxMessageSize;

    public LengthPrefixFramer(int maxMessageSize = 1024 * 1024)
    {
        if (maxMessageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxMessageSize));

        _maxMessageSize = maxMessageSize;
    }

    public async Task WriteFrameAsync(Stream stream, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var lengthPrefix = BitConverter.GetBytes(data.Length);
        await stream.WriteAsync(lengthPrefix, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]?> ReadFrameAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var lengthBuffer = new byte[4];
        if (!await ReadExactAsync(stream, lengthBuffer, cancellationToken).ConfigureAwait(false))
            return null;

        var messageLength = BitConverter.ToInt32(lengthBuffer, 0);
        if (messageLength < 0 || messageLength > _maxMessageSize)
            throw new InvalidOperationException(
                $"Message length {messageLength} exceeds maximum of {_maxMessageSize} bytes");

        if (messageLength == 0)
            return [];

        var messageBuffer = new byte[messageLength];
        if (!await ReadExactAsync(stream, messageBuffer, cancellationToken).ConfigureAwait(false))
            return null;

        return messageBuffer;
    }

    private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var bytesRead = await stream.ReadAsync(
                buffer.AsMemory(offset),
                cancellationToken).ConfigureAwait(false);

            if (bytesRead == 0)
                return false;

            offset += bytesRead;
        }
        return true;
    }
}
