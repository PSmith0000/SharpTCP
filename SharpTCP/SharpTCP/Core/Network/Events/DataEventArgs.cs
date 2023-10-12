using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpTCP.Core.Network.Events
{
    public class DataEventArgs : EventArgs
    {
        public int DataLength;
        public ArraySegment<byte> Data;
        public DataEventArgs(ArraySegment<byte> data, int dataLength) 
        {
           DataLength = dataLength;
           Data = data;
        }

    }
}
