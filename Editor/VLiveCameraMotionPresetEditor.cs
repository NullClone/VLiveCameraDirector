using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraMotionPreset用のカスタムインスペクター。
    /// 各Trackの階層的表示とMotion Validatorによるリアルタイム診断情報を提供します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraMotionPreset))]
    public class VLiveCameraMotionPresetEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _displayNameProp;
        private SerializedProperty _shotTypeProp;
        private SerializedProperty _motionFamilyProp;
        private SerializedProperty _shotSizeProp;
        private SerializedProperty _energyProp;
        private SerializedProperty _descriptionProp;
        private SerializedProperty _rigProfileProp;

        private SerializedProperty _knotsProp;
        private SerializedProperty _isClosedProp;
        private SerializedProperty _referenceSplineLengthProp;
        private SerializedProperty _startDistanceProp;
        private SerializedProperty _endDistanceProp;

        private SerializedProperty _clipDurationProp;
        private SerializedProperty _progressCurveProp;
        private SerializedProperty _scaleTimingModeProp;
        private SerializedProperty _minSpeedMultiplierProp;
        private SerializedProperty _maxSpeedMultiplierProp;
        private SerializedProperty _speedStepProp;

        private SerializedProperty _aimOffsetProp;
        private SerializedProperty _aimOffsetXCurveProp;
        private SerializedProperty _aimOffsetYCurveProp;
        private SerializedProperty _aimOffsetZCurveProp;
        private SerializedProperty _screenPositionProp;
        private SerializedProperty _screenPositionXCurveProp;
        private SerializedProperty _screenPositionYCurveProp;
        private SerializedProperty _deadZoneEnabledProp;
        private SerializedProperty _deadZoneSizeProp;
        private SerializedProperty _hardLimitsEnabledProp;
        private SerializedProperty _hardLimitsSizeProp;
        private SerializedProperty _hardLimitsOffsetProp;
        private SerializedProperty _dampingProp;
        private SerializedProperty _lookaheadEnabledProp;
        private SerializedProperty _lookaheadTimeProp;
        private SerializedProperty _lookaheadSmoothingProp;
        private SerializedProperty _centerOnActivateProp;

        private SerializedProperty _lensModeProp;
        private SerializedProperty _fieldOfViewProp;
        private SerializedProperty _fieldOfViewCurveProp;
        private SerializedProperty _focalLengthProp;
        private SerializedProperty _focalLengthCurveProp;
        private SerializedProperty _sensorSizeProp;

        private SerializedProperty _rollModeProp;
        private SerializedProperty _rollCurveProp;

        private SerializedProperty _entryModeProp;
        private SerializedProperty _inTimeProp;
        private SerializedProperty _outTimeProp;
        private SerializedProperty _exitBehaviorProp;

        private VLiveCameraMotionValidator.ValidationReport _cachedReport;


        // Methods

        private void OnEnable()
        {
            _displayNameProp = serializedObject.FindProperty("_displayName");
            _shotTypeProp = serializedObject.FindProperty("_shotType");
            _motionFamilyProp = serializedObject.FindProperty("_motionFamily");
            _shotSizeProp = serializedObject.FindProperty("_shotSize");
            _energyProp = serializedObject.FindProperty("_energy");
            _descriptionProp = serializedObject.FindProperty("_description");
            _rigProfileProp = serializedObject.FindProperty("_rigProfile");

            _knotsProp = serializedObject.FindProperty("_knots");
            _isClosedProp = serializedObject.FindProperty("_isClosed");
            _referenceSplineLengthProp = serializedObject.FindProperty("_referenceSplineLength");
            _startDistanceProp = serializedObject.FindProperty("_startDistance");
            _endDistanceProp = serializedObject.FindProperty("_endDistance");

            _clipDurationProp = serializedObject.FindProperty("_clipDuration");
            _progressCurveProp = serializedObject.FindProperty("_progressCurve");
            _scaleTimingModeProp = serializedObject.FindProperty("_scaleTimingMode");
            _minSpeedMultiplierProp = serializedObject.FindProperty("_minSpeedMultiplier");
            _maxSpeedMultiplierProp = serializedObject.FindProperty("_maxSpeedMultiplier");
            _speedStepProp = serializedObject.FindProperty("_speedStep");

            _aimOffsetProp = serializedObject.FindProperty("_aimOffset");
            _aimOffsetXCurveProp = serializedObject.FindProperty("_aimOffsetXCurve");
            _aimOffsetYCurveProp = serializedObject.FindProperty("_aimOffsetYCurve");
            _aimOffsetZCurveProp = serializedObject.FindProperty("_aimOffsetZCurve");
            _screenPositionProp = serializedObject.FindProperty("_screenPosition");
            _screenPositionXCurveProp = serializedObject.FindProperty("_screenPositionXCurve");
            _screenPositionYCurveProp = serializedObject.FindProperty("_screenPositionYCurve");
            _deadZoneEnabledProp = serializedObject.FindProperty("_deadZoneEnabled");
            _deadZoneSizeProp = serializedObject.FindProperty("_deadZoneSize");
            _hardLimitsEnabledProp = serializedObject.FindProperty("_hardLimitsEnabled");
            _hardLimitsSizeProp = serializedObject.FindProperty("_hardLimitsSize");
            _hardLimitsOffsetProp = serializedObject.FindProperty("_hardLimitsOffset");
            _dampingProp = serializedObject.FindProperty("_damping");
            _lookaheadEnabledProp = serializedObject.FindProperty("_lookaheadEnabled");
            _lookaheadTimeProp = serializedObject.FindProperty("_lookaheadTime");
            _lookaheadSmoothingProp = serializedObject.FindProperty("_lookaheadSmoothing");
            _centerOnActivateProp = serializedObject.FindProperty("_centerOnActivate");

            _lensModeProp = serializedObject.FindProperty("_lensMode");
            _fieldOfViewProp = serializedObject.FindProperty("_fieldOfView");
            _fieldOfViewCurveProp = serializedObject.FindProperty("_fieldOfViewCurve");
            _focalLengthProp = serializedObject.FindProperty("_focalLength");
            _focalLengthCurveProp = serializedObject.FindProperty("_focalLengthCurve");
            _sensorSizeProp = serializedObject.FindProperty("_sensorSize");

            _rollModeProp = serializedObject.FindProperty("_rollMode");
            _rollCurveProp = serializedObject.FindProperty("_rollCurve");

            _entryModeProp = serializedObject.FindProperty("_entryMode");
            _inTimeProp = serializedObject.FindProperty("_inTime");
            _outTimeProp = serializedObject.FindProperty("_outTime");
            _exitBehaviorProp = serializedObject.FindProperty("_exitBehavior");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var preset = (VLiveCameraMotionPreset)target;

            DrawHeader(preset);
            EditorGUILayout.Space(6);

            DrawIdentitySection();
            EditorGUILayout.Space(6);

            DrawBodySection();
            EditorGUILayout.Space(6);

            DrawTimingSection();
            EditorGUILayout.Space(6);

            DrawAimSection();
            EditorGUILayout.Space(6);

            DrawLensSection();
            EditorGUILayout.Space(6);

            DrawRollSection();
            EditorGUILayout.Space(6);

            DrawActivationSection();
            EditorGUILayout.Space(6);

            DrawDiagnosticsSection(preset);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(VLiveCameraMotionPreset preset)
        {
            var rect = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.09f, 0.13f));

            var titleRect = new Rect(rect.x + 12, rect.y + 6, rect.width - 24, 20);
            var subRect = new Rect(rect.x + 12, rect.y + 26, rect.width - 24, 16);

            EditorGUI.LabelField(titleRect, "VLIVE CAMERA MOTION PRESET", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            });

            EditorGUI.LabelField(subRect, $"{preset.DisplayName}  |  {preset.ShotType} ({preset.Family})  |  {preset.EntryMode} Entry", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.65f, 0.85f, 1f) }
            });
        }

        private void DrawIdentitySection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Identity & Intent (識別と意図)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_displayNameProp);
            EditorGUILayout.PropertyField(_shotTypeProp);
            EditorGUILayout.PropertyField(_motionFamilyProp);
            EditorGUILayout.PropertyField(_shotSizeProp);
            EditorGUILayout.PropertyField(_energyProp);
            EditorGUILayout.PropertyField(_descriptionProp);
            EditorGUILayout.PropertyField(_rigProfileProp);

            EditorGUILayout.EndVertical();
        }

        private void DrawBodySection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Body Track (移動軌道)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_isClosedProp);
            EditorGUILayout.PropertyField(_referenceSplineLengthProp);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_startDistanceProp, new GUIContent("Start Distance (m)"));
                EditorGUILayout.PropertyField(_endDistanceProp, new GUIContent("End Distance (m)"));
            }

            EditorGUILayout.PropertyField(_knotsProp, true);

            EditorGUILayout.EndVertical();
        }

        private void DrawTimingSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Timing Track (タイミング設定)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_clipDurationProp);
            EditorGUILayout.PropertyField(_progressCurveProp);
            EditorGUILayout.PropertyField(_scaleTimingModeProp);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_minSpeedMultiplierProp, new GUIContent("Min Speed Mult"));
                EditorGUILayout.PropertyField(_maxSpeedMultiplierProp, new GUIContent("Max Speed Mult"));
            }

            EditorGUILayout.PropertyField(_speedStepProp);

            EditorGUILayout.EndVertical();
        }

        private void DrawAimSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Aim & Composition (注視・構図設定)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_aimOffsetProp);
            if (_aimOffsetXCurveProp != null && _aimOffsetYCurveProp != null && _aimOffsetZCurveProp != null)
            {
                EditorGUILayout.PropertyField(_aimOffsetXCurveProp);
                EditorGUILayout.PropertyField(_aimOffsetYCurveProp);
                EditorGUILayout.PropertyField(_aimOffsetZCurveProp);
            }

            EditorGUILayout.PropertyField(_screenPositionProp);
            if (_screenPositionXCurveProp != null && _screenPositionYCurveProp != null)
            {
                EditorGUILayout.PropertyField(_screenPositionXCurveProp);
                EditorGUILayout.PropertyField(_screenPositionYCurveProp);
            }

            EditorGUILayout.PropertyField(_deadZoneEnabledProp);
            if (_deadZoneEnabledProp.boolValue)
            {
                EditorGUILayout.PropertyField(_deadZoneSizeProp);
            }

            EditorGUILayout.PropertyField(_hardLimitsEnabledProp);
            if (_hardLimitsEnabledProp.boolValue)
            {
                EditorGUILayout.PropertyField(_hardLimitsSizeProp);
                EditorGUILayout.PropertyField(_hardLimitsOffsetProp);
            }

            EditorGUILayout.PropertyField(_dampingProp);
            EditorGUILayout.PropertyField(_lookaheadEnabledProp);
            if (_lookaheadEnabledProp.boolValue)
            {
                EditorGUILayout.PropertyField(_lookaheadTimeProp);
                EditorGUILayout.PropertyField(_lookaheadSmoothingProp);
            }

            EditorGUILayout.PropertyField(_centerOnActivateProp);

            EditorGUILayout.EndVertical();
        }

        private void DrawLensSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Lens Track (レンズ設定)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_lensModeProp);
            if (_lensModeProp.enumValueIndex == (int)LensMode.FieldOfView)
            {
                EditorGUILayout.PropertyField(_fieldOfViewProp);
                EditorGUILayout.PropertyField(_fieldOfViewCurveProp);
            }
            else
            {
                EditorGUILayout.PropertyField(_focalLengthProp);
                EditorGUILayout.PropertyField(_focalLengthCurveProp);
                EditorGUILayout.PropertyField(_sensorSizeProp);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRollSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Roll Track (ロール設定)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_rollModeProp);
            if (_rollModeProp.enumValueIndex == (int)RollMode.RollCurve)
            {
                EditorGUILayout.PropertyField(_rollCurveProp);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActivationSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Activation & Entry (開始・終了設定)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_entryModeProp);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_inTimeProp, new GUIContent("In Time"));
                EditorGUILayout.PropertyField(_outTimeProp, new GUIContent("Out Time"));
            }

            EditorGUILayout.PropertyField(_exitBehaviorProp);

            EditorGUILayout.EndVertical();
        }

        private void DrawDiagnosticsSection(VLiveCameraMotionPreset preset)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Motion Diagnostics (運動・構図診断)", EditorStyles.boldLabel);

            if (GUILayout.Button("Run Diagnostics (診断実行)", GUILayout.Height(26)))
            {
                _cachedReport = VLiveCameraMotionValidator.ValidatePreset(preset);
            }

            if (_cachedReport != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"Duration: {_cachedReport.Duration:F2}s  |  Spline Length: {_cachedReport.SplineLength:F2}m", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Peak Speed: {_cachedReport.MaxSpeed:F2} m/s  |  Peak Acc: {_cachedReport.MaxAcceleration:F2} m/s²  |  Peak Jerk: {_cachedReport.MaxJerk:F2} m/s³", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"FOV Range: {_cachedReport.MinFieldOfView:F1}° 〜 {_cachedReport.MaxFieldOfView:F1}°", EditorStyles.miniLabel);

                EditorGUILayout.Space(4);
                foreach (var msg in _cachedReport.Messages)
                {
                    MessageType msgType = msg.Severity switch
                    {
                        VLiveCameraMotionValidator.DiagnosticSeverity.Error => MessageType.Error,
                        VLiveCameraMotionValidator.DiagnosticSeverity.Warning => MessageType.Warning,
                        _ => MessageType.Info
                    };

                    EditorGUILayout.HelpBox($"[{msg.Category}] {msg.Message}", msgType);
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
