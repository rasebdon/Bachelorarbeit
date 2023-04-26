using Netcode.Channeling;
using Netcode.Runtime.Behaviour;
using Netcode.Runtime.Communication.Client;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Integration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

    public ChannelType ChannelType;
    public bool Reliable;

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

        if(IsServer || IsHost)
        {
            NetworkIdentity.Identities.Select(x => x.Value).ToList().ForEach(no => no.OnReceiveMessage.RegisterMessageHandlerIfNotExists(
                new ActionMessageHandler<CustomNetworkMessage>(
                    Reliable ? no.ForwardTcp : no.ForwardUdp,
                    Guid.Parse("F6F32A4D-7664-40AA-9E3E-CCAD37BF2BEF"))));
        }

        currentWarmupCount = 0;
        currentRunCount = 0;

        CalculateNoise();
    }

    private void CalculateNoise()
    {
        for (int i = 0; i < noiseCancelCount + warmupCount; i++)
        {
            _stopwatch = Stopwatch.StartNew();
            _stopwatch.Stop();

            if (i < warmupCount)
                continue;

            noise.Add(_stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private void DistributeToChannel(CustomNetworkMessage msg, uint? clientId)
    {
        ChannelHandler.Instance.DistributeMessage(Identity, msg, ChannelType);
    }

    private void StopTimer(CustomNetworkMessage msg, uint? senderClientId)
    {
        if (msg.ObjectId != lastSentMessageGuid)
            return;

        _stopwatch.Stop();

        if (currentWarmupCount < warmupCount)
        {
            warmup.Add(_stopwatch.Elapsed.TotalMilliseconds);
            Debug.Log($"[{currentWarmupCount}] Warmup: {_stopwatch.Elapsed.TotalMilliseconds:0.00000}");
            currentWarmupCount++;
        }
        else if (currentRunCount < runCount)
        {
            actual.Add(_stopwatch.Elapsed.TotalMilliseconds);
            Debug.Log($"[{currentRunCount}] Actual: {_stopwatch.Elapsed.TotalMilliseconds:0.00000}");
            currentRunCount++;
        }

        if (currentWarmupCount >= warmupCount && currentRunCount >= runCount)
        {
            CreateCSV();
            Debug.Log($"Finished!");
            stop = true;
        }
    }

    [SerializeField] private float resetTime = 1f;
    private float timer = 0;
    private Guid lastSentMessageGuid = Guid.Empty;

    public int warmupCount;
    private int currentWarmupCount;
    public int noiseCancelCount;
    public int runCount;
    private int currentRunCount;

    private bool stop = false;

    private readonly List<double> warmup = new();
    private readonly List<double> actual = new();
    private readonly List<double> noise = new();

    private void CreateCSV()
    {
        string csv = "ms, measuringType\n";
        warmup.ForEach(element => csv += $"{element:0.00000}, {nameof(warmup)}\n");
        actual.ForEach(element => csv += $"{element:0.00000}, {nameof(actual)}\n");
        noise.ForEach(element => csv += $"{element:0.00000}, {nameof(noise)}\n");
        System.IO.File.WriteAllText($"C:\\Users\\rdohn\\OneDrive\\Bachelorarbeit\\Netcode_Testresults\\myNetcode_{ChannelType}_{(Reliable ? "tcp" : "udp")}_client_{NetworkHandler.Instance.ClientId}.csv", csv);
    }

    public override void NetworkUpdate()
    {
        if (stop) return;

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

                if(Reliable)
                    NetworkHandler.Instance.SendTcpToServer(new CustomNetworkMessage(lastSentMessageGuid));
                else
                    NetworkHandler.Instance.SendUdpToServer(new CustomNetworkMessage(lastSentMessageGuid));

                timer = resetTime;
            }
        }
    }
}
