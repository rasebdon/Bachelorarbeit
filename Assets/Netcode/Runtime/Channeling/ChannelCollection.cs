using Netcode.Behaviour;
using Netcode.Channeling;
using Netcode.Runtime.Integration;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Netcode.Runtime.Channeling
{
    /// <summary>
    /// The ChannelCollection is a component that has the two channel types
    /// (interaction and environment) as member variables and un-/subscribes
    /// network objects to those channels.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class ChannelCollection : MonoBehaviour
    {
        [SerializeField] private List<ChannelCollection> _neighbors = new();
        public List<ChannelCollection> Neighbors => _neighbors;

        public Channel InteractionChannel { get; private set; }
        public Channel EnvironmentChannel { get; private set; }

        private void Awake()
        {
            // Create channels
            EnvironmentChannel = new(ChannelType.Environment);
            InteractionChannel = new(ChannelType.Interaction, EnvironmentChannel);
        }

        private void Start()
        {
            // Get neighbor environment channels
            HashSet<Channel> environmentNeighbors = Neighbors.Select(cc => cc.EnvironmentChannel).Distinct().ToHashSet();

            // Remove own environment channel from collection
            if (environmentNeighbors.Contains(EnvironmentChannel))
            {
                environmentNeighbors.Remove(EnvironmentChannel);
            }

            // Add the neighbors to own channels
            InteractionChannel.EnvironmentNeighbors = environmentNeighbors;
            EnvironmentChannel.EnvironmentNeighbors = environmentNeighbors;
        }

#if UNITY_EDITOR

        [SerializeField] bool _gizmos;

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }

#endif

        private void Update()
        {
            if (NetworkHandler.Instance != null &&
                NetworkHandler.Instance.IsClient)
            {
                Destroy(this);
            }
        }
    }
}
