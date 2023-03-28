using Netcode.Behaviour;
using Netcode.Runtime.Channeling;
using Netcode.Runtime.Integration;
using UnityEngine;

namespace Netcode.Channeling
{
    /// <summary>
    /// Subscribes itself to the collided interaction channel
    /// and its neighboring environment channels
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class AreaOfInterest : MonoBehaviour
    {
        private NetworkIdentity _identity;
        private ChannelHandler _handler;

        private void Awake()
        {
            _identity = GetComponentInParent<NetworkIdentity>();
        }

        private void Start()
        {
            _handler = NetworkHandler.Instance.GetComponent<ChannelHandler>();
        }

#if UNITY_EDITOR

        [SerializeField] bool _gizmos;

        private SphereCollider _sphereCollider;

        private void OnDrawGizmos()
        {
            if(_sphereCollider == null)
            {
                _sphereCollider = GetComponent<SphereCollider>();
            }

            Gizmos.DrawWireSphere(transform.position, _sphereCollider.radius);
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

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<ChannelComposition>() is ChannelComposition channel)
            {
                _handler.AddChannels(_identity, channel);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<ChannelComposition>() is ChannelComposition channel)
            {
                _handler.RemoveChannels(_identity, channel);
            }
        }
    }
}