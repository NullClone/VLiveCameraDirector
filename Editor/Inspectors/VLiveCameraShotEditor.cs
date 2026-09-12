using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraShot用のカスタムインスペクター。
    /// 所有コンポーネント、適用済みMotion設定、再構築・Preset保存・診断・削除操作、実行時再生状態の表示を提供します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraShot))]
    [CanEditMultipleObjects]
    public class VLiveCameraShotEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _shotNameProp;
        private SerializedProperty _shotTypeProp;
        private SerializedProperty _rigProp;
        private SerializedProperty _cinemachineCameraProp;
        private SerializedProperty _splineDollyProp;
        private SerializedProperty _rotationComposerProp;
        private SerializedProperty _aimProxyProp;
        private SerializedProperty _motionPlayerProp;
        private SerializedProperty _performerTargetProp;
        private SerializedProperty _appliedPresetProp;
        private SerializedProperty _appliedTargetHeightProp;
        private SerializedProperty _appliedDistanceScaleProp;
        private SerializedProperty _appliedMotionScaleProp;
        private SerializedProperty _appliedVerticalMotionScaleProp;

        private VLiveCameraMotionValidationReport _report;


        // Methods

        private void OnEnable()
        {
            _shotNameProp = serializedObject.FindProperty("_shotName");
            _shotTypeProp = serializedObject.FindProperty("_shotType");
            _rigProp = serializedObject.FindProperty("_rig");
            _cinemachineCameraProp = serializedObject.FindProperty("_cinemachineCamera");
            _splineDollyProp = serializedObject.FindProperty("_splineDolly");
            _rotationComposerProp = serializedObject.FindProperty("_rotationComposer");
            _aimProxyProp = serializedObject.FindProperty("_aimProxy");
            _motionPlayerProp = serializedObject.FindProperty("_motionPlayer");
            _performerTargetProp = serializedObject.FindProperty("_performerTarget");
            _appliedPresetProp = serializedObject.FindProperty("_appliedPreset");
            _appliedTargetHeightProp = serializedObject.FindProperty("_appliedTargetHeight");
            _appliedDistanceScaleProp = serializedObject.FindProperty("_appliedDistanceScale");
            _appliedMotionScaleProp = serializedObject.FindProperty("_appliedMotionScale");
            _appliedVerticalMotionScaleProp = serializedObject.FindProperty("_appliedVerticalMotionScale");
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var shot = (VLiveCameraShot)target;

            DrawIdentificationSection(shot);
            EditorGUILayout.Space(6);

            DrawComponentsSection();
            EditorGUILayout.Space(6);

            DrawAppliedMotionSection(shot);
            EditorGUILayout.Space(6);

            DrawOperationsSection(shot);
            EditorGUILayout.Space(6);

            DrawRuntimeSection(shot);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawIdentificationSection(VLiveCameraShot shot)
        {
            EditorGUILayout.LabelField("Shot Configuration", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_shotNameProp);
            EditorGUILayout.PropertyField(_shotTypeProp);

            EditorGUILayout.LabelField("Status", shot.IsLive ? "Live (Program)" : "Off-Air");
        }

        private void DrawComponentsSection()
        {
            EditorGUILayout.LabelField("Component References", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_rigProp);
                EditorGUILayout.PropertyField(_cinemachineCameraProp);
                if (_shotTypeProp.enumValueIndex == (int)VLiveCameraShotType.Spline)
                {
                    EditorGUILayout.PropertyField(_splineDollyProp);
                }

                EditorGUILayout.PropertyField(_rotationComposerProp);
                EditorGUILayout.PropertyField(_aimProxyProp);
                EditorGUILayout.PropertyField(_motionPlayerProp);
                EditorGUILayout.PropertyField(_performerTargetProp);
            }

            if (_cinemachineCameraProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("CinemachineCamera is missing. Run Apply / Sync on the Rig to resolve.", MessageType.Warning);
            }

            if (_shotTypeProp.enumValueIndex == (int)VLiveCameraShotType.Spline && _splineDollyProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Spline Dolly is missing. Run Apply / Sync on the Rig to resolve.", MessageType.Warning);
            }
        }

        private void DrawAppliedMotionSection(VLiveCameraShot shot)
        {
            EditorGUILayout.LabelField("Applied Motion", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_appliedPresetProp, new GUIContent("Source Preset"));
                EditorGUILayout.PropertyField(_appliedTargetHeightProp, new GUIContent("Applied Target Height"));
                EditorGUILayout.PropertyField(_appliedDistanceScaleProp, new GUIContent("Applied Distance Scale"));
                EditorGUILayout.PropertyField(_appliedMotionScaleProp, new GUIContent("Applied Horizontal Scale"));
                EditorGUILayout.PropertyField(_appliedVerticalMotionScaleProp, new GUIContent("Applied Vertical Scale"));
            }

            VLiveCameraAppliedMotion applied = shot.AppliedMotion;
            if (applied != null)
            {
                EditorGUILayout.LabelField($"Duration: {applied.EffectiveDuration:F2}s | Timing: {applied.ScaleMode}");
                EditorGUILayout.LabelField($"Entry: {applied.EntryMode} (In: {applied.InTime:F2}s, Out: {applied.OutTime:F2}s) | Exit: {applied.ExitBehavior}");
                EditorGUILayout.LabelField($"Aim Offset: {applied.AimOffset} | Screen Pos: ({applied.ScreenPosition.x:F2}, {applied.ScreenPosition.y:F2})");
                EditorGUILayout.LabelField($"FOV: {applied.FieldOfView:F1}° ({applied.LensMode}) | Roll: {applied.RollMode}");
            }

            // Check if slot preset differs from applied preset
            var rig = shot.GetComponentInParent<VLiveCameraRig>();
            VLiveCameraMotionPreset slotPreset = null;
            if (rig != null && rig.Slots != null)
            {
                for (int i = 0; i < rig.Slots.Count; i++)
                {
                    if (rig.Slots[i] != null && rig.Slots[i].Shot == shot)
                    {
                        slotPreset = rig.Slots[i].Preset;
                        break;
                    }
                }
            }

            if (slotPreset != null && shot.AppliedPreset != null && slotPreset != shot.AppliedPreset)
            {
                EditorGUILayout.HelpBox($"Slot Preset ('{slotPreset.DisplayName}') differs from Applied Preset ('{shot.AppliedPreset.DisplayName}'). Click 'Rebuild From Preset' below to update.", MessageType.Warning);
            }
        }

        private void DrawOperationsSection(VLiveCameraShot shot)
        {
            if (targets.Length > 1)
            {
                return;
            }

            EditorGUILayout.LabelField("Shot Actions", EditorStyles.boldLabel);

            var rig = shot.GetComponentInParent<VLiveCameraRig>();

            using (new EditorGUILayout.HorizontalScope())
            {
                bool canRebuild = !Application.isPlaying && shot.PerformerTarget != null && shot.AppliedPreset != null;
                using (new EditorGUI.DisabledScope(!canRebuild))
                {
                    if (GUILayout.Button("Rebuild From Preset", GUILayout.Height(24)))
                    {
                        string presetName = shot.AppliedPreset != null ? shot.AppliedPreset.DisplayName : "Preset";
                        if (EditorUtility.DisplayDialog(
                                "Rebuild Shot From Preset",
                                $"Rebuild camera position, lens, aim, spline, and motion configuration for '{shot.ShotName}' from '{presetName}'?\nManual adjustments will be overwritten.",
                                "Rebuild",
                                "Cancel"))
                        {
                            serializedObject.ApplyModifiedProperties();
                            VLiveCameraRigBuilder.RebuildShotFromPreset(shot);
                            serializedObject.Update();
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                if (GUILayout.Button("Save As New Preset", GUILayout.Height(24)))
                {
                    VLiveCameraPresetBaker.BakePresetFromShot(rig, shot);
                }
            }

            EditorGUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Motion", GUILayout.Height(22)))
                {
                    _report = VLiveCameraMotionValidator.ValidateShot(shot);
                }

                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    if (GUILayout.Button("Delete Shot", GUILayout.Height(22)))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Delete Shot GameObject",
                                $"Delete Shot GameObject '{shot.name}', Spline, and Aim Proxy from the Scene?\nThis action can be undone.",
                                "Delete",
                                "Cancel"))
                        {
                            serializedObject.ApplyModifiedProperties();
                            VLiveCameraRigBuilder.DeleteShotGameObject(shot);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }

            if (_report != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(
                    $"Diagnostics: Duration {_report.Duration:F2}s, Spline {_report.SplineLength:F2}m, Speed {_report.MaxSpeed:F2} m/s, Acc {_report.MaxAcceleration:F2} m/s², Jerk {_report.MaxJerk:F2} m/s³",
                    EditorStyles.miniLabel
                );

                if (_report.Messages.Count == 0)
                {
                    EditorGUILayout.HelpBox("No issues found.", MessageType.Info);
                }
                else
                {
                    foreach (var msg in _report.Messages)
                    {
                        MessageType msgType = msg.Severity switch
                        {
                            VLiveCameraDiagnosticSeverity.Error => MessageType.Error,
                            VLiveCameraDiagnosticSeverity.Warning => MessageType.Warning,
                            _ => MessageType.Info
                        };

                        EditorGUILayout.HelpBox($"[{msg.Category}] {msg.Message}", msgType);
                    }
                }
            }
        }

        private void DrawRuntimeSection(VLiveCameraShot shot)
        {
            EditorGUILayout.LabelField("Runtime Playback", EditorStyles.boldLabel);

            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Runtime controls are unavailable when multiple shots are selected.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("State", $"Live: {shot.IsLive} | Playing: {shot.IsPlaying} | Hold: {shot.IsHolding} | Prepared: {shot.IsPrepared}");

            float duration = shot.AppliedMotion != null ? shot.AppliedMotion.EffectiveDuration : 0f;
            float progress = duration > 0.001f ? Mathf.Clamp01(shot.CurrentTime / duration) : 0f;

            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(false, 18),
                progress,
                $"Time: {shot.CurrentTime:F2}s / {duration:F2}s (Shot: {shot.CurrentSpeedMultiplier:F2}x, Master: {shot.MasterPlaybackSpeed:F2}x, Dist: {shot.CurrentSplineDistance:F2}m)"
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
        }
    }
}
