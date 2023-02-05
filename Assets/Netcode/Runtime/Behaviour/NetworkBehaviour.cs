using Netcode.Runtime.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Netcode.Behaviour
{
    [RequireComponent(typeof(NetworkIdentity))]
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        // Helper properties
        public bool IsClient { get => NetworkHandler.Instance.IsClient; }
        public bool IsServer { get => NetworkHandler.Instance.IsServer; }
        public bool IsHost { get => NetworkHandler.Instance.IsHost; }

        public NetworkIdentity Identity { get; private set; }

        private bool _networkStarted = false;

        private void Awake()
        {
            Identity = GetComponent<NetworkIdentity>();
            _networkStarted = false;
        }

        public void Start()
        {
            if (IsClient) 
            {
                _networkStarted = true;
            }
        }

        private void Update()
        {
            if (Identity.Instantiated)
            {
                if (!_networkStarted)
                {
                    NetworkStart();
                }
                else
                {
                    NetworkUpdate();
                }
            }
        }

        private void FixedUpdate()
        {
            if(Identity.Instantiated && _networkStarted)
            {
                NetworkFixedUpdate();
            }
        }

        private void LateUpdate()
        {
            if(Identity.Instantiated && _networkStarted)
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
