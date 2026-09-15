using UnityEditor;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraRigProfile用のカスタムインスペクター。
    /// 機材応答時間および検証推奨範囲を明確なセクションで提示します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraRigProfile))]
    public class VLiveCameraRigProfileEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _speedResponseTime;
        private SerializedProperty _holdDecelerationTime;
        private SerializedProperty _resumeAccelerationTime;
        private SerializedProperty _reverseDecelerationTime;
        private SerializedProperty _reverseAccelerationTime;

        private SerializedProperty _recommendedMaxSpeed;
        private SerializedProperty _recommendedMaxAcceleration;
        private SerializedProperty _recommendedMaxJerk;
        private SerializedProperty _recommendedMaxAngularSpeed;


        // Methods

        private void OnEnable()
        {
            _speedResponseTime = serializedObject.FindProperty(nameof(_speedResponseTime));
            _holdDecelerationTime = serializedObject.FindProperty(nameof(_holdDecelerationTime));
            _resumeAccelerationTime = serializedObject.FindProperty(nameof(_resumeAccelerationTime));
            _reverseDecelerationTime = serializedObject.FindProperty(nameof(_reverseDecelerationTime));
            _reverseAccelerationTime = serializedObject.FindProperty(nameof(_reverseAccelerationTime));

            _recommendedMaxSpeed = serializedObject.FindProperty(nameof(_recommendedMaxSpeed));
            _recommendedMaxAcceleration = serializedObject.FindProperty(nameof(_recommendedMaxAcceleration));
            _recommendedMaxJerk = serializedObject.FindProperty(nameof(_recommendedMaxJerk));
            _recommendedMaxAngularSpeed = serializedObject.FindProperty(nameof(_recommendedMaxAngularSpeed));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawResponseSection();
            EditorGUILayout.Space(8);

            DrawConstraintsSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawResponseSection()
        {
            EditorGUILayout.LabelField("Operational Responses", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_speedResponseTime);
            EditorGUILayout.PropertyField(_holdDecelerationTime);
            EditorGUILayout.PropertyField(_resumeAccelerationTime);
            EditorGUILayout.PropertyField(_reverseDecelerationTime);
            EditorGUILayout.PropertyField(_reverseAccelerationTime);
        }

        private void DrawConstraintsSection()
        {
            EditorGUILayout.LabelField("Recommended Constraints", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_recommendedMaxSpeed);
            EditorGUILayout.PropertyField(_recommendedMaxAcceleration);
            EditorGUILayout.PropertyField(_recommendedMaxJerk);
            EditorGUILayout.PropertyField(_recommendedMaxAngularSpeed);

            EditorGUILayout.HelpBox("These constraints are used by the Motion Validator for diagnostics and do not alter runtime playback.", MessageType.None);
        }
    }
}
