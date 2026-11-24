namespace SharpTCP.Events;

public sealed class ClientConnectedEventArgs(SharpTcpClient client) : EventArgs
{
    public SharpTcpClient Client { get; } = client;
}