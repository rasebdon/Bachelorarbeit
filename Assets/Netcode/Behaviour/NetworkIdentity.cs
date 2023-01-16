using System;
using UnityEngine;

namespace Netcode.Behaviour
{
    public class NetworkIdentity : MonoBehaviour
    {
        public Guid Guid { get => _guid; set => _guid = value; }
        [SerializeField] private Guid _guid;

        /// <summary>
        /// Gets invoked from this gameobject to notify the connected channels
        /// </summary>
        public Action<string> OnMessageSend;

        /// <summary>
        /// Gets invoked from the channels that this gameobject is subscribed to
        /// </summary>
        public Action<string> OnMessageReceive;

        private void Start()
        {
            InvokeRepeating(nameof(SendMessageTest), 0, 5);
            OnMessageReceive += (string s) =>
            {
                Debug.Log($"NetworkIdentity {_guid} notified!");
            };
        }

        private void SendMessageTest()
        {
            if(OnMessageSend != null)
            {
                OnMessageSend.Invoke("Test");
            }
        }
    }
}
