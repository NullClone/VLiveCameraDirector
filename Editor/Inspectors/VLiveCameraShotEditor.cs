using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraShot用のカスタムインスペクター。
    /// ショット設定、適用済みMotionプロファイル、再構築・診断・削除操作、および実行時再生状態の表示を提供します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraShot))]
    [CanEditMultipleObjects]
    public class VLiveCameraShotEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _shotName;
        private SerializedProperty _shotType;
        private SerializedProperty _appliedPreset;
        private SerializedProperty _cinemachineCamera;
        private SerializedProperty _splineDolly;
        private SerializedProperty _targetGroup;

        private VLiveCameraMotionValidationReport _report;
        private bool _showMotionProfile = true;


        // Methods

        private void OnEnable()
        {
            _shotName = serializedObject.FindProperty(nameof(_shotName));
            _shotType = serializedObject.FindProperty(nameof(_shotType));
            _appliedPreset = serializedObject.FindProperty(nameof(_appliedPreset));
            _cinemachineCamera = serializedObject.FindProperty(nameof(_cinemachineCamera));
            _splineDolly = serializedObject.FindProperty(nameof(_splineDolly));
            _targetGroup = serializedObject.FindProperty(nameof(_targetGroup));
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
            CheckMissingReferences();
            EditorGUILayout.Space(6);

            DrawAppliedMotionSection(shot);
            EditorGUILayout.Space(6);

            DrawOperationsSection(shot);

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(6);
                DrawRuntimeSection(shot);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawIdentificationSection(VLiveCameraShot shot)
        {
            EditorGUILayout.LabelField("Shot Configuration", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_shotName);
            EditorGUILayout.PropertyField(_shotType);
            EditorGUILayout.PropertyField(_appliedPreset, new GUIContent("Source Preset"));

            // Status and Slot identification
            int slotIndex = GetSlotIndex(shot, out VLiveCameraMotionPreset slotPreset);
            string slotText = slotIndex >= 0 ? $"Slot {slotIndex + 1} (Key {slotIndex + 1})" : "Not in Rig Slots";
            string statusText = shot.IsLive ? $"Live [Program]  •  {slotText}" : $"Off-Air  •  {slotText}";

            EditorGUILayout.LabelField("Status", statusText, EditorStyles.miniLabel);

            if (slotPreset != null && shot.AppliedPreset != null && slotPreset != shot.AppliedPreset)
            {
                EditorGUILayout.HelpBox($"Rig slot specifies '{slotPreset.DisplayName}', but this shot is using '{shot.AppliedPreset.DisplayName}'. Click 'Rebuild From Preset' below to sync.", MessageType.Warning);
            }
        }

        private void CheckMissingReferences()
        {
            if (_cinemachineCamera.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("CinemachineCamera is missing. Run Apply / Sync on the Rig to resolve.", MessageType.Warning);
            }

            if (_shotType.enumValueIndex == (int)VLiveCameraShotType.Spline && _splineDolly.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Spline Dolly is missing. Run Apply / Sync on the Rig to resolve.", MessageType.Warning);
            }

            if (_targetGroup.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Subject Target Group is missing. Run Apply on the Rig to resolve.", MessageType.Warning);
            }
        }

        private void DrawAppliedMotionSection(VLiveCameraShot shot)
        {
            _showMotionProfile = EditorGUILayout.Foldout(_showMotionProfile, "Applied Motion Profile", true);
            if (!_showMotionProfile)
            {
                return;
            }

            EditorGUI.indentLevel++;

            VLiveCameraAppliedMotion applied = shot.AppliedMotion;
            if (applied != null)
            {
                EditorGUILayout.LabelField("Timing", $"{applied.EffectiveDuration:F2}s ({applied.ScaleMode})  |  Entry: {applied.EntryMode}  |  Exit: {applied.ExitBehavior}");
                EditorGUILayout.LabelField("Framing", $"Aim Offset: {applied.AimOffset}  |  Screen Pos: ({applied.ScreenPosition.x:F2}, {applied.ScreenPosition.y:F2})");
                string lensText = applied.LensMode == LensMode.FocalLength
                    ? $"{applied.FocalLength:F1} mm"
                    : $"{applied.FieldOfView:F1}°";
                EditorGUILayout.LabelField("Lens & Roll", $"Lens: {lensText} ({applied.LensMode})  |  Roll: {applied.RollMode}");
            }
            else
            {
                EditorGUILayout.LabelField("Motion", "(Not applied. Run 'Rebuild From Preset' below.)", EditorStyles.miniLabel);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawOperationsSection(VLiveCameraShot shot)
        {
            if (targets.Length > 1)
            {
                return;
            }

            EditorGUILayout.LabelField("Shot Actions", EditorStyles.boldLabel);

            bool canRebuild = !Application.isPlaying && shot.Rig != null && shot.AppliedPreset != null;
            using (new EditorGUI.DisabledScope(!canRebuild))
            {
                if (GUILayout.Button("Rebuild From Preset", GUILayout.Height(26)))
                {
                    string presetName = shot.AppliedPreset != null ? shot.AppliedPreset.DisplayName : "Preset";
                    if (EditorUtility.DisplayDialog(
                            "Rebuild Shot From Preset",
                            $"Rebuild camera position, lens, aim, spline, and motion configuration for '{shot.ShotName}' from '{presetName}'?\n\nManual adjustments made on the camera and spline will be overwritten.",
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

            EditorGUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Motion", EditorStyles.miniButtonLeft, GUILayout.Height(22)))
                {
                    _report = VLiveCameraMotionValidator.ValidateShot(shot);
                }

                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    if (GUILayout.Button("Delete Shot", EditorStyles.miniButtonRight, GUILayout.Height(22)))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Delete Shot GameObject",
                                $"Delete Shot GameObject '{shot.name}', Spline, and Subject Target Group from the Scene?\n\nThis action can be undone.",
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
                if (_report.Messages.Count == 0)
                {
                    EditorGUILayout.HelpBox($"Validated: Duration {_report.Duration:F2}s, Spline {_report.SplineLength:F2}m, Max Speed {_report.MaxSpeed:F2}m/s. No issues found.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        $"Diagnostics: Duration {_report.Duration:F2}s, Spline {_report.SplineLength:F2}m, Speed {_report.MaxSpeed:F2}m/s",
                        EditorStyles.miniLabel
                    );

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
            EditorGUILayout.LabelField("Live Playback", EditorStyles.boldLabel);

            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Playback controls are unavailable during multi-selection.", MessageType.None);
                return;
            }

            float duration = shot.AppliedMotion != null ? shot.AppliedMotion.EffectiveDuration : 0f;
            float progress = duration > 0.001f ? Mathf.Clamp01(shot.CurrentTime / duration) : 0f;

            string progressText = $"Time: {shot.CurrentTime:F2}s / {duration:F2}s  ({shot.CurrentSpeedMultiplier:F2}x, {shot.CurrentSplineDistance:F1}m)";
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 18), progress, progressText);

            if (shot.IsLive)
            {
                EditorGUILayout.Space(2);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Speed -", EditorStyles.miniButtonLeft))
                    {
                        shot.SpeedDown();
                    }

                    if (GUILayout.Button("Speed +", EditorStyles.miniButtonMid))
                    {
                        shot.SpeedUp();
                    }

                    if (GUILayout.Button("Reverse", EditorStyles.miniButtonMid))
                    {
                        shot.Reverse();
                    }

                    if (shot.IsHolding)
                    {
                        if (GUILayout.Button("Resume", EditorStyles.miniButtonMid))
                        {
                            shot.Resume();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Hold", EditorStyles.miniButtonMid))
                        {
                            shot.Hold();
                        }
                    }

                    if (GUILayout.Button("Freeze", EditorStyles.miniButtonRight))
                    {
                        shot.Freeze();
                    }
                }
            }
        }

        private static int GetSlotIndex(VLiveCameraShot shot, out VLiveCameraMotionPreset slotPreset)
        {
            slotPreset = null;
            var rig = shot.GetComponentInParent<VLiveCameraRig>();
            if (rig != null && rig.Slots != null)
            {
                for (int i = 0; i < rig.Slots.Count; i++)
                {
                    if (rig.Slots[i] != null && rig.Slots[i].Shot == shot)
                    {
                        slotPreset = rig.Slots[i].Preset;
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}
