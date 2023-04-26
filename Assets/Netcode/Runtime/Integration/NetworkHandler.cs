using Netcode.Channeling;
using Netcode.Runtime.Behaviour;
using Netcode.Runtime.Communication.Client;
using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
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
        [SerializeField] private ushort _port = 27600;
        [SerializeField] private ushort _maxClients = 10;
        [SerializeField] private LogLevel _logLevel = LogLevel.Error;
        [SerializeField] private int _menuSceneBuildIndex;
        [SerializeField] private int _serverTickRate;
        [SerializeField] private int _clientTickRate;
        public int ServerTickRate => _serverTickRate;
        public int ClientTickRate => _clientTickRate;

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
        public NetworkIdentity LocalPlayer { get; set; }
        
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
            ILoggerFactory loggerFactory = new UnityLoggerFactory(_logLevel);

            // Setup server
            _server = new(_port, _maxClients, loggerFactory);

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
                    }
                });
            };

            _server.MessageHandlerRegistry.RegisterHandler(
                new ActionMessageHandler<SyncNetworkVariableMessage>(
                    NetworkVariableSyncServer, 
                    Guid.Parse("55DB337C-D5C6-4159-9778-80DF89104DBE")));

            _client.MessageHandlerRegistry.RegisterHandler(
                new ActionMessageHandler<SyncNetworkVariableMessage>(
                    NetworkVariableSyncClient, 
                    Guid.Parse("41898D5A-5700-47AE-8E27-A050FEBF135A")));
            _client.MessageHandlerRegistry.RegisterHandler(
                new ActionMessageHandler<InstantiateNetworkObjectMessage>(
                    InstantiateNetworkObjectClientCallback,
                    Guid.Parse("2BBF7D2E-5CE3-4D3B-B34E-9953B16DA4FF")));
            _client.MessageHandlerRegistry.RegisterHandler(
                new ActionMessageHandler<DestroyNetworkObjectMessage>(
                    DestroyNetworkObjectClientCallback,
                    Guid.Parse("118A50C9-A86D-4563-9CD8-6E32C561A6DF")));

            _client.OnDisconnect += ReloadSceneOnClientDisconnect;

            _client.OnConnect += (id) => { ClientId = id; };
            _client.OnDisconnect += (id) => { ClientId = null; };
        }

        private void NetworkVariableSyncClient(SyncNetworkVariableMessage msg, uint? senderClientId)
        {
            var netId = NetworkIdentity.FindByGuid(msg.NetworkIdentity);

            if (netId != null) 
                netId.SetNetworkVariableFromServerOnClient(msg);
        }

        private void NetworkVariableSyncServer(SyncNetworkVariableMessage msg, uint? senderClientId)
        {
            var nedId = NetworkIdentity.FindByGuid(msg.NetworkIdentity);

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                ChannelHandler.Instance.DistributeMessage(nedId, msg, msg.ChannelType);
            });
        }

        private void DestroyNetworkObjectClientCallback(DestroyNetworkObjectMessage msg, uint? senderClientId)
        {
            NetworkIdentity netId = NetworkIdentity.FindByGuid(msg.Identity);

            if (netId != null)
            {
                Destroy(netId.gameObject);
            }
        }

        private void ReloadSceneOnClientDisconnect(uint obj)
        {
            Destroy(gameObject); // Delete "Session"
            SceneManager.LoadScene(_menuSceneBuildIndex);
        }

        private void InstantiateNetworkObjectClientCallback(InstantiateNetworkObjectMessage msg, uint? senderClientId)
        {
            NetworkIdentity networkIdentity = null;

            if (IsClient)
            {
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
                    LocalPlayer = networkIdentity;
                }
                networkIdentity.OwnerClientId = msg.OwnerClientId.Value;
                networkIdentity.IsPlayer = msg.IsPlayer;
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

            InvokeRepeating(nameof(ClientTick), 0, 1f / _serverTickRate);
            await _client.Connect(_hostname, _port);
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
            InvokeRepeating(nameof(ClientTick), 0, 1f / _serverTickRate);
            await _client.Connect("127.0.0.1", _port);
        }

        void ServerTick() => _server.OnTick();
        void ClientTick() => _client.OnTick();

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

        public void SendTcpToClient<T>(T message, uint clientId) where T : NetworkMessage
        {
            if (IsClient)
            {
                Debug.LogError("Cannot send to client on the client!");
                return;
            }
            
            if (IsServer || IsHost)
            {
                _server.Clients.Find(c => c.ClientId == clientId)?.SendTcp(message);
            }
        }
        
        public void SendTcpToServer<T>(T message) where T : NetworkMessage
        {
            if (IsServer)
            {
                Debug.LogError("Cannot send to server on the server!");
                return;
            }

            if (IsClient || IsHost)
            {
                _client.SendTcp(message);
            }
        }

        public void SendUdpToClient<T>(T message, uint clientId) where T : NetworkMessage
        {
            if (IsClient)
            {
                Debug.LogError("Cannot send to client on the client!");
                return;
            }

            if (IsServer || IsHost)
            {
                _server.Clients.Find(c => c.ClientId == clientId)?.SendUdp(message);
            }
        }

        public void SendUdpToServer<T>(T message) where T : NetworkMessage
        {
            if (IsServer)
            {
                Debug.LogError("Cannot send to server on the server!");
                return;
            }

            if (IsClient || IsHost)
            {
                _client.SendUdp(message);
            }
        }

        public MessageHandlerRegistry ServerMessageHandlerRegistry => _server.MessageHandlerRegistry;
        public MessageHandlerRegistry ClientMessageHandlerRegistry => _client.MessageHandlerRegistry;
    }
}
