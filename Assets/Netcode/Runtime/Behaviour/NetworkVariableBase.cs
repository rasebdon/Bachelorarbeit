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
        public bool IsReliable { get; }
        public ChannelType ChannelType { get;}

        private DateTime LastSet { get; set; } = DateTime.MinValue;

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
                var syncMessage = new SyncNetworkVariableMessage(data, Hash, _networkBehaviour.Identity.Guid, ChannelType);

                if (_networkBehaviour.IsClient && ClientCanWrite(_networkBehaviour.LocalClientId))
                {
                    // Send message to manipulate the variable
                    if(IsReliable)
                        NetworkHandler.Instance.SendTcpToServer(syncMessage);
                    else
                        NetworkHandler.Instance.SendUdpToServer(syncMessage);
                }
                else
                {
                    // Send sync message to clients
                    ChannelHandler.Instance.DistributeMessage(_networkBehaviour.Identity, syncMessage, ChannelType);
                }
            }
        }

        public NetworkVariableBase() : this(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Everyone, ChannelType.Environment, true) { }
        public NetworkVariableBase(object initialValue) : this(initialValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Everyone, ChannelType.Environment, true) { }
        public NetworkVariableBase(object initialValue, ChannelType channelType, bool reliable) : this(initialValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Everyone, channelType, reliable) { }
        public NetworkVariableBase(
            object initialValue,
            NetworkVariableReadPermission readPermission,
            NetworkVariableWritePermission writePermission,
            ChannelType channelType,
            bool reliable)
        {
            _value = initialValue;
            ChannelType = channelType;
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

        public void SetValueFromNetworkMessage(object value, bool? reliable, DateTime timeStamp)
        {
            if (reliable.HasValue && reliable.Value == false && timeStamp < LastSet) // Old value over UDP received
                return;
            
            LastSet = timeStamp;

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