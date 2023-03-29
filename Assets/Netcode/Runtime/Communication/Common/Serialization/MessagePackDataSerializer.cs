using MessagePack;
using MessagePack.Resolvers;
using System;

namespace Netcode.Runtime.Communication.Common.Serialization
{
    public class MessagePackDataSerializer : IDataSerializer
    {
        private readonly MessagePackSerializerOptions _options;

        public MessagePackDataSerializer()
        {
            // Load standard resolver
            var resolver = CompositeResolver.Create(
                ContractlessStandardResolverAllowPrivate.Instance
            );

            // Set options
            _options = MessagePackSerializerOptions
                .Standard
                .WithCompression(MessagePackCompression.Lz4Block)
                .WithSecurity(MessagePackSecurity.TrustedData)
                .WithResolver(resolver);
        }

        public byte[] Serialize<T>(T obj)
        {
            return MessagePackSerializer.Serialize(obj, _options);
        }

        public byte[] Serialize(object obj, Type type)
        {
            return MessagePackSerializer.Serialize(type, obj, _options);
        }

        public object Deserialize(byte[] data, Type messageType)
        {
            return MessagePackSerializer.Deserialize(messageType, data, _options);
        }

        public T Deserialize<T>(byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data, _options);
        }
    }
}
