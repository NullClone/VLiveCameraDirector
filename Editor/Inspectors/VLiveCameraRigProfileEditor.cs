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

        private SerializedProperty _speedResponseTimeProp;
        private SerializedProperty _holdDecelerationTimeProp;
        private SerializedProperty _resumeAccelerationTimeProp;
        private SerializedProperty _reverseDecelerationTimeProp;
        private SerializedProperty _reverseAccelerationTimeProp;

        private SerializedProperty _recommendedMaxSpeedProp;
        private SerializedProperty _recommendedMaxAccelerationProp;
        private SerializedProperty _recommendedMaxJerkProp;
        private SerializedProperty _recommendedMaxAngularSpeedProp;


        // Methods

        private void OnEnable()
        {
            _speedResponseTimeProp = serializedObject.FindProperty("_speedResponseTime");
            _holdDecelerationTimeProp = serializedObject.FindProperty("_holdDecelerationTime");
            _resumeAccelerationTimeProp = serializedObject.FindProperty("_resumeAccelerationTime");
            _reverseDecelerationTimeProp = serializedObject.FindProperty("_reverseDecelerationTime");
            _reverseAccelerationTimeProp = serializedObject.FindProperty("_reverseAccelerationTime");

            _recommendedMaxSpeedProp = serializedObject.FindProperty("_recommendedMaxSpeed");
            _recommendedMaxAccelerationProp = serializedObject.FindProperty("_recommendedMaxAcceleration");
            _recommendedMaxJerkProp = serializedObject.FindProperty("_recommendedMaxJerk");
            _recommendedMaxAngularSpeedProp = serializedObject.FindProperty("_recommendedMaxAngularSpeed");
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

            EditorGUILayout.PropertyField(_speedResponseTimeProp);
            EditorGUILayout.PropertyField(_holdDecelerationTimeProp);
            EditorGUILayout.PropertyField(_resumeAccelerationTimeProp);
            EditorGUILayout.PropertyField(_reverseDecelerationTimeProp);
            EditorGUILayout.PropertyField(_reverseAccelerationTimeProp);
        }

        private void DrawConstraintsSection()
        {
            EditorGUILayout.LabelField("Recommended Constraints", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_recommendedMaxSpeedProp);
            EditorGUILayout.PropertyField(_recommendedMaxAccelerationProp);
            EditorGUILayout.PropertyField(_recommendedMaxJerkProp);
            EditorGUILayout.PropertyField(_recommendedMaxAngularSpeedProp);

            EditorGUILayout.HelpBox("These constraints are used by the Motion Validator for diagnostics and do not alter runtime playback.", MessageType.None);
        }
    }
}
