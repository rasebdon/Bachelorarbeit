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
    public class NetworkHandlerDebugGUI : MonoBehaviour
    {
        [SerializeField] private GameObject _prefabToInstantiate;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 0, 400, 800));
            GUILayout.BeginVertical();

            if(GUILayout.Button("Start Server"))
            {
                NetworkHandler.Instance.StartServer();
            }

            if (GUILayout.Button("Start Client"))
            {
                NetworkHandler.Instance.StartClient();
            }

            if (GUILayout.Button("Start Host"))
            {
                NetworkHandler.Instance.StartHost();
            }

            if(GUILayout.Button("Instantiate Prefab"))
            {
                NetworkHandler.Instance.InstantiateNetworkObject(
                    _prefabToInstantiate, Vector3.zero, Quaternion.identity);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
