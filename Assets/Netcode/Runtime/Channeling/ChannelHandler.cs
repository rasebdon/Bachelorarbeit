using Netcode.Runtime.Behaviour;
using Netcode.Runtime.Channeling;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using UnityEngine;
using UnityEngine.UI;

namespace Netcode.Channeling
{
    [RequireComponent(typeof(NetworkHandler))]
    public class ChannelHandler : MonoBehaviour
    {
        public static ChannelHandler Instance { get; private set; }

        private readonly Dictionary<NetworkIdentity, ZoneData> _zoneRegistry = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Cannot have multiple instances of NetworkHandler", this);
                return;
            }
            Instance = this;
        }

        public void DistributeMessage<T>(NetworkIdentity identity, T message, ChannelType channelType = ChannelType.Environment) where T : NetworkMessage
        {
            if(_zoneRegistry.TryGetValue(identity, out var zoneData) && zoneData.CurrentZone != null)
            {
                switch (channelType)
                {
                    case ChannelType.Environment:
                        zoneData.CurrentZone.Publish(message, channelType, identity.OwnerClientId, true);
                        break;
                    case ChannelType.Interaction:
                        foreach (var zone in zoneData.Zones)
                            zone.Publish(message, channelType, identity.OwnerClientId, false);
                        break;
                }
            }
        }

        public void EnterZone(NetworkIdentity identity, Zone zone)
        {
            if(!_zoneRegistry.ContainsKey(identity))
                _zoneRegistry.Add(identity, new());

            var zoneData = _zoneRegistry[identity];

            zoneData.Zones.Add(zone);

            if (zoneData.CurrentZone == null)
            {
                zone.Subscribe(identity, ChannelType.Environment);
                zoneData.CurrentZone = zone;
            }
            
            zone.Subscribe(identity, ChannelType.Interaction);
        }

        public void ExitZone(NetworkIdentity identity, Zone zone)
        {
            zone.Unsubscribe(identity, ChannelType.Environment);
            zone.Unsubscribe(identity, ChannelType.Interaction);

            if (_zoneRegistry.TryGetValue(identity, out var zoneData) && 
                zoneData.CurrentZone == zone)
            {
                zoneData.Zones.Remove(zone);
                zoneData.CurrentZone = zoneData.Zones.FirstOrDefault();
                if(zoneData.CurrentZone != null)
                    zoneData.CurrentZone.Subscribe(identity, ChannelType.Environment);
            }
        }

        public void ExitFromAllZones(NetworkIdentity identity)
        {
            if(_zoneRegistry.TryGetValue(identity, out var zoneData))
            {
                foreach (var zone in zoneData.Zones)
                {
                    zone.Unsubscribe(identity, ChannelType.Environment);
                    zone.Unsubscribe(identity, ChannelType.Interaction);
                }
                
                _zoneRegistry.Remove(identity);
            }
        }

        public bool HasChannels(NetworkIdentity networkIdentity)
        {
            return _zoneRegistry.ContainsKey(networkIdentity) && _zoneRegistry[networkIdentity].Zones.Any();
        }

        private class ZoneData
        {
            public HashSet<Zone> Zones { get; } = new ();
            public Zone CurrentZone { get; set; } = null;
        }
    }
}
