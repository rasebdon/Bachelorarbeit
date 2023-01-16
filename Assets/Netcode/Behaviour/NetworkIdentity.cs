using System;
using UnityEditor;
using UnityEngine;

namespace Netcode.Behaviour
{
    [DisallowMultipleComponent]
    public class NetworkIdentity : MonoBehaviour
    {
        public Guid Guid
        {
            get
            {
                if (_guid == Guid.Empty && !string.IsNullOrEmpty(_guidAsString))
                {
                    _guid = new Guid(_guidAsString);
                }
                return _guid;
            }
            set
            {
                _guid = value;
                _guidAsString = _guid.ToString();
            }
        }
        [SerializeField] private Guid _guid;
        [SerializeField] private string _guidAsString;

        /// <summary>
        /// Gets invoked on the server from this gameobject to notify the connected channels
        /// </summary>
        public Action<string> OnServerMessageDistribute;
        public Action<string> OnServerMessageProcess;

        /// <summary>
        /// Gets invoked on the client when this network identity receives a message
        /// </summary>
        public Action<string> OnClientMessageReceive;

        private void Start()
        {
            InvokeRepeating(nameof(SendMessageTest), 0, 5);
            
            // In reality, this will contain event methods that will call the network manager
            OnServerMessageProcess += (string s) =>
            {
                OnClientMessageReceive.Invoke(s);
            };

            // This will finally be called on the client to receive messages that were sent in the OnServerMessageProcess event
            OnClientMessageReceive += (string s) =>
            {
                Debug.Log($"NetworkIdentity {Guid} notified!");
            };
        }

        private void SendMessageTest()
        {
            if(OnServerMessageDistribute != null)
            {
                // This simulates the NetworkHandler.FindNetworkObject().OnServerMessageDistribute.Invoke() call
                OnServerMessageDistribute.Invoke("Test");
            }
        }
    }
}
