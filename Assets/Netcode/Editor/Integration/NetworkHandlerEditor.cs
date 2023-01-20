using Netcode.Runtime.Integration;
using System;
using UnityEditor;
using UnityEngine;

namespace Netcode.Editor.Integration
{
    [CustomEditor(typeof(NetworkHandler))]
    internal class NetworkHandlerEditor : UnityEditor.Editor
    {
        NetworkHandler handler;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            handler = (NetworkHandler)target;

            ShowConfiguration();

            ShowControls();

            ShowInstantiation();

            serializedObject.ApplyModifiedProperties();
        }

        private void ShowInstantiation()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Instantiation", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_playerPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_objectRegistry"));

            EditorGUILayout.EndVertical();
        }

        private void ShowControls()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            GUI.enabled = false;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_started"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_isServer"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_isClient"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_isHost"));
            
            GUI.enabled = true;
            
            NetcodeGUI.DrawHorizontalGUILine();
            EditorGUILayout.EndVertical();
        }

        private void ShowConfiguration()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_hostname"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_tcpPort"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_udpPort"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_maxClients"));

            NetcodeGUI.DrawHorizontalGUILine();
            EditorGUILayout.EndVertical();
        }
    }
}
