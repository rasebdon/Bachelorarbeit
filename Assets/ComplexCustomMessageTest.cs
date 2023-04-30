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

public class ComplexCustomNetworkMessage : NetworkMessage
{
    public string SomeString;
    public int SomeInt;
    public float SomeFloat;
    public double SomeDouble;
    public long SomeLong;
    public Vector3 SomeVector;
    public Guid SomeGuid;

    public ComplexCustomNetworkMessage(string someString, int someInt, float someFloat, double someDouble, long someLong, Vector3 someVector, Guid someGuid)
    {
        SomeString = someString;
        SomeInt = someInt;
        SomeFloat = someFloat;
        SomeDouble = someDouble;
        SomeLong = someLong;
        SomeVector = someVector;
        SomeGuid = someGuid;
    }
    public ComplexCustomNetworkMessage() { }
    public static ComplexCustomNetworkMessage Default => new()
    {
        SomeDouble = UnityEngine.Random.Range(float.MinValue, float.MaxValue),
        SomeFloat = UnityEngine.Random.Range(float.MinValue, float.MaxValue),
        SomeInt = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
        SomeLong = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
        SomeGuid = Guid.NewGuid(),
        SomeString = "Thisisaverylongstringwithmuchdata",
        SomeVector = new Vector3
        {
            x = UnityEngine.Random.Range(float.MinValue, float.MaxValue),
            y = UnityEngine.Random.Range(float.MinValue, float.MaxValue),
            z = UnityEngine.Random.Range(float.MinValue, float.MaxValue),
        }
    };
}

public class ComplexCustomNetworkMessageTest : NetworkBehaviour
{
    public ChannelType ChannelType;
    public bool Reliable;

    public Dictionary<Guid, Stopwatch> StopwatchList = new();

    public override void NetworkStart()
    {
        var serverMessageHandler = new ActionMessageHandler<ComplexCustomNetworkMessage>(DistributeToChannel, Guid.Parse("AF7D9B00-4F56-4F8D-B8D3-2C0173C452A5"));
        var clientMessageHandler = new ActionMessageHandler<ComplexCustomNetworkMessage>(StopTimer, Guid.Parse("BF7D9B00-4F56-4F8D-B8D4-7C0173C452A5"));

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

        if (IsServer || IsHost)
        {
            NetworkIdentity.Identities.Select(x => x.Value).ToList().ForEach(no => no.OnReceiveMessage.RegisterMessageHandlerIfNotExists(
                new ActionMessageHandler<ComplexCustomNetworkMessage>(
                    Reliable ? no.ForwardTcp : no.ForwardUdp,
                    Guid.Parse("A6F32A4D-7664-40AA-9E3E-DCAD37BF2BEF"))));
        }

        currentWarmupCount = 0;
        currentRunCount = 0;
        StopwatchList = new();
    }

    private void DistributeToChannel(ComplexCustomNetworkMessage msg, uint? clientId)
    {
        ChannelHandler.Instance.DistributeMessage(Identity, msg, ChannelType);
    }

    private void StopTimer(ComplexCustomNetworkMessage msg, uint? senderClientId)
    {
        if (!StopwatchList.TryGetValue(msg.SomeGuid, out var stopwatch))
            return;

        stopwatch.Stop();

        if (currentWarmupCount < warmupCount)
        {
            warmup.Add(stopwatch.Elapsed.TotalMilliseconds);
            Debug.Log($"[{currentWarmupCount}] Warmup: {stopwatch.Elapsed.TotalMilliseconds:0.00000}");
            currentWarmupCount++;
        }
        else if (currentRunCount < runCount)
        {
            actual.Add(stopwatch.Elapsed.TotalMilliseconds);
            Debug.Log($"[{currentRunCount}] Actual: {stopwatch.Elapsed.TotalMilliseconds:0.00000}");
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

    public int warmupCount;
    private int currentWarmupCount;
    public int noiseCancelCount;
    public int runCount;
    private int currentRunCount;

    private bool stop = false;

    private readonly List<double> warmup = new();
    private readonly List<double> actual = new();

    private void CreateCSV()
    {
        string csv = "\"ms\"\n";
        actual.ForEach(element => csv += $"\"{element:0.00000}\"\n");
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
            else
            {
                var message = ComplexCustomNetworkMessage.Default;
                StopwatchList.Add(message.SomeGuid, Stopwatch.StartNew());

                if (Reliable)
                    NetworkHandler.Instance.SendTcpToServer(message);
                else
                    NetworkHandler.Instance.SendUdpToServer(message);

                timer = resetTime;
            }
        }
    }
}
