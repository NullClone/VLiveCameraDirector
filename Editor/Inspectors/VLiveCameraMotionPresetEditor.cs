using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// Camera Performanceのトラック編集と数値診断を提供します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraMotionPreset))]
    public class VLiveCameraMotionPresetEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _identityProp;
        private SerializedProperty _bodyProp;
        private SerializedProperty _timingProp;
        private SerializedProperty _aimProp;
        private SerializedProperty _lensProp;
        private SerializedProperty _rollProp;
        private SerializedProperty _activationProp;
        private SerializedProperty _rigProfileProp;

        private VLiveCameraMotionValidationReport _cachedReport;
        private bool _diagnosticsExpanded;


        // Methods

        private void OnEnable()
        {
            SerializedProperty data = serializedObject.FindProperty("_data");
            _identityProp = data.FindPropertyRelative("_identity");
            _bodyProp = data.FindPropertyRelative("_body");
            _timingProp = data.FindPropertyRelative("_timing");
            _aimProp = data.FindPropertyRelative("_aim");
            _lensProp = data.FindPropertyRelative("_lens");
            _rollProp = data.FindPropertyRelative("_roll");
            _activationProp = data.FindPropertyRelative("_activation");
            _rigProfileProp = data.FindPropertyRelative("_rigProfile");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            DrawIdentity();
            EditorGUILayout.Space(4f);
            DrawTrack(_bodyProp, "Body");
            DrawTrack(_timingProp, "Timing");
            DrawTrack(_aimProp, "Aim & Composition");
            DrawTrack(_lensProp, "Lens");
            DrawTrack(_rollProp, "Roll");
            DrawTrack(_activationProp, "Activation");

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                _cachedReport = null;
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(4f);
            DrawDiagnostics((VLiveCameraMotionPreset)target);
        }

        private void DrawIdentity()
        {
            EditorGUILayout.LabelField("Camera Performance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_identityProp, new GUIContent("Identity & Intent"), true);
            EditorGUILayout.PropertyField(_rigProfileProp, new GUIContent("Rig Profile"));
        }

        private static void DrawTrack(SerializedProperty property, string label)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }

        private void DrawDiagnostics(VLiveCameraMotionPreset preset)
        {
            _diagnosticsExpanded = EditorGUILayout.Foldout(_diagnosticsExpanded, "Diagnostics", true);
            if (!_diagnosticsExpanded)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                if (GUILayout.Button("Run Diagnostics"))
                {
                    _cachedReport = VLiveCameraMotionValidator.ValidatePreset(preset);
                }

                if (_cachedReport == null)
                {
                    return;
                }

                EditorGUILayout.LabelField("Duration", $"{_cachedReport.Duration:F2} s");
                EditorGUILayout.LabelField("Spline Length", $"{_cachedReport.SplineLength:F2} m");
                EditorGUILayout.LabelField("Peak Speed", $"{_cachedReport.MaxSpeed:F2} m/s");
                EditorGUILayout.LabelField("Peak Acceleration", $"{_cachedReport.MaxAcceleration:F2} m/s²");
                EditorGUILayout.LabelField("Peak Jerk", $"{_cachedReport.MaxJerk:F2} m/s³");
                EditorGUILayout.LabelField("Field of View", $"{_cachedReport.MinFieldOfView:F1}° - {_cachedReport.MaxFieldOfView:F1}°");

                foreach (VLiveCameraDiagnosticMessage message in _cachedReport.Messages)
                {
                    MessageType type = message.Severity switch
                    {
                        VLiveCameraDiagnosticSeverity.Error => MessageType.Error,
                        VLiveCameraDiagnosticSeverity.Warning => MessageType.Warning,
                        _ => MessageType.Info
                    };

                    EditorGUILayout.HelpBox($"[{message.Category}] {message.Message}", type);
                }
            }
        }
    }
}
