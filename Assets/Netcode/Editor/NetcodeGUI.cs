using UnityEditor;
using UnityEngine;

namespace Netcode.Editor
{
    public static class NetcodeGUI
    {
        public static void DrawHorizontalGUILine()
        {
            GUILayout.Space(4);

            Rect rect = GUILayoutUtility.GetRect(10, 1, GUILayout.ExpandWidth(true));
            rect.height = 1;
            rect.xMin = 0;
            rect.xMax = EditorGUIUtility.currentViewWidth;

            Color lineColor = new(0.10196f, 0.10196f, 0.10196f, 1);
            EditorGUI.DrawRect(rect, lineColor);
            GUILayout.Space(4);
        }

    }

}