using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Serialization
{
    public interface IDataSerializer
    {
        public byte[] Serialize<T>(T obj);

        public object Deserialize(byte[] bytes, Type type);
        public T Deserialize<T>(byte[] bytes);
    }
}
