using Netcode.Behaviour;
using System;
using UnityEngine;

public class NetworkVariableTest : NetworkBehaviour
{
    public override void NetworkStart()
    {
        Debug.Log("Spawned!");
    }

    public override void NetworkUpdate()
    {
        Debug.Log("NetworkUpdate!");
    }
}
