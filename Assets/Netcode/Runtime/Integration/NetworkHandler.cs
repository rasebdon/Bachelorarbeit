using Netcode.Behaviour;
using Netcode.Runtime.Communication.Client;
using Netcode.Runtime.Communication.Common;
using Netcode.Runtime.Communication.Common.Serialization;
using Netcode.Runtime.Communication.Server;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Netcode.Runtime.Integration
{
    public class NetworkHandler : MonoBehaviour
    {
        public static NetworkHandler Instance { get; private set; }

        private NetworkServer _server;
        private NetworkClient _client;

        // Configuration
        [SerializeField] private string _hostname = "localhost";
        [SerializeField] private ushort _tcpPort = 27600;
        [SerializeField] private ushort _udpPort = 27600;
        [SerializeField] private ushort _maxClients = 10;

        // Controls
        public bool IsServer { get => _isServer; private set => _isServer = value; }
        [SerializeField] private bool _isServer;
        public bool IsClient { get => _isClient; private set => _isClient = value; }
        [SerializeField] private bool _isClient;
        public bool IsHost { get => _isHost; private set => _isHost = value; }
        [SerializeField] private bool _isHost;

        [SerializeField] private bool _started;

        // Instantiation
        [SerializeField] private Dictionary<Guid, GameObject> _networkObjects;
        [SerializeField] private List<GameObject> _objectRegistry;
        [SerializeField] private GameObject _playerPrefab;

        private void Awake()
        {
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

            // Setup server
            _server = new(_tcpPort, _udpPort, _maxClients, protocolHandler);

            // Setup client
            _client = new(protocolHandler);
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
            _client.Connect("localhost", _tcpPort, _udpPort);
        }

        public void InstantiateNetworkObject(GameObject obj, Vector3 position, Quaternion rotation)
        {
            // Check if we started
            if (!_started)
            {
                Debug.LogError("Cannot instantiate network objects when the server is not started!", this);
                return;
            }

            // Check that we are on the server
            if (IsClient)
            {
                Debug.LogError("Cannot instantiate network objects on the client!", this);
                return;
            }

            // Check that the object has a NetworkIdentity component attached
            if (!obj.GetComponent<NetworkIdentity>())
            {
                Debug.LogError("Cannot instantiate network objects without a NetworkIdentity component!", this);
                return;
            }

            // Check that the object is in the registry
            if (!_objectRegistry.Contains(obj))
            {
                Debug.LogError("Cannot instantiate objects that are not registered in the object registry!", this);
                return;
            }

            // Instantiate the object
            GameObject networkObject = Instantiate(obj, position, rotation);

            // Set the NetworkIdentity
            NetworkIdentity networkIdentity = networkObject.GetComponent<NetworkIdentity>();
            networkIdentity.Guid = Guid.NewGuid();

            // Invoke OnNetworkInstantiate
            networkIdentity.OnServerMessageDistribute?.Invoke("Instantiation");
        }
    }
}
