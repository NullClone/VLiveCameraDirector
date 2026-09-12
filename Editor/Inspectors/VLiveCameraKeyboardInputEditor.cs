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
        private SerializedProperty _freezeKeyProp;
#endif
        private bool _showControlsReference = false;


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
            _freezeKeyProp = serializedObject.FindProperty("_freezeKey");
#endif
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawTargetSection();
            EditorGUILayout.Space(8);

            DrawBindingsSection();
            EditorGUILayout.Space(8);

            DrawControlsReferenceSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("Target Switcher", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_switcherProp);

            if (_switcherProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("VLiveCameraSwitcher is unassigned. Keyboard camera switching will not function.", MessageType.Warning);
            }
        }

        private void DrawBindingsSection()
        {
            EditorGUILayout.LabelField("Key Bindings", EditorStyles.boldLabel);

#if ENABLE_INPUT_SYSTEM
            EditorGUILayout.PropertyField(_cutKeysProp, new GUIContent("Shot Cut Keys"), true);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Motion Control Keys", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_speedUpKeyProp);
            EditorGUILayout.PropertyField(_speedDownKeyProp);
            EditorGUILayout.PropertyField(_reverseKeyProp);
            EditorGUILayout.PropertyField(_holdKeyProp);
            EditorGUILayout.PropertyField(_resumeKeyProp);
            EditorGUILayout.PropertyField(_freezeKeyProp);

            ValidateKeyConflicts();
#else
            EditorGUILayout.LabelField("Legacy Input Manager active (Alpha1-9, Arrow Keys, R, H, Space).");
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void ValidateKeyConflicts()
        {
            if (_cutKeysProp == null)
            {
                return;
            }

            var warnings = new List<string>();

            // 1. Cut keys duplicate check
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
                warnings.Add($"Duplicate cut key '{keyName}' assigned in Shot Cut Keys.");
            }

            // 2. Motion control keys duplicate check
            var motionKeys = new Dictionary<string, int>
            {
                { "Speed Up", _speedUpKeyProp.intValue },
                { "Speed Down", _speedDownKeyProp.intValue },
                { "Reverse", _reverseKeyProp.intValue },
                { "Hold", _holdKeyProp.intValue },
                { "Resume", _resumeKeyProp.intValue },
                { "Freeze", _freezeKeyProp.intValue }
            };

            var seenMotionKeys = new Dictionary<int, string>();
            foreach (var kvp in motionKeys)
            {
                if (kvp.Value != (int)Key.None)
                {
                    if (seenMotionKeys.TryGetValue(kvp.Value, out string existingAction))
                    {
                        string keyName = ((Key)kvp.Value).ToString();
                        warnings.Add($"Duplicate motion key '{keyName}' used for both '{existingAction}' and '{kvp.Key}'.");
                    }
                    else
                    {
                        seenMotionKeys[kvp.Value] = kvp.Key;
                    }
                }
            }

            // 3. Cut keys vs Motion keys conflict check
            foreach (var kvp in seenMotionKeys)
            {
                if (seenCutKeys.Contains(kvp.Key))
                {
                    string keyName = ((Key)kvp.Key).ToString();
                    warnings.Add($"Conflict between Cut key and Motion key '{kvp.Value}' for key '{keyName}'.");
                }
            }

            // 4. Implicit keys check
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
                    warnings.Add($"Cut key {i + 1} has implicit Speed Up key '{key}' assigned (will trigger simultaneously).");
                }
                else if (VLiveCameraKeyboardInput.IsImplicitSpeedDownKey(key))
                {
                    warnings.Add($"Cut key {i + 1} has implicit Speed Down key '{key}' assigned (will trigger simultaneously).");
                }
                else if (VLiveCameraKeyboardInput.IsImplicitCutKey(key, out int numpadIndex))
                {
                    if (numpadIndex != i)
                    {
                        warnings.Add($"Cut key {i + 1} has implicit Cut key '{key}' for Shot {numpadIndex + 1} assigned (will trigger simultaneously).");
                    }
                    else
                    {
                        warnings.Add($"Cut key {i + 1} has implicit Cut key '{key}' assigned (numpad is active by default, duplicate assignment).");
                    }
                }
            }

            foreach (var kvp in motionKeys)
            {
                if (kvp.Value == (int)Key.None)
                {
                    continue;
                }

                Key key = (Key)kvp.Value;

                if (VLiveCameraKeyboardInput.IsImplicitCutKey(key, out int numpadIndex))
                {
                    warnings.Add($"Motion key '{kvp.Key}' has implicit Cut key '{key}' for Shot {numpadIndex + 1} assigned (will trigger simultaneously).");
                }
                else if (VLiveCameraKeyboardInput.IsImplicitSpeedUpKey(key))
                {
                    if (kvp.Key != "Speed Up")
                    {
                        warnings.Add($"Motion key '{kvp.Key}' has implicit Speed Up key '{key}' assigned (will trigger simultaneously).");
                    }
                }
                else if (VLiveCameraKeyboardInput.IsImplicitSpeedDownKey(key))
                {
                    if (kvp.Key != "Speed Down")
                    {
                        warnings.Add($"Motion key '{kvp.Key}' has implicit Speed Down key '{key}' assigned (will trigger simultaneously).");
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

        private void DrawControlsReferenceSection()
        {
            _showControlsReference = EditorGUILayout.Foldout(_showControlsReference, "Controls Reference");
            if (_showControlsReference)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("• Numbers 1-9 / Numpad 1-9: Direct cut to corresponding shot");
                EditorGUILayout.LabelField("• Up Arrow / = / +: Speed up spline motion");
                EditorGUILayout.LabelField("• Down Arrow / -: Speed down spline motion");
                EditorGUILayout.LabelField("• R: Reverse motion direction");
                EditorGUILayout.LabelField("• Space / H: Hold or resume motion");
                EditorGUI.indentLevel--;
            }
        }
    }
}
