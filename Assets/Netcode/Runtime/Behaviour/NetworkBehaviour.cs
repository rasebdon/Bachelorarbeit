using Netcode.Runtime.Integration;
using UnityEngine;

namespace Netcode.Runtime.Behaviour
{
    [RequireComponent(typeof(NetworkIdentity))]
    [DisallowMultipleComponent]
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        // Helper properties
        public bool IsClient { get => NetworkHandler.Instance.IsClient; }
        public bool IsServer { get => NetworkHandler.Instance.IsServer; }
        public bool IsHost { get => NetworkHandler.Instance.IsHost; }
        public uint? LocalClientId { get => NetworkHandler.Instance.ClientId; }

        public NetworkIdentity Identity { get; private set; }

        private bool _networkStarted = false;

        private void Awake()
        {
            Identity = GetComponent<NetworkIdentity>();
            _networkStarted = false;
        }

        private void Update()
        {
            if (Identity.IsSpawned)
            {
                if (!_networkStarted)
                {
                    NetworkStart();
                    _networkStarted = true;
                }
                else
                {
                    NetworkUpdate();
                }
            }
        }

        private void FixedUpdate()
        {
            if (Identity.IsSpawned && _networkStarted)
            {
                NetworkFixedUpdate();
            }
        }

        private void LateUpdate()
        {
            if (Identity.IsSpawned && _networkStarted)
            {
                NetworkLateUpdate();
            }
        }

        public virtual void NetworkStart() { }
        public virtual void NetworkFixedUpdate() { }
        public virtual void NetworkUpdate() { }
        public virtual void NetworkLateUpdate() { }
    }
}
