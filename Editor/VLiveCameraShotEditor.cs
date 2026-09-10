using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace toshi.VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraShot用のカスタムインスペクター。
    /// 構図設定、参照検証、および再生中の移動状態の可視化と手動操作を提供します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraShot))]
    [CanEditMultipleObjects]
    public class VLiveCameraShotEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _shotNameProp;
        private SerializedProperty _presetProp;
        private SerializedProperty _cinemachineCameraProp;
        private SerializedProperty _shotTypeProp;
        private SerializedProperty _splineDollyProp;
        private SerializedProperty _initialSpeedProp;
        private SerializedProperty _minSpeedProp;
        private SerializedProperty _maxSpeedProp;
        private SerializedProperty _speedStepProp;
        private SerializedProperty _initialDirectionProp;
        private SerializedProperty _startPositionProp;
        private SerializedProperty _endPositionProp;
        private SerializedProperty _decelerationDistanceProp;


        // Methods

        private void OnEnable()
        {
            _shotNameProp = serializedObject.FindProperty("_shotName");
            _presetProp = serializedObject.FindProperty("_preset");
            _cinemachineCameraProp = serializedObject.FindProperty("_cinemachineCamera");
            _shotTypeProp = serializedObject.FindProperty("_shotType");
            _splineDollyProp = serializedObject.FindProperty("_splineDolly");
            _initialSpeedProp = serializedObject.FindProperty("_initialSpeed");
            _minSpeedProp = serializedObject.FindProperty("_minSpeed");
            _maxSpeedProp = serializedObject.FindProperty("_maxSpeed");
            _speedStepProp = serializedObject.FindProperty("_speedStep");
            _initialDirectionProp = serializedObject.FindProperty("_initialDirection");
            _startPositionProp = serializedObject.FindProperty("_startPosition");
            _endPositionProp = serializedObject.FindProperty("_endPosition");
            _decelerationDistanceProp = serializedObject.FindProperty("_decelerationDistance");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var shot = (VLiveCameraShot)target;

            DrawHeader(shot);
            EditorGUILayout.Space(6);

            DrawIdentificationSection();
            EditorGUILayout.Space(6);

            DrawCameraSection();
            EditorGUILayout.Space(6);

            if (_shotTypeProp.enumValueIndex == (int)VLiveCameraShot.ShotType.Spline)
            {
                DrawMotionSection();
                EditorGUILayout.Space(6);
            }

            DrawRuntimeSection(shot);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(VLiveCameraShot shot)
        {
            var rect = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.09f, 0.13f));

            var titleRect = new Rect(rect.x + 12, rect.y + 6, rect.width - 24, 20);
            var subRect = new Rect(rect.x + 12, rect.y + 26, rect.width - 24, 16);

            EditorGUI.LabelField(titleRect, "VLIVE CAMERA SHOT", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            });

            string statusText = shot.IsLive ? "● LIVE PROGRAM" : "OFF-AIR";
            Color statusColor = shot.IsLive ? new Color(1f, 0.35f, 0.35f) : new Color(0.6f, 0.7f, 0.8f);

            EditorGUI.LabelField(subRect, $"{shot.ShotName}  |  {statusText}", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = statusColor }
            });
        }

        private void DrawIdentificationSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Shot Identification (ショット識別)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_shotNameProp);
            EditorGUILayout.PropertyField(_presetProp);

            EditorGUILayout.EndVertical();
        }

        private void DrawCameraSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Camera Configuration (カメラ設定)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_shotTypeProp);
            EditorGUILayout.PropertyField(_cinemachineCameraProp);

            if (_cinemachineCameraProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("CinemachineCamera が未割り当てです。ショットとして機能しません。", MessageType.Warning);
            }

            if (_shotTypeProp.enumValueIndex == (int)VLiveCameraShot.ShotType.Spline)
            {
                EditorGUILayout.PropertyField(_splineDollyProp);
                if (_splineDollyProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Spline Shot には CinemachineSplineDolly が必要です。", MessageType.Warning);
                }
                else
                {
                    var dolly = (CinemachineSplineDolly)_splineDollyProp.objectReferenceValue;
                    if (dolly != null && dolly.Spline == null)
                    {
                        EditorGUILayout.HelpBox("CinemachineSplineDolly に SplineContainer が設定されていません。", MessageType.Warning);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMotionSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Spline Motion Settings (スプライン移動設定)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_initialSpeedProp);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_minSpeedProp, new GUIContent("Min Speed"));
                EditorGUILayout.PropertyField(_maxSpeedProp, new GUIContent("Max Speed"));
            }

            EditorGUILayout.PropertyField(_speedStepProp);
            EditorGUILayout.PropertyField(_initialDirectionProp);

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(_startPositionProp);
            EditorGUILayout.PropertyField(_endPositionProp);
            EditorGUILayout.PropertyField(_decelerationDistanceProp);

            EditorGUILayout.EndVertical();
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        private void DrawRuntimeSection(VLiveCameraShot shot)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Runtime Status (再生状態)", EditorStyles.boldLabel);

            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("複数選択中はRuntime操作および状態表示が無効です。", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawStatusBadge("LIVE", shot.IsLive, new Color(0.9f, 0.2f, 0.2f));
                DrawStatusBadge("PLAYING", shot.IsPlaying, new Color(0.2f, 0.75f, 0.35f));
                DrawStatusBadge("HOLD", shot.IsHolding, new Color(0.95f, 0.65f, 0.15f));
                DrawStatusBadge("PREPARED", shot.IsPrepared, new Color(0.25f, 0.55f, 0.95f));
            }

            EditorGUILayout.Space(4);

            if (shot.Type == VLiveCameraShot.ShotType.Spline)
            {
                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(false, 18),
                    shot.CurrentPosition,
                    $"Position: {shot.CurrentPosition:F2} (Speed: {shot.CurrentSpeed:F2}, Dir: {shot.CurrentDirection})"
                );

                if (Application.isPlaying && shot.IsLive)
                {
                    EditorGUILayout.Space(4);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Speed -"))
                        {
                            shot.SpeedDown();
                        }

                        if (GUILayout.Button("Speed +"))
                        {
                            shot.SpeedUp();
                        }

                        if (GUILayout.Button("Reverse"))
                        {
                            shot.Reverse();
                        }

                        if (shot.IsHolding)
                        {
                            if (GUILayout.Button("Resume"))
                            {
                                shot.Resume();
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Hold"))
                            {
                                shot.Hold();
                            }
                        }
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusBadge(string label, bool active, Color activeColor)
        {
            Color color = active ? activeColor : new Color(0.3f, 0.3f, 0.3f);
            var style = new GUIStyle(EditorStyles.miniButton)
            {
                normal = { textColor = active ? Color.white : new Color(0.6f, 0.6f, 0.6f) },
                fontStyle = FontStyle.Bold
            };

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Label(label, style, GUILayout.Height(18));
            GUI.backgroundColor = prevColor;
        }
    }
}
