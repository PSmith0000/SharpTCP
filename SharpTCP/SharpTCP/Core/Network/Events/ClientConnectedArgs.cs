using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SharpTCP.Core.Network.Events
{
    public class ClientConnectedArgs : EventArgs
    {
        public TcpClient client;
        public ClientConnectedArgs(TcpClient tcp) {
          client = tcp;
        }
    }
}
