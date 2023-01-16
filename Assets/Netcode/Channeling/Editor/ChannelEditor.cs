using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using UnityEditor;
using UnityEngine;

namespace Netcode.Channeling.Editor
{
    [CustomEditor(typeof(Channel))]
    [CanEditMultipleObjects]
    internal class ChannelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Update the serialize object
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_subscribed"), true);

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_channelId"));
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
