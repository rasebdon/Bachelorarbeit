using Netcode.Channeling;
using Netcode.Runtime.Behaviour;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Integration;
using System;
using System.Diagnostics;
using UnityEngine;

public class CustomNetworkMessage : NetworkMessage
{
    public CustomNetworkMessage(Guid objectId)
    {
        ObjectId = objectId;
    }

    public Guid ObjectId { get; set; }
}

public class CustomMessageTest : NetworkBehaviour
{
    private Stopwatch _stopwatch;

    public override void NetworkStart()
    {
        var serverMessageHandler = new ActionMessageHandler<CustomNetworkMessage>(DistributeToChannel, Guid.Parse("3F7D9B00-4F56-4F8D-B8D3-7C0173C452A5"));
        var clientMessageHandler = new ActionMessageHandler<CustomNetworkMessage>(StopTimer, Guid.Parse("4F7D9B00-4F56-4F8D-B8D3-7C0173C452A5"));

        if (IsServer)
        {
            NetworkHandler.Instance.ServerMessageHandlerRegistry.RegisterMessageHandlerIfNotExists(serverMessageHandler);
        }
        else if (IsClient)
        {
            NetworkHandler.Instance.ClientMessageHandlerRegistry.RegisterMessageHandlerIfNotExists(clientMessageHandler);
        }
        else if (IsHost)
        {
            NetworkHandler.Instance.ServerMessageHandlerRegistry.RegisterMessageHandlerIfNotExists(serverMessageHandler);
            NetworkHandler.Instance.ClientMessageHandlerRegistry.RegisterMessageHandlerIfNotExists(clientMessageHandler);
        }

        if(NetworkHandler.Instance.LocalPlayer != null)
            NetworkHandler.Instance.LocalPlayer.OnReceiveMessage += ForwardToClient;
    }

    private void DistributeToChannel(CustomNetworkMessage msg, uint? clientId)
    {
        ChannelHandler.Instance.DistributeMessage(Identity, msg, ChannelType.Environment);
    }

    private void ForwardToClient(NetworkMessage msg)
    {
        if (msg is CustomNetworkMessage msg2)
            NetworkHandler.Instance.SendTcpToClient(msg2, NetworkHandler.Instance.LocalPlayer.OwnerClientId);
    }

    private void StopTimer(CustomNetworkMessage msg, uint? senderClientId)
    {
        if (msg.ObjectId != lastSentMessageGuid)
            return;

        _stopwatch.Stop();
        UnityEngine.Debug.Log($"RTT: {_stopwatch.ElapsedMilliseconds:0.00000} ms");
    }

    private float resetTime = 1f;
    private float timer = 0;
    private Guid lastSentMessageGuid = Guid.Empty;

    public override void NetworkUpdate()
    {
        if (IsClient || IsHost)
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
            }
            else if (_stopwatch == null || !_stopwatch.IsRunning)
            {
                lastSentMessageGuid = Guid.NewGuid();

                _stopwatch = Stopwatch.StartNew();
                NetworkHandler.Instance.SendTcpToServer(new CustomNetworkMessage(lastSentMessageGuid));
                timer = resetTime;
            }
        }
    }
}
