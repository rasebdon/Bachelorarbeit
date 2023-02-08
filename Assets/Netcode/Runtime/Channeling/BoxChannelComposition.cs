using Netcode.Behaviour;
using Netcode.Channeling;
using Netcode.Runtime.Integration;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Netcode.Runtime.Channeling
{
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public class BoxChannelComposition : ChannelComposition
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
