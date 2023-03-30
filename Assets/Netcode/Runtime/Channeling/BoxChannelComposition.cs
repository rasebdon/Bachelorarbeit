using UnityEngine;

namespace Netcode.Runtime.Channeling
{
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public class BoxChannelComposition : Zone
    {
#if UNITY_EDITOR

        [SerializeField] bool _gizmos;

        private void OnDrawGizmos()
        {
            if (_gizmos)
            {
                Gizmos.DrawWireCube(transform.position, transform.localScale);
            }
        }

#endif
    }
}
