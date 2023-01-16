using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Netcode.Behaviour.Editor
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
