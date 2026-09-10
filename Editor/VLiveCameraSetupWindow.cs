using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraUnitの初期Rig作成を行う簡略化されたセットアップウィンドウ。
    /// 初期作成後の詳細な編集はVLiveCameraRigのInspectorで行います。
    /// </summary>
    public class VLiveCameraSetupWindow : EditorWindow
    {
        // Methods

        /// <summary>
        /// Setup Windowを開きます。
        /// </summary>
        [MenuItem("Tools/VLive Camera/VLive Camera Setup", priority = 10)]
        public static void OpenWindow()
        {
            var window = GetWindow<VLiveCameraSetupWindow>("VLive Camera Setup");
            window.minSize = new Vector2(360f, 260f);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(8);

            DrawContent();
        }

        private void DrawHeader()
        {
            var rect = GUILayoutUtility.GetRect(0, 52, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.09f, 0.13f));

            var titleRect = new Rect(rect.x + 12, rect.y + 6, rect.width - 24, 22);
            var subRect = new Rect(rect.x + 12, rect.y + 28, rect.width - 24, 16);

            EditorGUI.LabelField(titleRect, "VLIVE CAMERA SETUP", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            });

            EditorGUI.LabelField(subRect, "Camera Rig 初期作成", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.65f, 0.85f, 1f) }
            });
        }

        private void DrawContent()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Camera Rig Setup (初期導入)", EditorStyles.boldLabel);

            // 既存Rigの存在確認
            VLiveCameraRig existingRig = FindFirstObjectByType<VLiveCameraRig>(FindObjectsInactive.Include);

            if (existingRig != null)
            {
                EditorGUILayout.HelpBox(
                    $"Scene内に既存の '{existingRig.name}' を検出しました。\n" +
                    "Target、正面基準、スケール、Shot構成は VLiveCameraRig Inspector から編集してください。",
                    MessageType.Info
                );

                EditorGUILayout.Space(6);

                if (GUILayout.Button("Select Camera Rig", GUILayout.Height(32)))
                {
                    Selection.activeGameObject = existingRig.gameObject;
                    EditorGUIUtility.PingObject(existingRig.gameObject);
                }

                EditorGUILayout.Space(4);

                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                if (GUILayout.Button("Create New Camera Rig", GUILayout.Height(26)))
                {
                    VLiveCameraRigBuilder.CreateRig();
                }

                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "一操作で新しい Camera Rig（Program Camera, Brain, Switcher, Input, 6つの標準Presetスロット）を作成します。\n\n" +
                    "作成後は Rig Inspector で Target を割り当てて 'Apply / Sync' を実行してください。",
                    MessageType.None
                );

                EditorGUILayout.Space(8);

                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                if (GUILayout.Button("Create Camera Rig", GUILayout.Height(36)))
                {
                    VLiveCameraRigBuilder.CreateRig();
                }

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
