using UnityEditor;
using UnityEngine;

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

            DrawProfileHeader();
            EditorGUILayout.Space(6);

            DrawResponseSection();
            EditorGUILayout.Space(6);

            DrawConstraintsSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProfileHeader()
        {
            var rect = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.09f, 0.13f));

            var titleRect = new Rect(rect.x + 12, rect.y + 6, rect.width - 24, 20);
            var subRect = new Rect(rect.x + 12, rect.y + 26, rect.width - 24, 16);

            EditorGUI.LabelField(titleRect, "VLIVE CAMERA RIG PROFILE", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            });

            EditorGUI.LabelField(subRect, "機材応答特性・検証推奨範囲プロファイル", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.65f, 0.85f, 1f) }
            });
        }

        private void DrawResponseSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Operational Responses (操作応答設定)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_speedResponseTimeProp);
            EditorGUILayout.PropertyField(_holdDecelerationTimeProp);
            EditorGUILayout.PropertyField(_resumeAccelerationTimeProp);
            EditorGUILayout.PropertyField(_reverseDecelerationTimeProp);
            EditorGUILayout.PropertyField(_reverseAccelerationTimeProp);

            EditorGUILayout.EndVertical();
        }

        private void DrawConstraintsSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Recommended Constraints (検証推奨範囲)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_recommendedMaxSpeedProp);
            EditorGUILayout.PropertyField(_recommendedMaxAccelerationProp);
            EditorGUILayout.PropertyField(_recommendedMaxJerkProp);
            EditorGUILayout.PropertyField(_recommendedMaxAngularSpeedProp);

            EditorGUILayout.HelpBox("これらの値はValidatorの警告判定に使用され、Runtime再生の演出を暗黙に変更しません。", MessageType.None);

            EditorGUILayout.EndVertical();
        }
    }
}
