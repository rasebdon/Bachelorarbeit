using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Netcode.Channeling.Editor
{
    [CustomEditor(typeof(ChannelArea))]
    internal class ChannelAreaEditor : UnityEditor.Editor
    {
        private SerializedProperty _drawGizmos;

        public override void OnInspectorGUI()
        {
            // Update the serialize object
            serializedObject.Update();

            ChannelArea channelArea = (ChannelArea)target;

            if (GUILayout.Button("Reload Channels"))
            {
                channelArea.Reset();
            }

            _drawGizmos = serializedObject.FindProperty("_drawGizmos");
            _drawGizmos.boolValue = EditorGUILayout.Toggle("Toggle Gizmos ", _drawGizmos.boolValue);

            _drawGizmos = serializedObject.FindProperty("_gizmoColor");
            _drawGizmos.colorValue = EditorGUILayout.ColorField("Gizmo Colors", _drawGizmos.colorValue);

            // Render list of ChannelSettings
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_channelSettings"), true);

            if (serializedObject.ApplyModifiedProperties())
            {
                channelArea.Reset();
            }
        }
    }
}
