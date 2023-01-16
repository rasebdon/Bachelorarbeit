using Netcode.Behaviour;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Netcode.Channeling
{
    /// <summary>
    /// Channel component that forwards messages between their subscribed
    /// objects and the channel handler. Is disabled on clients
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class Channel : MonoBehaviour
    {
        public uint ChannelId { get => _channelId; set => _channelId = value; }
        [SerializeField] private uint _channelId;

        [SerializeField] private List<NetworkIdentity> _subscribed;

#if UNITY_EDITOR
        private void OnValidate()
        {
            GenerateGlobalObjectIdHash();
        }

        internal void GenerateGlobalObjectIdHash()
        {
            // do NOT regenerate GlobalObjectIdHash for NetworkPrefabs while Editor is in PlayMode
            if (EditorApplication.isPlaying && !string.IsNullOrEmpty(gameObject.scene.name))
            {
                return;
            }

            // do NOT regenerate GlobalObjectIdHash if Editor is transitioning into or out of PlayMode
            if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var globalObjectIdString = GlobalObjectId.GetGlobalObjectIdSlow(this).ToString();
            _channelId = (uint)globalObjectIdString.GetHashCode();
        }
#endif // UNITY_EDITOR

        private void Start()
        {
            _subscribed = new();
        }

        public void Publish(string message)
        {
            foreach (NetworkIdentity netId in _subscribed)
            {
                netId.OnServerMessageProcess.Invoke(message);
            }
        }

        /// <summary>
        /// Subscribes a <see cref="NetworkIdentity"/> to this channel
        /// </summary>
        /// <param name="netId"></param>
        public void Subscribe(NetworkIdentity netId)
        {
            netId.OnServerMessageDistribute += Publish;
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
                netId.OnServerMessageDistribute -= Publish;
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
