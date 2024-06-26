﻿using Netcode.Runtime.Communication.Common.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Netcode.Runtime.Communication.Common.Serialization
{
    public class DefaultMessageSerializer : IMessageSerializer
    {
        private readonly Map<short, Type> MessageTypes = new();
        private readonly IDataSerializer _serializer;

        public DefaultMessageSerializer(IDataSerializer serializer)
        {
            // Get all message types from Assembly
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => typeof(NetworkMessage).IsAssignableFrom(type)
                ).ToList().OrderBy(t => t.FullName);

            for (short i = 0; i < types.Count(); i++)
            {
                // Get type
                Type type = types.ElementAt(i);

                // Add to collections
                MessageTypes.Add(i, type);
            }

            _serializer = serializer;
        }

        public NetworkMessage Deserialize(byte[] data, Type messageType)
        {
            return _serializer.Deserialize(data, messageType) as NetworkMessage;
        }

        public Type GetMessageType(short messageTypeId)
        {
            return MessageTypes.Forward[messageTypeId];
        }

        public short GetMessageTypeId(Type messageType)
        {
            return MessageTypes.Reverse[messageType];
        }

        public byte[] Serialize<T>(T message) where T : NetworkMessage
        {
            return _serializer.Serialize(message);
        }

        public byte[] Serialize(NetworkMessage message)
        {
            return _serializer.Serialize(message, message.GetType());
        }
    }

    public class Map<T1, T2> : IEnumerable<KeyValuePair<T1, T2>>
    {
        private readonly Dictionary<T1, T2> _forward = new Dictionary<T1, T2>();
        private readonly Dictionary<T2, T1> _reverse = new Dictionary<T2, T1>();

        public Map()
        {
            Forward = new Indexer<T1, T2>(_forward);
            Reverse = new Indexer<T2, T1>(_reverse);
        }

        public Indexer<T1, T2> Forward { get; private set; }
        public Indexer<T2, T1> Reverse { get; private set; }

        public void Add(T1 t1, T2 t2)
        {
            _forward.Add(t1, t2);
            _reverse.Add(t2, t1);
        }

        public void Remove(T1 t1)
        {
            T2 revKey = Forward[t1];
            _forward.Remove(t1);
            _reverse.Remove(revKey);
        }

        public void Remove(T2 t2)
        {
            T1 forwardKey = Reverse[t2];
            _reverse.Remove(t2);
            _forward.Remove(forwardKey);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator()
        {
            return _forward.GetEnumerator();
        }

        public class Indexer<T3, T4>
        {
            private readonly Dictionary<T3, T4> _dictionary;

            public Indexer(Dictionary<T3, T4> dictionary)
            {
                _dictionary = dictionary;
            }

            public T4 this[T3 index]
            {
                get { return _dictionary[index]; }
                set { _dictionary[index] = value; }
            }

            public bool Contains(T3 key)
            {
                return _dictionary.ContainsKey(key);
            }
        }
    }
}
