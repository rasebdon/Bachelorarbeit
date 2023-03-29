using JetBrains.Annotations;
using Netcode.Behaviour;
using Netcode.Runtime.Behaviour;
using System;
using UnityEngine;

public class NetworkVariableTest : NetworkBehaviour
{
    public NetworkVariable<int> netVar = new(10);

    public override void NetworkStart()
    {
        Debug.Log("Spawned!");
        Debug.Log($"netVar: {netVar}");

        netVar.OnValueChange += PrintValue;
    }

    private float resetTime = 0.01f;
    private float timer = 0;

    public override void NetworkUpdate()
    {
        if (Identity.IsLocalPlayer)
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
            }
            else
            {
                AddValue(1);
                timer = resetTime;
            }
        }
    }

    private void PrintValue(object arg1, object arg2)
    {
        Debug.Log($"{name}: {arg1} changed to {arg2}");
    }

    public void AddValue(int add)
    {
        netVar.SetValue(netVar.GetValue() + add);
    }
}
