using Netcode.Runtime.Integration;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Netcode.Editor.Integration
{
    [CustomEditor(typeof(NetworkHandler))]
    internal class NetworkHandlerInspector : UnityEditor.Editor
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

            SceneAsset oldScene = null;
            var menuSceneBuildIndex = serializedObject.FindProperty("_menuSceneBuildIndex");
            bool error = false;

            if (menuSceneBuildIndex.intValue != -1)
            {
                try
                {
                    oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneManager.GetSceneByBuildIndex(menuSceneBuildIndex.intValue).path);
                }
                catch
                {
                    error = true;
                }
            }

            var newScene = EditorGUILayout.ObjectField("Menu Scene", oldScene, typeof(SceneAsset), false) as SceneAsset;

            if (!error)
            {
                int newSceneBuildIndex = SceneManager.GetSceneByPath(AssetDatabase.GetAssetPath(newScene)).buildIndex;

                if (EditorGUI.EndChangeCheck())
                {
                    menuSceneBuildIndex.intValue = newSceneBuildIndex;
                }
            }

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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_serverTickRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_clientTickRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_logLevel"));

            NetcodeGUI.DrawHorizontalGUILine();
            EditorGUILayout.EndVertical();
        }
    }
}
