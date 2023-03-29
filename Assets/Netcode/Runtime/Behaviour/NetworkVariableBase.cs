using Netcode.Channeling;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Integration;
using System;

namespace Netcode.Behaviour
{
    public enum NetworkVariableWritePermission
    {
        Everyone = 0,
        Owner = 1,
        Server = 2,
    }
    public enum NetworkVariableReadPermission
    {
        Everyone = 0,
        Owner = 1,
        Server = 2,
    }

    public abstract class NetworkVariableBase
    {
        public uint Hash { get; set; }

        private NetworkBehaviour _networkBehaviour;

        public NetworkVariableReadPermission ReadPermission { get; }
        public NetworkVariableWritePermission WritePermission { get; }

        private object _value;

        public object Value
        {
            get 
            {
                if (_networkBehaviour == null)
                {
                    throw new InvalidOperationException("Cannot get value of network variable before object is spawned!");
                }

                if (_networkBehaviour.IsClient && ClientCanRead(_networkBehaviour.LocalClientId) || !_networkBehaviour.IsClient)
                {
                    return _value;
                }

                throw new InvalidOperationException("Cannot get value of network variable if object has no read permissions!");
            }
            set 
            { 
                if(_networkBehaviour == null)
                {
                    throw new InvalidOperationException("Cannot set value of network variable before object is spawned!");
                }

                byte[] data = NetworkHandler.Instance.Serializer.Serialize(value);
                var syncMessage = new SyncNetworkVariableMessage(data, Hash, _networkBehaviour.Identity.Guid);

                if (_networkBehaviour.IsClient && ClientCanWrite(_networkBehaviour.LocalClientId))
                {
                    // Send message to manipulate the variable
                    NetworkHandler.Instance.SendTcp(syncMessage, 0);
                }
                else
                {
                    // Send sync message to clients
                    ChannelHandler.Instance.DistributeMessage(_networkBehaviour.Identity, syncMessage, ChannelType.Environment);
                }
            }
        }

        public NetworkVariableBase() : this(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Everyone) { }
        public NetworkVariableBase(object initialValue) : this(initialValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Everyone) { }
        public NetworkVariableBase(object initialValue, 
            NetworkVariableReadPermission readPermission = NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission writePermission = NetworkVariableWritePermission.Everyone)
        {
            _value = initialValue;
            ReadPermission = readPermission;
            WritePermission = writePermission;
        }

        private bool ClientCanWrite(uint? localClientId)
        {
            return WritePermission switch
            {
                NetworkVariableWritePermission.Everyone => true,
                NetworkVariableWritePermission.Owner => localClientId == _networkBehaviour.Identity.OwnerClientId,
                _ => false,
            };
        }

        private bool ClientCanRead(uint? localClientId)
        {
            return ReadPermission switch
            {
                NetworkVariableReadPermission.Everyone => true,
                NetworkVariableReadPermission.Owner => localClientId == _networkBehaviour.Identity.OwnerClientId,
                _ => false,
            };
        }

        public void SetValueFromNetworkMessage(object value)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnValueChange.Invoke(_value, value);
                _value = value;
            });
            
        }

        public Action<object, object> OnValueChange;

        public void SetNetworkBehaviour(NetworkBehaviour behavior)
        {
            _networkBehaviour = behavior;
        }
    }
}