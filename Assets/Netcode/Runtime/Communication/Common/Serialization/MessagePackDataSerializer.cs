using MessagePack;
using MessagePack.Resolvers;
using Netcode.Runtime.Communication.Common.Exceptions;
using Netcode.Runtime.Communication.Common.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.VersionControl;

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
