using Netcode.Runtime.Behaviour;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class NetworkVariableSpeedTest : NetworkBehaviour
{
    public NetworkVariable<int> netVar = new(10);

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

            Debug.Log($"Noise : {noise.Average():0.00000000000000000000000}");

            netVar.OnValueChange += OnChange;
        }
    }

    private readonly float resetTime = 1f;
    private float timer = 0;
    Stopwatch stopwatch;

    private bool printed = false;

    public int warmupCount;
    private int currentWarmupCount;
    public int noiseCancelCount;
    public int runCount;
    private int currentRunCount;

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
                if(currentWarmupCount >= warmupCount && currentRunCount >= runCount)
                {
                    if (!printed)
                    {
                        Debug.Log($"Conclusion");
                        Debug.Log($"Noise : {noise.Average():0.00000000000000000000000}");
                        Debug.Log($"Warmup: {warmup.Average():0.00000000000000000000000}");
                        Debug.Log($"Actual: {actual.Average():0.00000000000000000000000}");
                        Debug.Log($"Cutting away lowest and highest values");
                        Debug.Log($"Warmup: {warmup.Where(x => x != warmup.Min() && x != warmup.Max()).Average()}");
                        Debug.Log($"Actual: {actual.Where(x => x != actual.Min() && x != actual.Max()).Average()}");
                        Debug.Log($"Removing noise");
                        Debug.Log($"Warmup: {warmup.Where(x => x != warmup.Min() && x != warmup.Max()).Average() - noise.Average()}");
                        Debug.Log($"Actual: {actual.Where(x => x != warmup.Min() && x != warmup.Max()).Average() - noise.Average()}");
                        printed = true;
                    }

                    return;
                }
                timer = resetTime;
                stopwatch = Stopwatch.StartNew();
                netVar.SetValue(netVar.GetValue() + 1);
            }
        }
    }

    private readonly List<double> warmup = new();
    private readonly List<double> actual = new();
    private readonly List<double> noise = new();

    private void OnChange(int arg1, int arg2)
    {
        if (Identity.IsLocalPlayer)
        {
            stopwatch.Stop();

            if (currentWarmupCount < warmupCount)
            {
                warmup.Add(stopwatch.Elapsed.TotalMilliseconds);
                Debug.Log($"[{currentWarmupCount++}] Warmup: {stopwatch.Elapsed.TotalMilliseconds:0.00000000000000000000000}");
            }
            else if (currentRunCount < runCount)
            {
                actual.Add(stopwatch.Elapsed.TotalMilliseconds);
                Debug.Log($"[{currentRunCount++}] Actual: {stopwatch.Elapsed.TotalMilliseconds:0.00000000000000000000000}");
            }
        }
    }
}
