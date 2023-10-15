# SharpTCP
A TCP Listener &amp; TCP Client Wrapper


## Features

- Support for generic packets
- TripleDES Encryption
- Simple implementation

## Usage/Examples

Starting the TCP Server
```C#
public static void Start() {
  server.StartServer();;
  server.AcceptClients();
}
```
Starting the TCP Client
```C#
public static void Start() {
  SharpTcpClient client = new SharpTcpClient(new IPEndPoint(IPAddress.Parse("192.168.50.238"), 13371));

  client.BeginReceiveData();
}
```

Events
```C#
public static void RegisterEvents() {
SharpTCP.Core.Network.Events.ClientConnected._OnClientConnected += ClientConnected__OnClientConnected;
 SharpTCP.Core.Network.Events.DataRecived._dataRecived += DataRecived__dataRecived;
}
```

Packets
```C#
public static void Send() {
   MemoryStream ms = new MemoryStream();
 Key = Des.GenerateKey();
 var s = new Handshake(Key).ToJson();
 var data = Encoding.Default.GetBytes(s).ToList();
 data.Insert(0, 0); //header, clientinfo
 client.SendPacket(new MemoryStream(data.ToArray()));         
}

public static void Receive() {
   var h = SharpTCP.Core.Network.Packets.Packet<Handshake>.ParsePacket(new System.IO.MemoryStream(data.ToArray()));
 Console.WriteLine($"PacketType: " + h.Item2);
 Console.WriteLine($"{h.Item1.Username}, {h.Item1.MachineName}, DesKey: {h.Item1.DesKey}");
}

public class Handshake { 

    public Handshake(string Key) {
       this.DesKey = Key;
    }

    [JsonProperty]
    public string MachineName = Environment.MachineName;

    [JsonProperty]
    public string Username = Environment.UserName;

    [JsonProperty]
    public string DesKey { get; set; }
}
```



