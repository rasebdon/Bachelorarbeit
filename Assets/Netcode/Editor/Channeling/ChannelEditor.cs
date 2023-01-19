using Netcode.Channeling;
using UnityEditor;
using UnityEngine;

namespace Netcode.Editor.Channeling
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
