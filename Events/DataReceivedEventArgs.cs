namespace SharpTCP.Events;

public sealed class DataReceivedEventArgs(ReadOnlyMemory<byte> data) : EventArgs
{
    public ReadOnlyMemory<byte> Data { get; } = data;
}