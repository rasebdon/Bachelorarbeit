using Netcode.Behaviour;
using Netcode.Channeling;
using Netcode.Runtime.Communication.Client;
using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Pipeline;
using Netcode.Runtime.Communication.Common.Serialization;
using Netcode.Runtime.Communication.Server;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        [SerializeField] private int _menuSceneBuildIndex;
        [SerializeField] private int _serverTickRate;
        [SerializeField] private int _clientTickRate;

        // Controls
        public bool IsServer { get => _isServer; private set => _isServer = value; }
        [SerializeField] private bool _isServer;
        public bool IsClient { get => _isClient; private set => _isClient = value; }
        [SerializeField] private bool _isClient;
        public bool IsHost { get => _isHost; private set => _isHost = value; }
        [SerializeField] private bool _isHost;

        public uint? ClientId { get => clientId; private set => clientId = value; }
        [SerializeField] private uint? clientId;

        public bool IsStarted { get => _started; private set => _started = value; }
        [SerializeField] private bool _started;

        // Instantiation
        [SerializeField] private List<GameObject> _objectRegistry;
        [SerializeField] private GameObject _playerPrefab;
        private readonly Dictionary<uint, NetworkIdentity> _playerObjects = new();

        // Events
        public Action<NetworkIdentity> OnPlayerSpawn;

        // Utils
        public IDataSerializer Serializer { get; private set; }

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
            if (_playerPrefab && !_objectRegistry.Contains(_playerPrefab))
            {
                _objectRegistry.Add(_playerPrefab);
            }
            _playerObjects.Clear();

            DontDestroyOnLoad(this);

            // Setup pipeline
            Serializer = new MessagePackDataSerializer();
            IPipeline pipeline = PipelineFactory.CreatePipeline(); 
            ILoggerFactory loggerFactory = new UnityLoggerFactory(_logLevel);

            // Setup server
            _server = new(_tcpPort, _udpPort, _maxClients, loggerFactory);

            // Setup client
            _client = new(loggerFactory.CreateLogger<NetworkClient>());

            // Setup channel handler
            _server.OnServerClientConnect += (obj, args) =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    // Instantiate player object
                    NetworkIdentity playerObject = InstantiateNetworkObject(_playerPrefab, Vector3.zero, Quaternion.identity, $"Player_{args.Client.ClientId}");
                    playerObject.IsPlayer = true;
                    playerObject.OwnerClientId = args.Client.ClientId;
                    _playerObjects.Add(playerObject.OwnerClientId, playerObject);

                    OnPlayerSpawn?.Invoke(playerObject);
                });
            };
            _server.OnServerClientDisconnect += (obj, args) =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (_playerObjects.TryGetValue(args.Client.ClientId, out var playerObject))
                    {
                        Destroy(playerObject.gameObject);

                        // TODO: Send destroy object to other clients
                    }
                });
            };

            _server.OnServerMessageReceive += NetworkVariableSyncServer;

            _client.OnReceive += NetworkVariableSyncClient;
            _client.OnReceive += InstantiateNetworkObjectClientCallback;
            _client.OnReceive += DestroyNetworkObjectClientCallback;
            _client.OnDisconnect += ReloadSceneOnClientDisconnect;

            _client.OnConnect += (id) => { ClientId = id; };
            _client.OnDisconnect += (id) => { ClientId = null; };
        }

        private void NetworkVariableSyncClient(object sender, NetworkMessageRecieveArgs e)
        {
            if (e.Message is SyncNetworkVariableMessage msg)
            {
                NetworkIdentity.FindByGuid(msg.NetworkIdentity).SetNetworkVariableFromServerOnClient(msg);
            }
        }

        private void NetworkVariableSyncServer(object sender, ServerMessageReceiveEventArgs e)
        {
            if(e.Message is SyncNetworkVariableMessage msg)
            {
                var nedId = NetworkIdentity.FindByGuid(msg.NetworkIdentity);

                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ChannelHandler.Instance.DistributeMessage(nedId, msg, ChannelType.Environment);
                });
            }
        }

        private void DestroyNetworkObjectClientCallback(object sender, NetworkMessageRecieveArgs e)
        {
            if (e.Message is DestroyNetworkObjectMessage msg)
            {
                NetworkIdentity netId = NetworkIdentity.FindByGuid(msg.Identity);

                if (netId != null)
                {
                    Destroy(netId.gameObject);
                }
            }
        }

        private void ReloadSceneOnClientDisconnect(uint obj)
        {
            Destroy(gameObject); // Delete "Session"
            SceneManager.LoadScene(_menuSceneBuildIndex);
        }

        private void InstantiateNetworkObjectClientCallback(object sender, NetworkMessageRecieveArgs e)
        {
            if (e.Message is InstantiateNetworkObjectMessage msg)
            {
                NetworkIdentity networkIdentity = null;

                if (IsClient)
                {
                    // TODO: Find out why instantiation is multiple times
                    // Try find existing object
                    if (NetworkIdentity.FindByGuid(msg.NetworkIdentityGuid) != null)
                    {
                        return;
                    }

                    // Find prefab with id in object registry
                    GameObject prefab = _objectRegistry[msg.PrefabId];

                    // Instantiate object
                    GameObject networkObject = Instantiate(prefab, msg.Position, msg.Rotation);

                    // Set the NetworkIdentity
                    networkIdentity = networkObject.GetComponent<NetworkIdentity>();
                    networkIdentity.Guid = msg.NetworkIdentityGuid;
                }
                else if (IsHost)
                {
                    // Find existing object
                    networkIdentity = NetworkIdentity.FindByGuid(msg.NetworkIdentityGuid);
                }

                networkIdentity.PrefabId = msg.PrefabId;
                networkIdentity.name = msg.ObjectName;

                // Get object with prefab id
                if (msg.OwnerClientId.HasValue)
                {
                    // Is local player
                    if (_client.ClientId == msg.OwnerClientId.Value && msg.IsPlayer)
                    {
                        networkIdentity.IsLocalPlayer = true;
                    }
                    networkIdentity.OwnerClientId = msg.OwnerClientId.Value;
                    networkIdentity.IsPlayer = msg.IsPlayer;
                }
            }
        }

        public void StartServer()
        {
            if (IsStarted)
            {
                Debug.Log("Cannot start server if network manager has already started a client or server!");
                return;
            }

            IsServer = true;
            IsStarted = true;

            _server.Start();
            InvokeRepeating(nameof(ServerTick), 0, 1f / _serverTickRate);
        }

        public async void StartClient()
        {
            if (IsStarted)
            {
                Debug.Log("Cannot start server if network manager has already started a client or server!");
                return;
            }

            IsClient = true;
            IsStarted = true;

            _client.OnConnect += (uint id) => InvokeRepeating(nameof(ClientTick), 0, 1f / _clientTickRate);
            await _client.Connect(_hostname, _tcpPort, _udpPort);
        }

        public async void StartHost()
        {
            if (IsStarted)
            {
                Debug.Log("Cannot start server if network manager has already started a client or server!");
                return;
            }

            IsHost = true;
            IsStarted = true;

            _server.Start();
            InvokeRepeating(nameof(ServerTick), 0, 1f / _serverTickRate);
            _client.OnConnect += (uint id) => InvokeRepeating(nameof(ClientTick), 0, 1f / _clientTickRate);
            await _client.Connect("127.0.0.1", _tcpPort, _udpPort);
        }

        private void ServerTick()
        {
            _server.OnTick();
        }

        private void ClientTick()
        {
            _client.OnTick();
        }

        public NetworkIdentity InstantiateNetworkObject(GameObject obj, Vector3 position, Quaternion rotation)
        {
            return InstantiateNetworkObject(obj, position, rotation, obj.name);
        }
        public NetworkIdentity InstantiateNetworkObject(GameObject obj, Vector3 position, Quaternion rotation, string objectName)
        {
            // Check if we started
            if (!IsStarted)
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
            networkIdentity.name = objectName;
            networkIdentity.Guid = Guid.NewGuid();
            networkIdentity.PrefabId = _objectRegistry.IndexOf(obj);

            return networkIdentity;
        }

        private void OnApplicationQuit()
        {
            _server.Dispose();
            _client.Dispose();
        }

        public void SendTcp<T>(T message, uint clientId) where T : NetworkMessage
        {
            if (IsClient)
            {
                _client.SendTcp(message);
            }
            else if (IsServer || IsHost)
            {
                _server.Clients.Find(c => c.ClientId == clientId).SendTcp(message);
            }
        }

        public void SendUdp<T>(T message, uint clientId) where T : NetworkMessage
        {
            if (IsClient)
            {
                _client.SendUdp(message);
            }
            else if (IsServer || IsHost)
            {
                _server.Clients.Find(c => c.ClientId == clientId).SendUdp(message);
            }
        }
    }
}
