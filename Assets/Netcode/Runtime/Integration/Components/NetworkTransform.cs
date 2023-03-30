using Netcode.Runtime.Behaviour;
using Netcode.Runtime.Integration;
using System;
using UnityEngine;

public class NetworkTransform : NetworkBehaviour
{
    public NetworkVariable<Vector3> netVar = new(Vector3.zero);
    public float syncDistance = 0.1f;
    public float accuracy = 0.01f;
    public bool interpolate;

    public override void NetworkUpdate()
    {
        if(IsClient || IsHost)
        {
            if (!Identity.IsLocalPlayer && Vector3.Distance(transform.position, netVar.GetValue()) > accuracy)
            {
                Vector3 targetPosition;
                if (interpolate)
                {
                    targetPosition = Vector3.Lerp(transform.position, netVar.GetValue(), Time.deltaTime * NetworkHandler.Instance.ClientTickRate);
                }
                else
                {
                    targetPosition = netVar.GetValue();
                }

                transform.position = targetPosition;
            }
            else if(Vector3.Distance(netVar.GetValue(), transform.position) > syncDistance)
                netVar.SetValue(transform.position);
        }
    }
}
