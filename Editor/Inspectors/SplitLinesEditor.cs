using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// 構図ガイドとレターボックスを標準IMGUIで編集します。
    /// </summary>
    [CustomEditor(typeof(SplitLines))]
    [CanEditMultipleObjects]
    public class SplitLinesEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _guideModeProp;
        private SerializedProperty _drawGuidesProp;
        private SerializedProperty _lineColorProp;
        private SerializedProperty _lineWidthProp;
        private SerializedProperty _letterboxEnabledProp;
        private SerializedProperty _letterboxColorProp;
        private SerializedProperty _letterboxRatioProp;


        // Methods

        private void OnEnable()
        {
            _guideModeProp = serializedObject.FindProperty("_guideMode");
            _drawGuidesProp = serializedObject.FindProperty("_drawGuides");
            _lineColorProp = serializedObject.FindProperty("_lineColor");
            _lineWidthProp = serializedObject.FindProperty("_lineWidth");
            _letterboxEnabledProp = serializedObject.FindProperty("_letterboxEnabled");
            _letterboxColorProp = serializedObject.FindProperty("_letterboxColor");
            _letterboxRatioProp = serializedObject.FindProperty("_letterboxRatio");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Composition Guides", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_drawGuidesProp, new GUIContent("Draw Guides"));

            using (new EditorGUI.DisabledScope(!_drawGuidesProp.boolValue))
            {
                EditorGUILayout.PropertyField(_guideModeProp, new GUIContent("Guide Mode"));
                EditorGUILayout.PropertyField(_lineColorProp);
                EditorGUILayout.PropertyField(_lineWidthProp);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Letterbox", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_letterboxEnabledProp, new GUIContent("Enabled"));

            using (new EditorGUI.DisabledScope(!_letterboxEnabledProp.boolValue))
            {
                EditorGUILayout.PropertyField(_letterboxColorProp, new GUIContent("Color"));
                EditorGUILayout.PropertyField(_letterboxRatioProp, new GUIContent("Ratio"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
