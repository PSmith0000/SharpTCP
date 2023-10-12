using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpTCP.Core.Network.Events
{
    public class DataRecived
    {
        public static event EventHandler<DataEventArgs> _dataRecived;

        public static void OnDataRecived(object sender, DataEventArgs e) {
           if (_dataRecived != null)
            {
                _dataRecived?.Invoke(sender, e);
            }
        }
    }
}
