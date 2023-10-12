using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpTCP.Core.Network.Events
{
    public class ClientConnected
    {
        public static event EventHandler<ClientConnectedArgs> _OnClientConnected;

        public static void OnClientConnected(object sender, ClientConnectedArgs args) {
           if(_OnClientConnected != null) _OnClientConnected?.Invoke(sender, args);
        }
    }
}
