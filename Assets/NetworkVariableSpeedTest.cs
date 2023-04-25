using Netcode.Channeling;
using Netcode.Runtime.Behaviour;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class NetworkVariableSpeedTest : NetworkBehaviour
{
    public NetworkVariable<int> netVar = new(10, ChannelType.Environment, true);

    public override void NetworkStart()
    {
        if (Identity.IsLocalPlayer)
        {
            currentWarmupCount = 0;
            currentRunCount = 0;

            for (int i = 0; i < noiseCancelCount + warmupCount; i++)
            {
                stopwatch = Stopwatch.StartNew();
                stopwatch.Stop();

                if (i < warmupCount)
                    continue;

                noise.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            netVar.OnValueChange += OnChange;

            SendUpdate(netVar.GetValue() + 1);
        }
    }

    [SerializeField] private float resetTime = 1f;
    Stopwatch stopwatch;

    public int warmupCount;
    private int currentWarmupCount;
    public int noiseCancelCount;
    public int runCount;
    private int currentRunCount;

    private readonly List<double> warmup = new();
    private readonly List<double> actual = new();
    private readonly List<double> noise = new();

    private void SendUpdate(int newVal)
    {
        stopwatch = Stopwatch.StartNew();
        netVar.SetValue(newVal);
    }

    private void CreateCSV()
    {
        string csv = "ms, measuringType\n";
        warmup.ForEach(element => csv += $"{element:0.00000}, {nameof(warmup)}\n");
        actual.ForEach(element => csv += $"{element:0.00000}, {nameof(actual)}\n");
        noise.ForEach(element => csv += $"{element:0.00000}, {nameof(noise)}\n");
        System.IO.File.WriteAllText($"C:\\Users\\rdohn\\OneDrive\\Bachelorarbeit\\Netcode_Testresults\\myNetcode_{netVar.ChannelType}_{(netVar.IsReliable ? "tcp" : "udp")}.csv", csv);
    }

    private void OnChange(int arg1, int arg2)
    {
        if (Identity.IsLocalPlayer)
        {
            stopwatch.Stop();

            if (currentWarmupCount < warmupCount)
            {
                warmup.Add(stopwatch.Elapsed.TotalMilliseconds);
                Debug.Log($"[{currentWarmupCount}] Warmup: {stopwatch.Elapsed.TotalMilliseconds:0.00000} - Value {arg2}");
                currentWarmupCount++;
            }
            else if (currentRunCount < runCount)
            {
                actual.Add(stopwatch.Elapsed.TotalMilliseconds);
                Debug.Log($"[{currentRunCount}] Actual: {stopwatch.Elapsed.TotalMilliseconds:0.00000} - Value {arg2}");
                currentRunCount++;
            }

            if (currentWarmupCount >= warmupCount && currentRunCount >= runCount)
            {
                Debug.Log($"Noise : {noise.Average()}");
                Debug.Log($"Warmup: {warmup.Average()}");
                Debug.Log($"Actual: {actual.Average()}");
                Debug.Log($"Cutting away lowest and highest values");
                Debug.Log($"Warmup: {warmup.Where(x => x != warmup.Min() && x != warmup.Max()).Average()}");
                Debug.Log($"Actual: {actual.Where(x => x != actual.Min() && x != actual.Max()).Average()}");
                Debug.Log($"Removing noise");
                Debug.Log($"Warmup: {warmup.Where(x => x != warmup.Min() && x != warmup.Max()).Average() - noise.Average()}");
                Debug.Log($"Actual: {actual.Where(x => x != warmup.Min() && x != warmup.Max()).Average() - noise.Average()}");
                CreateCSV();
                return;
            }

            SendUpdate(arg2 + 1);
        }
    }
}
