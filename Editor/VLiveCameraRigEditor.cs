using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraRig用のカスタムインスペクター。
    /// Target設定、正面基準、スケール、順序付きShot Slotsの編集、Apply/Syncおよび一括Rebuild操作を提供します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraRig))]
    public class VLiveCameraRigEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _performerTargetProp;
        private SerializedProperty _programCameraProp;
        private SerializedProperty _forwardReferenceModeProp;
        private SerializedProperty _customReferenceProp;
        private SerializedProperty _targetHeightProp;
        private SerializedProperty _distanceScaleProp;
        private SerializedProperty _motionScaleProp;
        private SerializedProperty _verticalMotionScaleProp;
        private SerializedProperty _masterPlaybackSpeedProp;
        private SerializedProperty _slotsProp;


        // Methods

        private void OnEnable()
        {
            _performerTargetProp = serializedObject.FindProperty("_performerTarget");
            _programCameraProp = serializedObject.FindProperty("_programCamera");
            _forwardReferenceModeProp = serializedObject.FindProperty("_forwardReferenceMode");
            _customReferenceProp = serializedObject.FindProperty("_customReference");
            _targetHeightProp = serializedObject.FindProperty("_targetHeight");
            _distanceScaleProp = serializedObject.FindProperty("_distanceScale");
            _motionScaleProp = serializedObject.FindProperty("_motionScale");
            _verticalMotionScaleProp = serializedObject.FindProperty("_verticalMotionScale");
            _masterPlaybackSpeedProp = serializedObject.FindProperty("_masterPlaybackSpeed");
            _slotsProp = serializedObject.FindProperty("_slots");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var rig = (VLiveCameraRig)target;

            DrawSetupAndFramingSection(rig);
            EditorGUILayout.Space(8);

            DrawShotSlotsSection(rig);
            EditorGUILayout.Space(8);

            DrawActionsSection(rig);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSetupAndFramingSection(VLiveCameraRig rig)
        {
            EditorGUILayout.PropertyField(_performerTargetProp);
            EditorGUILayout.PropertyField(_programCameraProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_forwardReferenceModeProp);

            if (_forwardReferenceModeProp.enumValueIndex == (int)ForwardReferenceMode.CustomReference)
            {
                EditorGUILayout.PropertyField(_customReferenceProp);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_targetHeightProp);
            EditorGUILayout.PropertyField(_distanceScaleProp);
            EditorGUILayout.PropertyField(_motionScaleProp, new GUIContent("Horizontal Motion Scale"));
            EditorGUILayout.PropertyField(_verticalMotionScaleProp);
            EditorGUILayout.PropertyField(_masterPlaybackSpeedProp);

            // Validation messages
            if (_performerTargetProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Performer Target is unassigned. Assign a target Transform before running Apply / Sync.", MessageType.Warning);
            }
            else if (_forwardReferenceModeProp.enumValueIndex == (int)ForwardReferenceMode.TargetForward)
            {
                var targetTransform = (Transform)_performerTargetProp.objectReferenceValue;
                if (targetTransform != null)
                {
                    Vector3 fwdXZ = Vector3.ProjectOnPlane(targetTransform.forward, Vector3.up);
                    if (fwdXZ.sqrMagnitude < 0.0001f)
                    {
                        EditorGUILayout.HelpBox("Target forward projected on XZ plane is near-zero. World +Z will be used.", MessageType.Warning);
                    }
                }
            }

            if (_programCameraProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Program Camera is unassigned. Assign a Unity Camera to output program video.", MessageType.Info);
            }

            if (_forwardReferenceModeProp.enumValueIndex == (int)ForwardReferenceMode.CustomReference)
            {
                if (_customReferenceProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Custom Reference is unassigned. Assign a reference Transform before running Apply / Sync.", MessageType.Error);
                }
                else
                {
                    var customTransform = (Transform)_customReferenceProp.objectReferenceValue;
                    Vector3 fwdXZ = Vector3.ProjectOnPlane(customTransform.forward, Vector3.up);
                    if (fwdXZ.sqrMagnitude < 0.0001f)
                    {
                        EditorGUILayout.HelpBox("Custom Reference forward is pointing vertically. Horizontal forward direction cannot be determined.", MessageType.Error);
                    }
                }
            }

            if (_distanceScaleProp.floatValue <= 0f || _motionScaleProp.floatValue <= 0f || _verticalMotionScaleProp.floatValue < 0f)
            {
                EditorGUILayout.HelpBox("Distance and horizontal motion scales must be greater than zero. Vertical motion scale cannot be negative.", MessageType.Warning);
            }
        }

        private void DrawShotSlotsSection(VLiveCameraRig rig)
        {
            EditorGUILayout.LabelField("Shot Slots", EditorStyles.boldLabel);

            int slotCount = _slotsProp.arraySize;

            for (int i = 0; i < slotCount; i++)
            {
                SerializedProperty slotElem = _slotsProp.GetArrayElementAtIndex(i);
                SerializedProperty presetProp = slotElem.FindPropertyRelative("_preset");
                SerializedProperty shotProp = slotElem.FindPropertyRelative("_shot");

                EditorGUILayout.BeginHorizontal();
                string slotTitle = $"Slot {i + 1} (Key {i + 1})";
                EditorGUILayout.LabelField(slotTitle, EditorStyles.boldLabel, GUILayout.Width(110));

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(i == 0 || Application.isPlaying))
                {
                    if (GUILayout.Button("Up", EditorStyles.miniButtonLeft, GUILayout.Width(38)))
                    {
                        _slotsProp.MoveArrayElement(i, i - 1);
                        serializedObject.ApplyModifiedProperties();
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUI.DisabledScope(i == slotCount - 1 || Application.isPlaying))
                {
                    if (GUILayout.Button("Down", EditorStyles.miniButtonMid, GUILayout.Width(44)))
                    {
                        _slotsProp.MoveArrayElement(i, i + 1);
                        serializedObject.ApplyModifiedProperties();
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    if (GUILayout.Button("Remove", EditorStyles.miniButtonRight, GUILayout.Width(58)))
                    {
                        int prevCount = _slotsProp.arraySize;
                        _slotsProp.DeleteArrayElementAtIndex(i);
                        if (_slotsProp.arraySize == prevCount)
                        {
                            _slotsProp.DeleteArrayElementAtIndex(i);
                        }

                        serializedObject.ApplyModifiedProperties();
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(presetProp, new GUIContent("Preset"));

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(shotProp, new GUIContent("Shot"));
                }

                var shotObj = (VLiveCameraShot)shotProp.objectReferenceValue;
                var presetObj = (VLiveCameraMotionPreset)presetProp.objectReferenceValue;

                if (shotObj != null && presetObj != null && shotObj.AppliedPreset != null && shotObj.AppliedPreset != presetObj)
                {
                    EditorGUILayout.HelpBox($"Slot Preset differs from Applied Motion on Shot ({shotObj.AppliedPreset.DisplayName}). Rebuild the Shot in its Inspector to update.", MessageType.Warning);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.Space(2);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("+ Add Shot Slot", GUILayout.Height(22)))
                {
                    _slotsProp.InsertArrayElementAtIndex(_slotsProp.arraySize);
                    SerializedProperty newElem = _slotsProp.GetArrayElementAtIndex(_slotsProp.arraySize - 1);
                    newElem.FindPropertyRelative("_preset").objectReferenceValue = null;
                    newElem.FindPropertyRelative("_shot").objectReferenceValue = null;
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawActionsSection(VLiveCameraRig rig)
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Rig generation, sync, and rebuild operations are disabled in Play Mode.", MessageType.Info);
            }

            bool hasTarget = _performerTargetProp.objectReferenceValue != null;
            bool isForwardValid = rig.IsForwardReferenceValid();
            bool canApply = !Application.isPlaying && hasTarget && isForwardValid;

            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("Apply / Sync", GUILayout.Height(26)))
                {
                    serializedObject.ApplyModifiedProperties();
                    VLiveCameraRigBuilder.ApplySync(rig);
                    serializedObject.Update();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.Space(4);

            bool canRebuildAll = !Application.isPlaying && hasTarget && isForwardValid && _slotsProp.arraySize > 0;

            using (new EditorGUI.DisabledScope(!canRebuildAll))
            {
                if (GUILayout.Button("Rebuild All From Presets", GUILayout.Height(24)))
                {
                    if (EditorUtility.DisplayDialog(
                            "Rebuild All Shots From Presets",
                            "Rebuild camera position, lens, aim, spline, and motion configuration for all shots from their presets?\nManual adjustments will be overwritten.",
                            "Rebuild All",
                            "Cancel"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        VLiveCameraRigBuilder.RebuildAllShotsFromPreset(rig);
                        serializedObject.Update();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
    }
}
