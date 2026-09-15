using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraRig用のカスタムインスペクター。
    /// 基準座標、Physical Camera、Rig共通演者、Shot Slot Override、Applyおよび一括Rebuild操作を提供します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraRig))]
    public class VLiveCameraRigEditor : UnityEditor.Editor
    {
        // Fields

        private static readonly GUIContent s_arrowContent = new GUIContent("→");
        private static readonly GUIContent s_warningContent = new GUIContent("⚠", "Slot Preset differs from Applied Preset on Shot. Rebuild Shot to apply.");
        private static readonly GUIContent s_motionScaleContent = new GUIContent("Horizontal Motion Scale");
        private static readonly GUIContent s_programCameraContent = new GUIContent("Program Camera");
        private static readonly GUIContent s_referenceTransformContent = new GUIContent("Reference Transform");
        private static readonly GUIContent s_forwardModeContent = new GUIContent("Forward Mode");
        private static readonly GUIContent s_rigPerformersContent = new GUIContent("Rig Performers");
        private static readonly GUIContent s_usePerformerOverrideContent = new GUIContent("Override Performers");
        private static readonly GUIContent s_performerOverrideContent = new GUIContent("Performers");

        private SerializedProperty _referenceTransform;
        private SerializedProperty _programCamera;
        private SerializedProperty _performers;
        private SerializedProperty _forwardReferenceMode;
        private SerializedProperty _customReference;
        private SerializedProperty _sensorSize;
        private SerializedProperty _gateFit;
        private SerializedProperty _lensShift;
        private SerializedProperty _nearClipPlane;
        private SerializedProperty _farClipPlane;
        private SerializedProperty _distanceScale;
        private SerializedProperty _motionScale;
        private SerializedProperty _verticalMotionScale;
        private SerializedProperty _masterPlaybackSpeed;
        private SerializedProperty _slots;

        private ReorderableList _slotsList;
        private bool _showCameraSettings = true;
        private bool _showFramingSettings;


        // Methods

        private void OnEnable()
        {
            _referenceTransform = serializedObject.FindProperty(nameof(_referenceTransform));
            _programCamera = serializedObject.FindProperty(nameof(_programCamera));
            _performers = serializedObject.FindProperty(nameof(_performers));
            _forwardReferenceMode = serializedObject.FindProperty(nameof(_forwardReferenceMode));
            _customReference = serializedObject.FindProperty(nameof(_customReference));
            _sensorSize = serializedObject.FindProperty(nameof(_sensorSize));
            _gateFit = serializedObject.FindProperty(nameof(_gateFit));
            _lensShift = serializedObject.FindProperty(nameof(_lensShift));
            _nearClipPlane = serializedObject.FindProperty(nameof(_nearClipPlane));
            _farClipPlane = serializedObject.FindProperty(nameof(_farClipPlane));
            _distanceScale = serializedObject.FindProperty(nameof(_distanceScale));
            _motionScale = serializedObject.FindProperty(nameof(_motionScale));
            _verticalMotionScale = serializedObject.FindProperty(nameof(_verticalMotionScale));
            _masterPlaybackSpeed = serializedObject.FindProperty(nameof(_masterPlaybackSpeed));
            _slots = serializedObject.FindProperty(nameof(_slots));

            InitializeSlotsList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var rig = (VLiveCameraRig)target;

            DrawSetupSection();
            DrawPerformersSection();
            DrawCameraSettingsSection();
            DrawFramingAndScalingSection();
            DrawShotSlotsSection();
            DrawActionsSection(rig);

            serializedObject.ApplyModifiedProperties();
        }

        private void InitializeSlotsList()
        {
            _slotsList = new ReorderableList(serializedObject, _slots, true, true, true, true)
            {
                drawHeaderCallback = DrawSlotsHeader,
                drawElementCallback = DrawSlotElement,
                elementHeightCallback = GetSlotElementHeight,
                onAddCallback = OnAddSlot,
                onRemoveCallback = OnRemoveSlot
            };
        }

        private void DrawSetupSection()
        {
            EditorGUILayout.PropertyField(_programCamera, s_programCameraContent);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_referenceTransform, s_referenceTransformContent);
            EditorGUILayout.PropertyField(_forwardReferenceMode, s_forwardModeContent);
            if (_forwardReferenceMode.enumValueIndex == (int)ForwardReferenceMode.CustomReference)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_customReference);
                EditorGUI.indentLevel--;
            }

            if (_forwardReferenceMode.enumValueIndex == (int)ForwardReferenceMode.ReferenceForward)
            {
                Transform reference = (Transform)_referenceTransform.objectReferenceValue;
                if (reference != null)
                {
                    Vector3 fwdXZ = Vector3.ProjectOnPlane(reference.forward, Vector3.up);
                    if (fwdXZ.sqrMagnitude < 0.0001f)
                    {
                        EditorGUILayout.HelpBox("Reference forward projected on XZ is near-zero. World +Z will be used.", MessageType.Warning);
                    }
                }
            }

            if (_forwardReferenceMode.enumValueIndex == (int)ForwardReferenceMode.CustomReference)
            {
                if (_customReference.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Custom Reference is unassigned. Assign a reference Transform before running Apply / Sync.", MessageType.Error);
                }
                else
                {
                    var customTransform = (Transform)_customReference.objectReferenceValue;
                    Vector3 fwdXZ = Vector3.ProjectOnPlane(customTransform.forward, Vector3.up);
                    if (fwdXZ.sqrMagnitude < 0.0001f)
                    {
                        EditorGUILayout.HelpBox("Custom Reference forward is pointing vertically. Horizontal forward direction cannot be determined.", MessageType.Error);
                    }
                }
            }
        }

        private void DrawPerformersSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_performers, s_rigPerformersContent, true);
        }

        private void DrawCameraSettingsSection()
        {
            _showCameraSettings = EditorGUILayout.Foldout(_showCameraSettings, "Common Physical Camera Settings", true);
            if (!_showCameraSettings)
            {
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_sensorSize);
            EditorGUILayout.PropertyField(_gateFit);
            EditorGUILayout.PropertyField(_lensShift);
            EditorGUILayout.PropertyField(_nearClipPlane);
            EditorGUILayout.PropertyField(_farClipPlane);

            if (_nearClipPlane.floatValue <= 0f || _farClipPlane.floatValue <= _nearClipPlane.floatValue)
            {
                EditorGUILayout.HelpBox("Far Clip Plane must be greater than Near Clip Plane.", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawFramingAndScalingSection()
        {
            _showFramingSettings = EditorGUILayout.Foldout(_showFramingSettings, "Motion Settings", true);
            if (!_showFramingSettings)
            {
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_distanceScale);
            EditorGUILayout.PropertyField(_motionScale, s_motionScaleContent);
            EditorGUILayout.PropertyField(_verticalMotionScale);
            EditorGUILayout.PropertyField(_masterPlaybackSpeed);

            if (_distanceScale.floatValue <= 0f || _motionScale.floatValue <= 0f || _verticalMotionScale.floatValue < 0f)
            {
                EditorGUILayout.HelpBox("Distance and horizontal motion scales must be greater than zero. Vertical motion scale cannot be negative.", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawShotSlotsSection()
        {
            EditorGUILayout.Space();

            _slotsList.draggable = !Application.isPlaying;
            _slotsList.displayAdd = !Application.isPlaying;
            _slotsList.displayRemove = !Application.isPlaying;

            _slotsList.DoLayoutList();

            // Outdated preset summary check
            bool anyMismatch = false;
            for (int i = 0; i < _slots.arraySize; i++)
            {
                SerializedProperty slotElem = _slots.GetArrayElementAtIndex(i);
                var preset = (VLiveCameraMotionPreset)slotElem.FindPropertyRelative("_preset").objectReferenceValue;
                var shot = (VLiveCameraShot)slotElem.FindPropertyRelative("_shot").objectReferenceValue;
                if (shot != null && preset != null && shot.AppliedPreset != null && shot.AppliedPreset != preset)
                {
                    anyMismatch = true;
                    break;
                }
            }

            if (anyMismatch)
            {
                EditorGUILayout.HelpBox("One or more slots have an updated Preset. Click 'Rebuild All' or rebuild the individual Shot to apply changes.", MessageType.Warning);
            }
        }

        private void DrawSlotsHeader(Rect rect)
        {
            int count = _slots.arraySize;
            EditorGUI.LabelField(rect, $"Shot Slots  ({count})", EditorStyles.boldLabel);
        }

        private float GetSlotElementHeight(int index)
        {
            if (index < 0 || index >= _slots.arraySize)
            {
                return EditorGUIUtility.singleLineHeight + 4f;
            }

            SerializedProperty slot = _slots.GetArrayElementAtIndex(index);
            SerializedProperty useOverride = slot.FindPropertyRelative("_usePerformerOverride");
            SerializedProperty performers = slot.FindPropertyRelative("_performers");
            SerializedProperty version = slot.FindPropertyRelative("_performerAssignmentVersion");
            bool hasLegacyOverride = version.intValue == 0 && performers.arraySize > 0;
            bool showOverride = useOverride.boolValue || hasLegacyOverride;
            float height = (EditorGUIUtility.singleLineHeight * 2f) + 10f;
            if (showOverride)
            {
                height += EditorGUI.GetPropertyHeight(performers, true) + 4f;
            }

            return height;
        }

        private void DrawSlotElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= _slots.arraySize)
            {
                return;
            }

            SerializedProperty slotElem = _slots.GetArrayElementAtIndex(index);
            SerializedProperty presetProp = slotElem.FindPropertyRelative("_preset");
            SerializedProperty useOverrideProp = slotElem.FindPropertyRelative("_usePerformerOverride");
            SerializedProperty performersProp = slotElem.FindPropertyRelative("_performers");
            SerializedProperty performerAssignmentVersionProp = slotElem.FindPropertyRelative("_performerAssignmentVersion");
            SerializedProperty shotProp = slotElem.FindPropertyRelative("_shot");

            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;

            float indexWidth = 24f;
            float arrowWidth = 16f;
            float spacing = 4f;
            float contentWidth = rect.width - indexWidth - arrowWidth - (spacing * 3f);
            float presetWidth = contentWidth * 0.48f;
            float shotWidth = contentWidth * 0.52f;

            Rect indexRect = new Rect(rect.x, rect.y, indexWidth, rect.height);
            Rect presetRect = new Rect(indexRect.xMax + spacing, rect.y, presetWidth, rect.height);
            Rect arrowRect = new Rect(presetRect.xMax + spacing, rect.y, arrowWidth, rect.height);
            Rect shotRect = new Rect(arrowRect.xMax + spacing, rect.y, shotWidth, rect.height);

            string slotLabel = $"{index + 1}";
            string tooltip = index < 9 ? $"Slot {index + 1} (Direct Cut Key: {index + 1})" : $"Slot {index + 1}";
            EditorGUI.LabelField(indexRect, new GUIContent(slotLabel, tooltip), EditorStyles.miniBoldLabel);

            EditorGUI.PropertyField(presetRect, presetProp, GUIContent.none);

            var shotObj = (VLiveCameraShot)shotProp.objectReferenceValue;
            var presetObj = (VLiveCameraMotionPreset)presetProp.objectReferenceValue;
            bool hasMismatch = shotObj != null && presetObj != null && shotObj.AppliedPreset != null && shotObj.AppliedPreset != presetObj;

            if (hasMismatch)
            {
                GUI.Label(arrowRect, s_warningContent, EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                GUI.Label(arrowRect, s_arrowContent, EditorStyles.centeredGreyMiniLabel);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(shotRect, shotProp, GUIContent.none);
            }

            bool hasLegacyOverride = performerAssignmentVersionProp.intValue == 0 && performersProp.arraySize > 0;
            bool useOverride = useOverrideProp.boolValue || hasLegacyOverride;
            Rect overrideRect = new Rect(
                presetRect.x,
                rect.y + EditorGUIUtility.singleLineHeight + 4f,
                rect.xMax - presetRect.x,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();
            useOverride = EditorGUI.ToggleLeft(overrideRect, s_usePerformerOverrideContent, useOverride);
            if (EditorGUI.EndChangeCheck())
            {
                useOverrideProp.boolValue = useOverride;
                performerAssignmentVersionProp.intValue = 1;
            }

            if (useOverride)
            {
                Rect performersRect = new Rect(
                    presetRect.x,
                    overrideRect.yMax + 4f,
                    rect.xMax - presetRect.x,
                    EditorGUI.GetPropertyHeight(performersProp, true));
                EditorGUI.PropertyField(performersRect, performersProp, s_performerOverrideContent, true);
            }
        }

        private void OnAddSlot(ReorderableList list)
        {
            int newIndex = _slots.arraySize;
            _slots.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newElem = _slots.GetArrayElementAtIndex(newIndex);
            newElem.FindPropertyRelative("_preset").objectReferenceValue = null;
            newElem.FindPropertyRelative("_usePerformerOverride").boolValue = false;
            newElem.FindPropertyRelative("_performers").arraySize = 0;
            newElem.FindPropertyRelative("_performerAssignmentVersion").intValue = 1;
            newElem.FindPropertyRelative("_shot").objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
        }

        private void OnRemoveSlot(ReorderableList list)
        {
            int targetIndex = (list.index >= 0 && list.index < _slots.arraySize)
                ? list.index
                : _slots.arraySize - 1;

            if (targetIndex >= 0 && targetIndex < _slots.arraySize)
            {
                int prevCount = _slots.arraySize;
                _slots.DeleteArrayElementAtIndex(targetIndex);
                if (_slots.arraySize == prevCount)
                {
                    _slots.DeleteArrayElementAtIndex(targetIndex);
                }

                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawActionsSection(VLiveCameraRig rig)
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Rig generation, sync, and rebuild operations are disabled in Play Mode.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            using (new GUILayout.HorizontalScope())
            {
                bool isForwardValid = rig.IsForwardReferenceValid();
                bool canApply = isForwardValid;

                using (new EditorGUI.DisabledScope(!canApply))
                {
                    if (GUILayout.Button("Apply", GUILayout.Height(22)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        VLiveCameraRigBuilder.ApplySync(rig);
                        serializedObject.Update();
                        GUIUtility.ExitGUI();
                    }
                }

                bool canRebuildAll = isForwardValid && _slots.arraySize > 0;
                using (new EditorGUI.DisabledScope(!canRebuildAll))
                {
                    if (GUILayout.Button("Rebuild", GUILayout.Height(22)))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Rebuild All Shots From Presets",
                                "Rebuild camera position, lens, aim, spline, and motion configuration for all shots from their presets?\n\nManual adjustments made on scene cameras and splines will be overwritten.",
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

            using (new EditorGUI.DisabledScope(_programCamera.objectReferenceValue == null && rig.SlotCount == 0))
            {
                if (GUILayout.Button("Apply Camera Settings to All Shots", GUILayout.Height(22)))
                {
                    serializedObject.ApplyModifiedProperties();
                    VLiveCameraRigBuilder.ApplyCommonCameraSettings(rig);
                    serializedObject.Update();
                    GUIUtility.ExitGUI();
                }
            }
        }
    }
}
