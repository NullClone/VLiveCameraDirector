using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraKeyboardInput用のカスタムインスペクター。
    /// 入力割り当ての確認、キー重複・競合の検証警告、操作ガイドを提供します。
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
            EditorGUILayout.PropertyField(_cutKeysProp, new GUIContent("Shot Cut Keys"), true);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Motion Control Keys:", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_speedUpKeyProp);
            EditorGUILayout.PropertyField(_speedDownKeyProp);
            EditorGUILayout.PropertyField(_reverseKeyProp);
            EditorGUILayout.PropertyField(_holdKeyProp);
            EditorGUILayout.PropertyField(_resumeKeyProp);

            ValidateKeyConflicts();
#else
            EditorGUILayout.LabelField("Legacy Input Manager が有効です (Alpha1-9, Arrow Keys, R, H, Space)。");
#endif

            EditorGUILayout.EndVertical();
        }

#if ENABLE_INPUT_SYSTEM
        private void ValidateKeyConflicts()
        {
            if (_cutKeysProp == null)
            {
                return;
            }

            var warnings = new List<string>();

            // 1. Cut Keys内の重複チェック
            var seenCutKeys = new HashSet<int>();
            var duplicateCutKeys = new HashSet<int>();

            for (int i = 0; i < _cutKeysProp.arraySize; i++)
            {
                int keyVal = _cutKeysProp.GetArrayElementAtIndex(i).intValue;
                if (keyVal != (int)Key.None)
                {
                    if (!seenCutKeys.Add(keyVal))
                    {
                        duplicateCutKeys.Add(keyVal);
                    }
                }
            }

            foreach (int dupKey in duplicateCutKeys)
            {
                string keyName = ((Key)dupKey).ToString();
                warnings.Add($"Shot Cut Keys 内でキー '{keyName}' が重複して割り当てられています。");
            }

            // 2. Motion Control Keysの収集と重複チェック
            var motionKeys = new Dictionary<string, int>
            {
                { "Speed Up", _speedUpKeyProp.intValue },
                { "Speed Down", _speedDownKeyProp.intValue },
                { "Reverse", _reverseKeyProp.intValue },
                { "Hold", _holdKeyProp.intValue },
                { "Resume", _resumeKeyProp.intValue }
            };

            var seenMotionKeys = new Dictionary<int, string>();
            foreach (var kvp in motionKeys)
            {
                if (kvp.Value != (int)Key.None)
                {
                    if (seenMotionKeys.TryGetValue(kvp.Value, out string existingAction))
                    {
                        string keyName = ((Key)kvp.Value).ToString();
                        warnings.Add($"移動制御キー '{keyName}' が '{existingAction}' と '{kvp.Key}' で重複しています。");
                    }
                    else
                    {
                        seenMotionKeys[kvp.Value] = kvp.Key;
                    }
                }
            }

            // 3. Cut KeysとMotion Control Keysの競合チェック
            foreach (var kvp in seenMotionKeys)
            {
                if (seenCutKeys.Contains(kvp.Key))
                {
                    string keyName = ((Key)kvp.Key).ToString();
                    warnings.Add($"Cut キーと移動制御キー '{kvp.Value}' で同じキー '{keyName}' が競合しています。");
                }
            }

            // 4. 暗黙キー（テンキー1〜9、=、-、テンキー±）との競合チェック
            // 4a. Cut Keys と暗黙キーの検証
            for (int i = 0; i < _cutKeysProp.arraySize; i++)
            {
                int keyVal = _cutKeysProp.GetArrayElementAtIndex(i).intValue;
                if (keyVal == (int)Key.None)
                {
                    continue;
                }

                Key key = (Key)keyVal;

                if (VLiveCameraKeyboardInput.IsImplicitSpeedUpKey(key))
                {
                    warnings.Add($"Cut キー {i + 1} に暗黙の Speed Up キー '{key}' が割り当てられています（同時発火します）。");
                }
                else if (VLiveCameraKeyboardInput.IsImplicitSpeedDownKey(key))
                {
                    warnings.Add($"Cut キー {i + 1} に暗黙の Speed Down キー '{key}' が割り当てられています（同時発火します）。");
                }
                else if (VLiveCameraKeyboardInput.IsImplicitCutKey(key, out int numpadIndex))
                {
                    if (numpadIndex != i)
                    {
                        warnings.Add($"Cut キー {i + 1} に Shot {numpadIndex + 1} の暗黙 Cut キー '{key}' が割り当てられています（同時発火します）。");
                    }
                    else
                    {
                        warnings.Add($"Cut キー {i + 1} に暗黙 Cut キー '{key}' が重複指定されています（テンキーは常時有効のため設定不要です）。");
                    }
                }
            }

            // 4b. 移動制御キーと暗黙キーの検証
            foreach (var kvp in motionKeys)
            {
                if (kvp.Value == (int)Key.None)
                {
                    continue;
                }

                Key key = (Key)kvp.Value;

                if (VLiveCameraKeyboardInput.IsImplicitCutKey(key, out int numpadIndex))
                {
                    warnings.Add($"移動制御キー '{kvp.Key}' に Shot {numpadIndex + 1} の暗黙 Cut キー '{key}' が割り当てられています（同時発火します）。");
                }
                else if (VLiveCameraKeyboardInput.IsImplicitSpeedUpKey(key))
                {
                    if (kvp.Key != "Speed Up")
                    {
                        warnings.Add($"移動制御キー '{kvp.Key}' に暗黙の Speed Up キー '{key}' が割り当てられています（同時発火します）。");
                    }
                    else
                    {
                        warnings.Add($"移動制御キー 'Speed Up' に暗黙キー '{key}' が指定されています（既に暗黙で有効なため設定不要です）。");
                    }
                }
                else if (VLiveCameraKeyboardInput.IsImplicitSpeedDownKey(key))
                {
                    if (kvp.Key != "Speed Down")
                    {
                        warnings.Add($"移動制御キー '{kvp.Key}' に暗黙の Speed Down キー '{key}' が割り当てられています（同時発火します）。");
                    }
                    else
                    {
                        warnings.Add($"移動制御キー 'Speed Down' に暗黙キー '{key}' が指定されています（既に暗黙で有効なため設定不要です）。");
                    }
                }
            }

            if (warnings.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(string.Join("\n", warnings), MessageType.Warning);
            }
        }
#endif

        private void DrawGuideSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Operation Guide (操作ガイド)", EditorStyles.boldLabel);

            string guideText =
                "【キーボード操作一覧】\n" +
                "・ 数字キー 1〜N: 対応番号のShotへ直接Cut（テンキー対応）\n" +
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
