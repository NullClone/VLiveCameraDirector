using UnityEditor;
using UnityEngine;

namespace toshi.VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraKeyboardInput用のカスタムインスペクター。
    /// 入力割り当ての確認と操作ガイドを提供します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraKeyboardInput))]
    [CanEditMultipleObjects]
    public class VLiveCameraKeyboardInputEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _switcherProp;
#if ENABLE_INPUT_SYSTEM
        private SerializedProperty _cutKeysProp;
        private SerializedProperty _speedUpKeyProp;
        private SerializedProperty _speedDownKeyProp;
        private SerializedProperty _reverseKeyProp;
        private SerializedProperty _holdKeyProp;
        private SerializedProperty _resumeKeyProp;
#endif


        // Methods

        private void OnEnable()
        {
            _switcherProp = serializedObject.FindProperty("_switcher");
#if ENABLE_INPUT_SYSTEM
            _cutKeysProp = serializedObject.FindProperty("_cutKeys");
            _speedUpKeyProp = serializedObject.FindProperty("_speedUpKey");
            _speedDownKeyProp = serializedObject.FindProperty("_speedDownKey");
            _reverseKeyProp = serializedObject.FindProperty("_reverseKey");
            _holdKeyProp = serializedObject.FindProperty("_holdKey");
            _resumeKeyProp = serializedObject.FindProperty("_resumeKey");
#endif
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawTitleBanner();
            EditorGUILayout.Space(6);

            DrawTargetSection();
            EditorGUILayout.Space(6);

            DrawBindingsSection();
            EditorGUILayout.Space(6);

            DrawGuideSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTitleBanner()
        {
            var rect = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.09f, 0.13f));

            var titleRect = new Rect(rect.x + 12, rect.y + 6, rect.width - 24, 20);
            var subRect = new Rect(rect.x + 12, rect.y + 26, rect.width - 24, 16);

            EditorGUI.LabelField(titleRect, "VLIVE CAMERA KEYBOARD INPUT", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            });

            EditorGUI.LabelField(subRect, "キーボードによるショットCutおよび移動制御", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.65f, 0.85f, 1f) }
            });
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Target Switcher (制御対象)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_switcherProp);

            if (_switcherProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("VLiveCameraSwitcher が未割り当てです。入力によるカメラ切り替えが機能しません。", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBindingsSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Key Bindings (キー設定)", EditorStyles.boldLabel);

#if ENABLE_INPUT_SYSTEM
            EditorGUILayout.PropertyField(_cutKeysProp, new GUIContent("Shot Cut Keys (1〜9)"), true);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Motion Control Keys:", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_speedUpKeyProp);
            EditorGUILayout.PropertyField(_speedDownKeyProp);
            EditorGUILayout.PropertyField(_reverseKeyProp);
            EditorGUILayout.PropertyField(_holdKeyProp);
            EditorGUILayout.PropertyField(_resumeKeyProp);
#else
            EditorGUILayout.LabelField("Legacy Input Manager が有効です (Alpha1-9, Arrow Keys, R, H, Space)。");
#endif

            EditorGUILayout.EndVertical();
        }

        private void DrawGuideSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Operation Guide (操作ガイド)", EditorStyles.boldLabel);

            string guideText =
                "【キーボード操作一覧】\n" +
                "・ 数字キー 1〜9: 対応番号のShotへ直接Cut（テンキー対応）\n" +
                "・ ↑ / ＝ / ＋: Spline進行速度アップ\n" +
                "・ ↓ / －: Spline進行速度ダウン\n" +
                "・ R: 進行方向の反転 (Reverse)\n" +
                "・ H: 移動の一時停止 (Hold)\n" +
                "・ Space: 移動の再開 (Resume)\n\n" +
                "※ 同一Shotの再選択は現在の状態を維持します。\n" +
                "※ Fixed Shotへの移動操作は安全に無視されます。";

            EditorGUILayout.HelpBox(guideText, MessageType.None);

            EditorGUILayout.EndVertical();
        }
    }
}
