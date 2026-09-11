using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraShot用のカスタムインスペクター。
    /// 所有コンポーネント、適用済みMotion設定、再生状態の可視化と手動操作、Preset保存、診断機能を提供します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraShot))]
    [CanEditMultipleObjects]
    public class VLiveCameraShotEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _shotNameProp;
        private SerializedProperty _shotTypeProp;
        private SerializedProperty _cinemachineCameraProp;
        private SerializedProperty _splineDollyProp;
        private SerializedProperty _rotationComposerProp;
        private SerializedProperty _aimProxyProp;
        private SerializedProperty _motionPlayerProp;
        private SerializedProperty _performerTargetProp;
        private SerializedProperty _appliedPresetProp;
        private SerializedProperty _appliedTargetHeightProp;

        private VLiveCameraMotionValidator.ValidationReport _report;


        // Methods

        private void OnEnable()
        {
            _shotNameProp = serializedObject.FindProperty("_shotName");
            _shotTypeProp = serializedObject.FindProperty("_shotType");
            _cinemachineCameraProp = serializedObject.FindProperty("_cinemachineCamera");
            _splineDollyProp = serializedObject.FindProperty("_splineDolly");
            _rotationComposerProp = serializedObject.FindProperty("_rotationComposer");
            _aimProxyProp = serializedObject.FindProperty("_aimProxy");
            _motionPlayerProp = serializedObject.FindProperty("_motionPlayer");
            _performerTargetProp = serializedObject.FindProperty("_performerTarget");
            _appliedPresetProp = serializedObject.FindProperty("_appliedPreset");
            _appliedTargetHeightProp = serializedObject.FindProperty("_appliedTargetHeight");
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var shot = (VLiveCameraShot)target;

            DrawHeader(shot);
            EditorGUILayout.Space(6);

            DrawIdentificationSection();
            EditorGUILayout.Space(6);

            DrawComponentsSection();
            EditorGUILayout.Space(6);

            DrawAppliedMotionSummary(shot);
            EditorGUILayout.Space(6);

            DrawOperationsSection(shot);
            EditorGUILayout.Space(6);

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

            EditorGUI.LabelField(subRect, $"{shot.ShotName}  |  {shot.Type}  |  {statusText}", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = statusColor }
            });
        }

        private void DrawIdentificationSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Shot Identification (ショット識別)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_shotNameProp);
            EditorGUILayout.PropertyField(_shotTypeProp);

            EditorGUILayout.EndVertical();
        }

        private void DrawComponentsSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Owned Components & References (所有コンポーネント)", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_cinemachineCameraProp);
                EditorGUILayout.PropertyField(_splineDollyProp);
                EditorGUILayout.PropertyField(_rotationComposerProp);
                EditorGUILayout.PropertyField(_aimProxyProp);
                EditorGUILayout.PropertyField(_motionPlayerProp);
                EditorGUILayout.PropertyField(_performerTargetProp);
            }

            if (_cinemachineCameraProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("CinemachineCamera が未割り当てです。Rig Inspector の Apply / Sync で修復してください。", MessageType.Warning);
            }

            if (_shotTypeProp.enumValueIndex == (int)VLiveCameraShot.ShotType.Spline && _splineDollyProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Spline Dolly が未割り当てです。Rig Inspector の Apply / Sync で修復してください。", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAppliedMotionSummary(VLiveCameraShot shot)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Applied Motion Configuration (適用済み設定)", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_appliedPresetProp, new GUIContent("Source Preset"));
                EditorGUILayout.PropertyField(_appliedTargetHeightProp, new GUIContent("Applied Target Height"));
            }

            VLiveCameraAppliedMotion applied = shot.AppliedMotion;
            if (applied != null)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField($"Duration: {applied.EffectiveDuration:F2}s  |  Timing Mode: {applied.ScaleMode}", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Entry: {applied.EntryMode} (In: {applied.InTime:F2}s, Out: {applied.OutTime:F2}s)  |  Exit: {applied.ExitBehavior}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Aim Offset: {applied.AimOffset}  |  Screen Pos: ({applied.ScreenPosition.x:F2}, {applied.ScreenPosition.y:F2})", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"FOV: {applied.FieldOfView:F1}° ({applied.LensMode})  |  Roll: {applied.RollMode}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOperationsSection(VLiveCameraShot shot)
        {
            if (targets.Length > 1)
            {
                return;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Shot Operations (操作)", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save As New Preset (新規Preset保存)", GUILayout.Height(24)))
                {
                    var rig = shot.GetComponentInParent<VLiveCameraRig>();
                    VLiveCameraPresetBaker.BakePresetFromShot(rig, shot);
                }

                if (GUILayout.Button("Run Diagnostics (診断実行)", GUILayout.Height(24)))
                {
                    _report = VLiveCameraMotionValidator.ValidateShot(shot);
                }
            }

            if (_report != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginVertical("helpBox");
                EditorGUILayout.LabelField("Diagnostics Report:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Duration: {_report.Duration:F2}s  |  Spline: {_report.SplineLength:F2}m", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Peak Speed: {_report.MaxSpeed:F2} m/s  |  Peak Acc: {_report.MaxAcceleration:F2} m/s²  |  Peak Jerk: {_report.MaxJerk:F2} m/s³", EditorStyles.miniLabel);

                foreach (var msg in _report.Messages)
                {
                    MessageType msgType = msg.Severity switch
                    {
                        VLiveCameraMotionValidator.DiagnosticSeverity.Error => MessageType.Error,
                        VLiveCameraMotionValidator.DiagnosticSeverity.Warning => MessageType.Warning,
                        _ => MessageType.Info
                    };

                    EditorGUILayout.HelpBox($"[{msg.Category}] {msg.Message}", msgType);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
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

            float duration = shot.AppliedMotion != null ? shot.AppliedMotion.EffectiveDuration : 0f;
            float progress = duration > 0.001f ? Mathf.Clamp01(shot.CurrentTime / duration) : 0f;

            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(false, 18),
                progress,
                $"Time: {shot.CurrentTime:F2}s / {duration:F2}s (Speed: {shot.CurrentSpeedMultiplier:F2}x, Dist: {shot.CurrentSplineDistance:F2}m)"
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

                    if (GUILayout.Button("Freeze"))
                    {
                        shot.Freeze();
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
