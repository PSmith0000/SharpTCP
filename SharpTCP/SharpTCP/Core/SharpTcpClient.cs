using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SharpTCP.Core
{
    public class SharpTcpClient
    {
        public TcpClient _tcp { get; set; }

        private CancellationTokenSource RunningCancellationToken = new CancellationTokenSource();
        private byte[] vbuffer = new byte[125000 * 3]; //3mb
        private readonly IPEndPoint _EndPoint;

        public SharpTcpClient(IPEndPoint EndPoint)
        {
            _EndPoint = EndPoint;

            _tcp = new TcpClient();
            _tcp.Connect(EndPoint);
        }

        public SharpTcpClient(TcpClient client) {
            _tcp = client;
        }

        #region Private Fields
        private void ReceiveData(IAsyncResult Result)
        {
            TcpClient tcp = Result.AsyncState as TcpClient;
            if (tcp != null)
            {
                int data_length = tcp.Client.EndReceive(Result);
                ArraySegment<byte> DataSeg = new ArraySegment<byte>(vbuffer, 0, data_length);
                Network.Events.DataRecived.OnDataRecived(this, new Network.Events.DataEventArgs(DataSeg, data_length));
            }
        }

        private void BeginSendPacket(IAsyncResult Result)
        {
            TcpClient tcp = Result.AsyncState as TcpClient;
            if (tcp != null)
            {
                int data_sent = tcp.Client.EndSend(Result);
            }
        }
        #endregion

        #region Public Methods
        public void SendPacket(MemoryStream ms)
        {
            _tcp.Client.BeginSend(ms.ToArray(), 0, (int)ms.Length, SocketFlags.None, BeginSendPacket, _tcp);
        }

        public async Task BeginReceiveData()
        {
            CancellationToken cancellationToken = RunningCancellationToken.Token;
            await Task.Factory.StartNew(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_tcp.Client.Available != 0)
                    {
                        _tcp.Client.BeginReceive(vbuffer, 0, vbuffer.Length, SocketFlags.None, ReceiveData, _tcp);
                    }
                    Thread.Sleep(50);
                }
            });
        }

        public void StopReceiveData() 
        {
            RunningCancellationToken.Cancel();
        }
        #endregion
    }
}
