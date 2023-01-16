using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netcode.Channeling
{
    [Serializable]
    public class ChannelSettings
    {
        public ChannelType channelType;
        public Vector3 size;

        public ChannelSettings(ChannelType channelType)
        {
            this.channelType = channelType;
            size = Vector3.one * 10;
        }
    }

    public class ChannelArea : MonoBehaviour
    {
        private Dictionary<ChannelType, Channel> _channels = new();

        [SerializeField] private List<ChannelSettings> _channelSettings;
        [SerializeField] private bool _drawGizmos;
        [SerializeField] private Color _gizmoColor = Color.white;

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if(_drawGizmos)
            {
                foreach (ChannelSettings settings in _channelSettings)
                {
                    Gizmos.color = _gizmoColor;
                    Gizmos.DrawWireCube(transform.position, settings.size);
                    Gizmos.color = Color.white;
                }
            }
        }

#endif

        public void Reset()
        {
            ReloadChannels(Enum.GetValues(typeof(ChannelType)).Cast<ChannelType>());
        }

        private void Awake()
        {
            var actualChannels = GetComponentsInChildren<Channel>();
            foreach (var channel in actualChannels)
            {
                _channels.Add(Enum.Parse<ChannelType>(channel.name.Replace("Channel", "")), channel);
            }

            var expectedChannels = Enum.GetValues(typeof(ChannelType)).Cast<ChannelType>();

            foreach (var channel in expectedChannels)
            {
                if (!_channels.ContainsKey(channel))
                {
                    ReloadChannels(expectedChannels);
                }
            }
        }

        private void ReloadChannels(IEnumerable<ChannelType> channelTypes)
        {
            Channel[] channels = transform.GetComponentsInChildren<Channel>();
            channels.ToList().ForEach(c => DestroyImmediate(c.gameObject));
            _channels = new();
            _channelSettings ??= new();

            // Add all channels
            foreach (ChannelType channelType in channelTypes)
            {
                // Get channel settings
                IEnumerable<ChannelSettings> settings = _channelSettings.Where(
                    s => s.channelType == channelType);

                ChannelSettings setting = settings.FirstOrDefault();
                if (setting == null)
                {
                    setting = new ChannelSettings(channelType);
                    _channelSettings.Add(setting);
                }
                // Get rid of too many settings
                else if (settings.Count() > 1)
                {
                    _channelSettings.RemoveAll(s => s.channelType == channelType);
                    _channelSettings.Add(setting);
                }

                // Create and add the channel
                AddChannel(CreateChannel(channelType, setting));
            }
        }

        private void AddChannel(KeyValuePair<ChannelType, Channel> channel)
        {
            _channels.Add(channel.Key, channel.Value);
        }

        /// <summary>
        /// Dynamically creates a <see cref="Channel"/> GameObject, sets this transform as parent
        /// and configures the trigger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="channelDiameter"></param>
        /// <returns></returns>
        private KeyValuePair<ChannelType, Channel> CreateChannel(ChannelType type, ChannelSettings settings)
        {
            // Create GameObject
            GameObject channelObject = new($"{type}Channel");

            // Set transform
            channelObject.transform.SetParent(transform);
            channelObject.transform.position = transform.position;
            
            // Configure box collider component
            BoxCollider collider = channelObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = settings.size;

            // Configure channel component
            Channel channel = channelObject.AddComponent<Channel>();

            return new(type, channel);
        }
    }
}
