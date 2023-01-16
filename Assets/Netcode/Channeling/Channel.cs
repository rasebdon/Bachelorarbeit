using Netcode.Behaviour;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netcode.Channeling
{
    [RequireComponent(typeof(Collider))]
    public class Channel : MonoBehaviour
    {
        public ushort ChannelId { get => _channelId; set => _channelId = value; }
        [SerializeField] private ushort _channelId;

        private ChannelHandler _channelHandler;

        [SerializeField] private List<NetworkIdentity> _subscribed;

        private void Awake()
        {
            _subscribed = new();
            _channelHandler = FindObjectOfType<ChannelHandler>();
            _channelHandler.RegisterChannel(this);
        }

        public void SendPublish(string message)
        {
            _channelHandler.SendPublish(message, this);
        }

        public void ReceivePublish(string message)
        {
            foreach (NetworkIdentity netId in _subscribed)
            {
                netId.OnMessageReceive.Invoke(message);
            }
        }

        /// <summary>
        /// Subscribes a <see cref="NetworkIdentity"/> to this channel
        /// </summary>
        /// <param name="netId"></param>
        public void Subscribe(NetworkIdentity netId)
        {
            netId.OnMessageSend += SendPublish;
            _subscribed.Add(netId);
        }

        /// <summary>
        /// Unsubscribes a <see cref="NetworkIdentity"/> from this channel
        /// </summary>
        /// <param name="netId"></param>
        public void Unsubscribe(NetworkIdentity netId)
        {
            if (_subscribed.Contains(netId))
            {
                netId.OnMessageSend -= SendPublish;
                _subscribed.Remove(netId);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // If a network identity enters this channels area, it gets subscribed
            if (other.GetComponent<NetworkIdentity>() is NetworkIdentity identity)
            {
                Subscribe(identity);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // If a network identity exits this channels area, it gets unsubscribed
            if (other.GetComponent<NetworkIdentity>() is NetworkIdentity identity)
            {
                Unsubscribe(identity);
            }
        }
    }
}
