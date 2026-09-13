using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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

        private SerializedProperty _letterboxEnabledProp;
        private SerializedProperty _aspectRatioProp;
        private SerializedProperty _letterboxColorProp;
        private SerializedProperty _letterboxOpacityProp;
        private SerializedProperty _animateOnStartProp;
        private SerializedProperty _frameResponseTimeProp;
        private SerializedProperty _guideModeProp;
        private SerializedProperty _drawGuidesProp;
        private SerializedProperty _lineColorProp;
        private SerializedProperty _lineWidthProp;
        private SerializedProperty _showSettingsOnStartProp;
        private SerializedProperty _settingsResponseTimeProp;
        private SerializedProperty _panelThemeProp;
        private SerializedProperty _panelScaleProp;


        // Methods

        private void OnEnable()
        {
            _letterboxEnabledProp = serializedObject.FindProperty("_letterboxEnabled");
            _aspectRatioProp = serializedObject.FindProperty("_aspectRatio");
            _letterboxColorProp = serializedObject.FindProperty("_letterboxColor");
            _letterboxOpacityProp = serializedObject.FindProperty("_letterboxOpacity");
            _animateOnStartProp = serializedObject.FindProperty("_animateOnStart");
            _frameResponseTimeProp = serializedObject.FindProperty("_frameResponseTime");
            _guideModeProp = serializedObject.FindProperty("_guideMode");
            _drawGuidesProp = serializedObject.FindProperty("_drawGuides");
            _lineColorProp = serializedObject.FindProperty("_lineColor");
            _lineWidthProp = serializedObject.FindProperty("_lineWidth");
            _showSettingsOnStartProp = serializedObject.FindProperty("_showSettingsOnStart");
            _settingsResponseTimeProp = serializedObject.FindProperty("_settingsResponseTime");
            _panelThemeProp = serializedObject.FindProperty("_panelTheme");
            _panelScaleProp = serializedObject.FindProperty("_panelScale");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawUiDocumentStatus();

            EditorGUILayout.LabelField("Frame Mask", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_letterboxEnabledProp, new GUIContent("Enabled"));

            using (new EditorGUI.DisabledScope(!_letterboxEnabledProp.boolValue))
            {
                EditorGUILayout.PropertyField(_aspectRatioProp, new GUIContent("Aspect Ratio"));
                EditorGUILayout.PropertyField(_letterboxColorProp, new GUIContent("Color"));
                EditorGUILayout.PropertyField(_letterboxOpacityProp, new GUIContent("Opacity"));
                EditorGUILayout.PropertyField(_animateOnStartProp, new GUIContent("Animate On Start"));
                EditorGUILayout.PropertyField(_frameResponseTimeProp, new GUIContent("Response Time"));
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Composition Lines", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_drawGuidesProp, new GUIContent("Draw Frame + Lines"));

            using (new EditorGUI.DisabledScope(!_drawGuidesProp.boolValue))
            {
                EditorGUILayout.PropertyField(_guideModeProp, new GUIContent("Guide Mode"));
                EditorGUILayout.PropertyField(_lineColorProp, new GUIContent("Line Color"));
                EditorGUILayout.PropertyField(_lineWidthProp, new GUIContent("Line Width"));
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Runtime Panel", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_showSettingsOnStartProp, new GUIContent("Open On Start"));
            EditorGUILayout.PropertyField(_settingsResponseTimeProp, new GUIContent("Response Time"));
            EditorGUILayout.PropertyField(_panelThemeProp, new GUIContent("Theme"));
            EditorGUILayout.PropertyField(_panelScaleProp, new GUIContent("Scale"));

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying && targets.Length == 1)
            {
                EditorGUILayout.Space(8f);
                DrawRuntimeActions();
            }
        }

        private void DrawUiDocumentStatus()
        {
            if (targets.Length != 1)
                return;

            SplitLines splitLines = (SplitLines)target;
            UIDocument document = splitLines.GetComponent<UIDocument>();
            bool configured = document != null && document.panelSettings != null && document.visualTreeAsset != null;
            if (configured)
                return;

            EditorGUILayout.HelpBox(
                "The UI Document needs the VLive Camera Director Panel Settings and UXML asset.",
                MessageType.Warning);

            if (GUILayout.Button("Configure UI Document"))
                VLiveCameraCompositionOverlayBuilder.Configure(splitLines);

            EditorGUILayout.Space(6f);
        }

        private void DrawRuntimeActions()
        {
            SplitLines splitLines = (SplitLines)target;
            EditorGUILayout.LabelField("Live Controls", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Settings"))
                    splitLines.SetSettingsOpen(true);

                if (GUILayout.Button("Close Settings"))
                    splitLines.SetSettingsOpen(false);
            }

            if (GUILayout.Button("Reset Runtime Settings"))
                splitLines.ResetRuntimeSettings();
        }
    }
}
