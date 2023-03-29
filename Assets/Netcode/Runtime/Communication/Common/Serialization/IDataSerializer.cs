using System;

namespace Netcode.Runtime.Communication.Common.Serialization
{
    public interface IDataSerializer
    {
        public byte[] Serialize<T>(T obj);
        public byte[] Serialize(object obj, Type type);

        public object Deserialize(byte[] bytes, Type type);
        public T Deserialize<T>(byte[] bytes);
    }
}
