using Netcode.Runtime.Behaviour;
using System;
using UnityEngine;

public class NetworkTransform : NetworkBehaviour
{
    public NetworkVariable<Vector3> netVar = new(Vector3.zero);

    public override void NetworkStart()
    {
        if ((IsClient || IsHost) && !Identity.IsLocalPlayer)
        {
            netVar.OnValueChange += OnPositionChanged;
        }
    }

    private void OnPositionChanged(Vector3 arg1, Vector3 arg2)
    {
        transform.position = arg2;
    }

    public override void NetworkFixedUpdate()
    {
        if ((IsClient || IsHost) && Identity.IsLocalPlayer)
        {
            netVar.SetValue(transform.position);
        }
    }
}
