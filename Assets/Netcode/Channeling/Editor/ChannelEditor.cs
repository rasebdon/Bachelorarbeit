using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Netcode.Channeling.Editor
{
    [CustomEditor(typeof(Channel))]
    [CanEditMultipleObjects]
    internal class ChannelEditor : UnityEditor.Editor
    {
        private static ushort _nextChannelId;
        private ushort _channelId = _nextChannelId++;

        public override void OnInspectorGUI()
        {
            // Update the serialize object
            serializedObject.Update();

            serializedObject.FindProperty("_channelId").intValue = _channelId;
            EditorGUILayout.IntField("ChannelId", serializedObject.FindProperty("_channelId").intValue);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_subscribed"), true);

            if (serializedObject.ApplyModifiedProperties())
            {
                
            }
        }
    }
}
