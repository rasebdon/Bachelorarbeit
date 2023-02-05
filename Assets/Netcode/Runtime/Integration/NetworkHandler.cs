using Netcode.Behaviour;
using Netcode.Channeling;
using Netcode.Runtime.Communication.Client;
using Netcode.Runtime.Communication.Common;
using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Serialization;
using Netcode.Runtime.Communication.Server;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netcode.Runtime.Integration
{
    [RequireComponent(typeof(ChannelHandler))]
    public class NetworkHandler : MonoBehaviour
    {
        public static NetworkHandler Instance { get; private set; }

        private NetworkServer _server;
        private NetworkClient _client;

        // Configuration
        [SerializeField] private string _hostname = "127.0.0.1";
        [SerializeField] private ushort _tcpPort = 27600;
        [SerializeField] private ushort _udpPort = 27600;
        [SerializeField] private ushort _maxClients = 10;
        [SerializeField] private LogLevel _logLevel = LogLevel.Error;

        // Controls
        public bool IsServer { get => _isServer; private set => _isServer = value; }
        [SerializeField] private bool _isServer;
        public bool IsClient { get => _isClient; private set => _isClient = value; }
        [SerializeField] private bool _isClient;
        public bool IsHost { get => _isHost; private set => _isHost = value; }
        [SerializeField] private bool _isHost;

        [SerializeField] private bool _started;

        // Instantiation
        [SerializeField] private List<GameObject> _objectRegistry;
        [SerializeField] private GameObject _playerPrefab;

        // Events
        public Action<NetworkIdentity> OnPlayerSpawn;

        private void Awake()
        {
            // Setup Singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Cannot have multiple instances of NetworkHandler", this);
                return;
            }
            Instance = this;

            // Add player prefab to registry
            _objectRegistry ??= new();
            if(_playerPrefab) _objectRegistry.Add(_playerPrefab);

            DontDestroyOnLoad(this);

            // Setup protocol
            IMessageSerializer messageSerializer = new MessagePackMessageSerializer();
            IMessageProtocolHandler protocolHandler = new MessageProtocolHandler(messageSerializer); ;
            ILoggerFactory loggerFactory = new UnityLoggerFactory(_logLevel);

            // Setup server
            _server = new(_tcpPort, _udpPort, _maxClients, protocolHandler, loggerFactory);

            // Setup client
            _client = new(protocolHandler, loggerFactory.CreateLogger<NetworkClient>());

            // Setup channel handler
            _server.OnServerClientConnect += (obj, args) =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    // Instantiate player object
                    NetworkIdentity playerObject = InstantiateNetworkObject(_playerPrefab, Vector3.zero, Quaternion.identity);
                    playerObject.IsPlayer = true;
                    playerObject.ClientId = args.Client.ClientId;

                    OnPlayerSpawn?.Invoke(playerObject);

                    Debug.Log($"Spawned player object for client {args.Client.ClientId}!");
                });
            };

            _client.OnReceive += InstantiateNetworkObjectClientCallback;
        }

        private void InstantiateNetworkObjectClientCallback(object sender, NetworkMessageRecieveArgs e)
        {
            if(e.Message is InstantiateNetworkObjectMessage msg)
            {
                NetworkIdentity networkIdentity = null;

                if (IsClient)
                {
                    // Find prefab with id in object registry
                    GameObject prefab = _objectRegistry.Find(obj => obj.GetInstanceID() == msg.PrefabId);

                    // Instantiate object
                    GameObject networkObject = Instantiate(prefab, msg.Position, msg.Rotation);

                    // Set the NetworkIdentity
                    networkIdentity = networkObject.GetComponent<NetworkIdentity>();
                    networkIdentity.Guid = msg.NetworkIdentityGuid;
                    networkIdentity.PrefabId = prefab.GetInstanceID();
                }
                else if (IsHost)
                {
                    // Find existing object
                    networkIdentity = NetworkIdentity.FindByGuid(msg.NetworkIdentityGuid);
                }

                // Get object with prefab id
                if(msg.ClientId.HasValue)
                {
                    // Is local player
                    if(_client.ClientId == msg.ClientId.Value)
                    {
                        networkIdentity.IsLocalPlayer = true;
                    }

                    networkIdentity.ClientId = msg.ClientId.Value;
                    networkIdentity.IsPlayer = true;
                    networkIdentity.PrefabId = msg.PrefabId;
                }
            }
        }

        public void StartServer()
        {
            if (_started)
            {
                Debug.Log("Cannot start server if network manager has already started a client or server!");
                return;
            }

            IsServer = true;
            _started = true;

            _server.Start();
        }

        public void StartClient()
        {
            if (_started)
            {
                Debug.Log("Cannot start server if network manager has already started a client or server!");
                return;
            }

            IsClient = true;
            _started = true;

            _client.Connect(_hostname, _tcpPort, _udpPort);
        }

        public void StartHost()
        {
            if (_started)
            {
                Debug.Log("Cannot start server if network manager has already started a client or server!");
                return;
            }

            IsHost = true;
            _started = true;

            _server.Start();
            _client.Connect("127.0.0.1", _tcpPort, _udpPort);
        }

        public NetworkIdentity InstantiateNetworkObject(GameObject obj, Vector3 position, Quaternion rotation)
        {
            // Check if we started
            if (!_started)
            {
                Debug.LogError("Cannot instantiate network objects when the server is not started!", this);
                return null;
            }

            // Check that we are on the server
            if (IsClient)
            {
                Debug.LogError("Cannot instantiate network objects on the client!", this);
                return null;
            }

            // Check that the object has a NetworkIdentity component attached
            if (!obj.GetComponent<NetworkIdentity>())
            {
                Debug.LogError("Cannot instantiate network objects without a NetworkIdentity component!", this);
                return null;
            }

            // Check that the object is in the registry
            if (!_objectRegistry.Contains(obj))
            {
                Debug.LogError("Cannot instantiate objects that are not registered in the object registry!", this);
                return null;
            }

            // Instantiate the object
            GameObject networkObject = Instantiate(obj, position, rotation);

            // Set the NetworkIdentity
            NetworkIdentity networkIdentity = networkObject.GetComponent<NetworkIdentity>();
            networkIdentity.Guid = Guid.NewGuid();
            networkIdentity.PrefabId = obj.GetInstanceID();

            return networkIdentity;
        }

        private void OnApplicationQuit()
        {
            _server.Dispose();
            _client.Dispose();
        }

        public void Send<T>(T message, uint clientId) where T : NetworkMessage
        {
            if (IsClient)
            {
                Task.Run(async () =>
                {
                    await _client.SendTcpAsync(message);
                });
            }
            else if (IsServer || IsHost)
            {
                Task.Run(async () =>
                {
                    await _server.Clients.Find(c => c.ClientId == clientId).SendTcpAsync(message);
                });
            }
        }
    }
}
