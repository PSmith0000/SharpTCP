using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpTCP.Core.Network.Security;

namespace SharpTCP.Core.Network.Packets
{
    public class Packet<T> where T : class
    {
        private byte PackType { get; set; }

        private object PacketObj { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data">The data being sent</param>
        /// <param name="PacketType">a value indicating what the packet is (optional)</param>
        public Packet(T packet, byte PacketType = 0)
        {
            PacketObj = packet;
            PackType = PacketType;
        }
   
        public virtual MemoryStream CompilePacket(bool encrypt = false, string TripleDesKey = null, byte[] IV = null)
        {
            string json_string = JsonConvert.SerializeObject(PacketObj);
            List<byte> json_bytes= Encoding.Default.GetBytes(json_string).ToList();
            json_bytes.Insert(0, PackType); //PackType
            if (encrypt && TripleDesKey != null && IV != null)
            {               
                var enc_ms = Des.Encrypt(new MemoryStream(json_bytes.ToArray()), TripleDesKey, IV);
                return enc_ms;
            }
            else
            {
                return new MemoryStream(json_bytes.ToArray());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="memoryStream">recieved data</param>
        /// <param name="encrypted">data is encrpyted</param>
        /// <param name="TripleDesKey"></param>
        /// <param name="IV"></param>
        /// <returns>Class Structure and PacketID</returns>
        public static (T, int) ParsePacket(MemoryStream memoryStream, bool encrypted = false, string TripleDesKey = null, byte[] IV = null) {
            int packHead = 0;
            MemoryStream ms = memoryStream;
            if (encrypted && TripleDesKey != null && IV != null)
            {
                ms = Des.Decrypt(memoryStream, TripleDesKey, IV);
            }
            packHead = ms.ReadByte();
            string json_string = Encoding.Default.GetString(ms.ToArray());
            return (JsonConvert.DeserializeObject<T>(json_string), packHead);
        }
    }
}
