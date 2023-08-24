using UnityEditor;
using UnityEngine;

namespace Baidu.Meta.ComponentsTool.Editor
{
    public class ABErrorDialog : EditorWindow
    {
        static public void InitWindow(string msg)
        {
            // Only try to pull up an error window if there is actually a way to display it.  Headless build modes don't show it.
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                ABErrorDialog window = GetWindow<ABErrorDialog>();
                window.message = msg;
                window.titleContent = new GUIContent("Error");
                window.Show();
            }
            else Debug.LogWarning(msg);
        }

        private string message;
        void OnGUI()
        {
            GUILayout.Label("Cyclical Dependency Detected", EditorStyles.boldLabel);

            Rect lastRect = GUILayoutUtility.GetLastRect();

            var bigTextArea = new GUIStyle(GUI.skin.textArea);
            bigTextArea.fontSize = 15;
            bigTextArea.richText = true;
            EditorGUI.SelectableLabel(new Rect(lastRect.x, lastRect.y + lastRect.height, lastRect.width, position.height - lastRect.height), message, bigTextArea);
        }
    }
}
