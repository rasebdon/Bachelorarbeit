using System.Collections;
using UnityEngine;

namespace Netcode.Runtime.Integration
{
    [RequireComponent(typeof(NetworkHandler))]
    public class NetworkHandlerDebugGUI : MonoBehaviour
    {
        [SerializeField] private GameObject _prefabToInstantiate;

        private NetworkHandler _handler;
        private Vector2 _scrollView;

        private void Awake()
        {
            _handler = GetComponent<NetworkHandler>();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 0, 400, Screen.height));
            GUILayout.BeginVertical();

            if (_handler.IsStarted)
            {
                // Server
                if (_handler.IsHost || _handler.IsServer)
                {
                    ShowServerHostButtons();
                }
                // Client
                else
                {
                    ShowClientButtons();
                }
            }
            else
            {
                ShowNotStartedButtons();
            }

            ShowConsole();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        Queue myLogQueue = new();
        uint queueSize = 300;
        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            myLogQueue.Enqueue(logString);
            if (type == LogType.Exception)
                myLogQueue.Enqueue(stackTrace);
            while (myLogQueue.Count > queueSize)
                myLogQueue.Dequeue();
        }

        GUIStyle scrollViewStyle;
        int oldHeight = 0;

        public void ShowConsole()
        {
            scrollViewStyle ??= GUI.skin.scrollView;

            if (oldHeight != Screen.height)
            {
                scrollViewStyle.normal.background = MakeTex(400, Screen.height, new Color(0, 0, 0, 0.5f));
                oldHeight = Screen.height;
            }

            _scrollView = GUILayout.BeginScrollView(_scrollView, scrollViewStyle);

            GUILayout.Label("\n" + string.Join("\n", myLogQueue.ToArray()));

            GUILayout.EndScrollView();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color32[] pix = new Color32[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new(width, height);
            result.SetPixels32(pix);
            result.Apply();
            return result;
        }

        public void ShowServerHostButtons()
        {
            if (GUILayout.Button("Instantiate Prefab"))
            {
                NetworkHandler.Instance.InstantiateNetworkObject(
                    _prefabToInstantiate, Vector3.zero, Quaternion.identity);
            }
        }

        public void ShowClientButtons()
        {

        }

        public void ShowNotStartedButtons()
        {
            if (GUILayout.Button("Start Server"))
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
        }
    }
}
