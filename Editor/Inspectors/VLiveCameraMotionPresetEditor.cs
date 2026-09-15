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

        private SerializedProperty _identity;
        private SerializedProperty _body;
        private SerializedProperty _timing;
        private SerializedProperty _aim;
        private SerializedProperty _lens;
        private SerializedProperty _roll;
        private SerializedProperty _activation;
        private SerializedProperty _rigProfile;

        private VLiveCameraMotionValidationReport _cachedReport;
        private bool _diagnosticsExpanded;


        // Methods

        private void OnEnable()
        {
            SerializedProperty data = serializedObject.FindProperty("_data");
            _identity = data.FindPropertyRelative(nameof(_identity));
            _body = data.FindPropertyRelative(nameof(_body));
            _timing = data.FindPropertyRelative(nameof(_timing));
            _aim = data.FindPropertyRelative(nameof(_aim));
            _lens = data.FindPropertyRelative(nameof(_lens));
            _roll = data.FindPropertyRelative(nameof(_roll));
            _activation = data.FindPropertyRelative(nameof(_activation));
            _rigProfile = data.FindPropertyRelative(nameof(_rigProfile));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            DrawIdentity();
            EditorGUILayout.Space(4f);
            DrawTrack(_body, "Body");
            DrawTrack(_timing, "Timing");
            DrawTrack(_aim, "Aim & Composition");
            DrawTrack(_lens, "Lens");
            DrawTrack(_roll, "Roll");
            DrawTrack(_activation, "Activation");

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
            EditorGUILayout.PropertyField(_identity, new GUIContent("Identity & Intent"), true);
            EditorGUILayout.PropertyField(_rigProfile, new GUIContent("Rig Profile"));
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
