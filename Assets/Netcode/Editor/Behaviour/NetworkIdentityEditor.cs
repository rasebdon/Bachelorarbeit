using Netcode.Behaviour;
using System;
using UnityEditor;
using UnityEngine;

namespace Netcode.Editor.Behaviour
{
    [CustomEditor(typeof(NetworkIdentity))]
    internal class NetworkIdentityEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            NetworkIdentity netId = (NetworkIdentity)target;

            if (netId.Guid == Guid.Empty)
            {
                netId.Guid = Guid.NewGuid();
            }

            GUI.enabled = false;
            EditorGUILayout.TextField("GUID", netId.Guid.ToString());
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
