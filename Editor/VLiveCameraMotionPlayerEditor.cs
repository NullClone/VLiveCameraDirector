using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraMotionPlayer用のカスタムインスペクター。
    /// 参照コンポーネント、現在時刻、進行速度、方向、再生ライフサイクル状態の監視と手動操作を提供します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraMotionPlayer))]
    public class VLiveCameraMotionPlayerEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _shotProp;
        private SerializedProperty _cinemachineCameraProp;
        private SerializedProperty _splineDollyProp;
        private SerializedProperty _rotationComposerProp;
        private SerializedProperty _aimProxyProp;


        // Methods

        private void OnEnable()
        {
            _shotProp = serializedObject.FindProperty("_shot");
            _cinemachineCameraProp = serializedObject.FindProperty("_cinemachineCamera");
            _splineDollyProp = serializedObject.FindProperty("_splineDolly");
            _rotationComposerProp = serializedObject.FindProperty("_rotationComposer");
            _aimProxyProp = serializedObject.FindProperty("_aimProxy");
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var player = (VLiveCameraMotionPlayer)target;

            DrawHeader(player);
            EditorGUILayout.Space(6);

            DrawReferencesSection();
            EditorGUILayout.Space(6);

            DrawPlaybackStateSection(player);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(VLiveCameraMotionPlayer player)
        {
            var rect = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.09f, 0.13f));

            var titleRect = new Rect(rect.x + 12, rect.y + 6, rect.width - 24, 20);
            var subRect = new Rect(rect.x + 12, rect.y + 26, rect.width - 24, 16);

            EditorGUI.LabelField(titleRect, "VLIVE CAMERA MOTION PLAYER", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            });

            string shotName = player.Shot != null ? player.Shot.ShotName : "Unattached";
            string status = player.IsLive ? "● LIVE" : "OFF-AIR";
            Color statusColor = player.IsLive ? new Color(1f, 0.35f, 0.35f) : new Color(0.6f, 0.7f, 0.8f);

            EditorGUI.LabelField(subRect, $"{shotName}  |  {status}", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = statusColor }
            });
        }

        private void DrawReferencesSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Component References (コンポーネント参照)", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_shotProp);
                EditorGUILayout.PropertyField(_cinemachineCameraProp);
                EditorGUILayout.PropertyField(_splineDollyProp);
                EditorGUILayout.PropertyField(_rotationComposerProp);
                EditorGUILayout.PropertyField(_aimProxyProp);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPlaybackStateSection(VLiveCameraMotionPlayer player)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Runtime Playback Status (再生状態)", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawBadge("LIVE", player.IsLive, new Color(0.9f, 0.2f, 0.2f));
                DrawBadge("PLAYING", player.IsPlaying, new Color(0.2f, 0.75f, 0.35f));
                DrawBadge("HOLD", player.IsHolding, new Color(0.95f, 0.65f, 0.15f));
                DrawBadge("REVERSING", player.IsReversing, new Color(0.85f, 0.4f, 0.85f));
                DrawBadge("PREPARED", player.IsPrepared, new Color(0.25f, 0.55f, 0.95f));
            }

            EditorGUILayout.Space(6);

            float duration = (player.Shot != null && player.Shot.AppliedMotion != null) ? player.Shot.AppliedMotion.EffectiveDuration : 0f;
            float progress = duration > 0.001f ? Mathf.Clamp01(player.CurrentTime / duration) : 0f;

            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(false, 20),
                progress,
                $"Time: {player.CurrentTime:F2}s / {duration:F2}s  |  Distance: {player.CurrentSplineDistance:F2}m"
            );

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField($"Speed Multiplier: {player.CurrentSpeedMultiplier:F2}x (Target: {player.TargetSpeedMultiplier:F2}x)  |  Direction: {(player.CurrentDirection > 0 ? "Forward (+1)" : "Reverse (-1)")}");

            if (Application.isPlaying && player.IsLive)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Manual Operation (手動操作テスト):", EditorStyles.miniBoldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Speed -"))
                    {
                        player.SpeedDown();
                    }

                    if (GUILayout.Button("Speed +"))
                    {
                        player.SpeedUp();
                    }

                    if (GUILayout.Button("Reverse"))
                    {
                        player.Reverse();
                    }

                    if (player.IsHolding)
                    {
                        if (GUILayout.Button("Resume"))
                        {
                            player.Resume();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Hold"))
                        {
                            player.Hold();
                        }
                    }

                    if (GUILayout.Button("Freeze"))
                    {
                        player.Freeze();
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBadge(string label, bool active, Color activeColor)
        {
            Color color = active ? activeColor : new Color(0.25f, 0.25f, 0.25f);
            var style = new GUIStyle(EditorStyles.miniButton)
            {
                normal = { textColor = active ? Color.white : new Color(0.55f, 0.55f, 0.55f) },
                fontStyle = FontStyle.Bold
            };

            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Label(label, style, GUILayout.Height(18));
            GUI.backgroundColor = prevBg;
        }
    }
}
