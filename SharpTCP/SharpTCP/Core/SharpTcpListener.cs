using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharpTCP.Core
{

    public class SharpTcpListener : TcpListener, IDisposable
    {
        #region Private Fields
        private readonly IPEndPoint ListenerEndPoint;

        private bool IsStarted = false;

        private CancellationTokenSource RunningCancellationToken = new CancellationTokenSource();

        private byte[] vbuffer = new byte[125000 * 3]; //3mb
        #endregion

        #region Public Fields

        #endregion

        #region public Ctor
        public SharpTcpListener(IPAddress localaddr, int port) : base(localaddr, port)
        {
            ListenerEndPoint = new IPEndPoint(localaddr, port);
            Server.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
        }
        #endregion

        #region Private Methods
        private void BeginAccept(IAsyncResult result)
        {
            SharpTcpListener tcp = result.AsyncState as SharpTcpListener;
            if (tcp != null)
            {
                TcpClient tcpClient = tcp.EndAcceptTcpClient(result);
                Network.Events.ClientConnected.OnClientConnected(this, new Network.Events.ClientConnectedArgs(tcpClient));
            }
        }

        private void ReceiveData(IAsyncResult result)
        {
            TcpClient tcp = result.AsyncState as TcpClient;
            if (tcp != null)
            {
                int data_length = tcp.Client.EndReceive(result);
                ArraySegment<byte> DataSeg = new ArraySegment<byte>(vbuffer, 0, data_length);
                Network.Events.DataRecived.OnDataRecived(tcp, new Network.Events.DataEventArgs(DataSeg, data_length));
            }
        }

        private void BeginSendPacket(IAsyncResult result)
        {
            SharpTcpListener tcp = result.AsyncState as SharpTcpListener;
            if (tcp != null)
            {
                int data_sent = tcp.Server.EndSend(result);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts the TCP Listener
        /// </summary>
        /// <returns>Connected Status</returns>
        public bool StartServer()
        {
            if (!IsStarted)
            {
                Start();
            }
            //Warn
            return Server.Connected;
        }

        public async Task AcceptClients()
        {
            CancellationToken cancellationToken = RunningCancellationToken.Token;
            await Task.Factory.StartNew(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (Pending())
                    {
                        BeginAcceptTcpClient(BeginAccept, this);
                    }
                    Thread.Sleep(50); //CPU
                }
            });
        }

        public async Task ReceiveData(TcpClient tcp)
        {
            CancellationToken cancellationToken = RunningCancellationToken.Token;
            await Task.Factory.StartNew(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if(tcp.Client.Available != 0)
                    {
                        tcp.Client.BeginReceive(vbuffer, 0, vbuffer.Length, SocketFlags.None, ReceiveData, tcp);
                    }
                    Thread.Sleep(50);
                }
            });
        }

        public void StopAcceptingClients()
        {
            RunningCancellationToken.Cancel();
        }

        public void SendPacket(MemoryStream ms) {
            base.Server.BeginSend(ms.ToArray(), 0, (int)ms.Length, SocketFlags.None, BeginSendPacket, this);
        }

        public void Dispose()
        {
            this.RunningCancellationToken.Cancel();
            base.Stop();

            base.Server?.Dispose();
        }
        #endregion
    }
}
