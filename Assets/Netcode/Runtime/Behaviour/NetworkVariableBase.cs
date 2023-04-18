using Netcode.Channeling;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Integration;
using System;

namespace Netcode.Runtime.Behaviour
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
        public bool IsReliable { get; set; }

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
                if (_networkBehaviour == null)
                {
                    throw new InvalidOperationException("Cannot set value of network variable before object is spawned!");
                }

                byte[] data = NetworkHandler.Instance.Serializer.Serialize(value);
                var syncMessage = new SyncNetworkVariableMessage(data, Hash, _networkBehaviour.Identity.Guid);

                if (_networkBehaviour.IsClient && ClientCanWrite(_networkBehaviour.LocalClientId))
                {
                    // Send message to manipulate the variable
                    if(IsReliable)
                        NetworkHandler.Instance.SendTcp(syncMessage, 0);
                    else
                        NetworkHandler.Instance.SendUdp(syncMessage, 0);
                }
                else
                {
                    // Send sync message to clients
                    if(IsReliable)
                        ChannelHandler.Instance.DistributeMessage(_networkBehaviour.Identity, syncMessage, ChannelType.Environment);
                    else 
                        ChannelHandler.Instance.DistributeMessage(_networkBehaviour.Identity, syncMessage, ChannelType.Interaction);
                }
            }
        }

        public NetworkVariableBase() : this(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Everyone, true) { }
        public NetworkVariableBase(object initialValue) : this(initialValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Everyone, true) { }
        public NetworkVariableBase(object initialValue, bool reliable) : this(initialValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Everyone, reliable) { }
        public NetworkVariableBase(object initialValue,
            NetworkVariableReadPermission readPermission = NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission writePermission = NetworkVariableWritePermission.Everyone,
            bool reliable = true)
        {
            _value = initialValue;
            IsReliable = reliable;
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
                _onValueChange?.Invoke(_value, value);
                _value = value;
            });

        }

        protected Action<object, object> _onValueChange;

        public void SetNetworkBehaviour(NetworkBehaviour behavior)
        {
            _networkBehaviour = behavior;
        }
    }
}