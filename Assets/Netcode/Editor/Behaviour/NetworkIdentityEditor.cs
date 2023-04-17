#if UNITY_EDITOR

using Netcode.Runtime.Behaviour;
using System;
using UnityEditor;
using UnityEngine;

namespace Netcode.Editor.Behaviour
{
    [CustomEditor(typeof(NetworkIdentity))]
    internal class NetworkHandlerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            NetworkIdentity netId = (NetworkIdentity)target;

            // It is a prefab
            if (netId.gameObject.scene.rootCount == 0)
            {
                netId.Guid = Guid.Empty;
            }
            // It is a scene instance object
            else if (netId.Guid == Guid.Empty)
            {
                netId.Guid = Guid.NewGuid();
            }

            GUI.enabled = false;
            EditorGUILayout.TextField("GUID", netId.Guid.ToString());
            EditorGUILayout.Toggle("IsLocalPlayer", netId.IsLocalPlayer);
            EditorGUILayout.Toggle("IsPlayer", netId.IsPlayer);
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif