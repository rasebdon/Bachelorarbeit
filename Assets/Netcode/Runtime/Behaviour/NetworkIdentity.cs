using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Server;
using Netcode.Runtime.Integration;
using System;
using UnityEditor;
using UnityEngine;

namespace Netcode.Behaviour
{
    [DisallowMultipleComponent]
    public class NetworkIdentity : MonoBehaviour
    {
        public Guid Guid
        {
            get
            {
                if (_guid == Guid.Empty && !string.IsNullOrEmpty(_guidAsString))
                {
                    _guid = new Guid(_guidAsString);
                }
                return _guid;
            }
            set
            {
                _guid = value;
                _guidAsString = _guid.ToString();
            }
        }
        [SerializeField] private Guid _guid;
        [SerializeField] private string _guidAsString;

        // Helper properties
        public bool IsClient { get => NetworkHandler.Instance.IsClient; }
        public bool IsServer { get => NetworkHandler.Instance.IsServer; }
        public bool IsHost { get => NetworkHandler.Instance.IsHost; }

        /// <summary>
        /// Gets invoked on the server from this gameobject to notify the connected channels
        /// </summary>
        public Action<NetworkMessage> OnDistributeToChannels;

        /// <summary>
        /// Gets invoked when this network identity receives a message
        /// </summary>
        public Action<NetworkMessage> OnReceiveMessage;
    }
}
