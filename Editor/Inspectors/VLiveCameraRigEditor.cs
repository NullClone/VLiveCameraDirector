using UnityEditor;
using UnityEditorInternal;
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

        private static readonly GUIContent s_arrowContent = new GUIContent("→");
        private static readonly GUIContent s_warningContent = new GUIContent("⚠", "Slot Preset differs from Applied Preset on Shot. Rebuild Shot to apply.");
        private static readonly GUIContent s_motionScaleContent = new GUIContent("Horizontal Motion Scale");

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

        private ReorderableList _slotsList;
        private bool _showFramingSettings;


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

            InitializeSlotsList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var rig = (VLiveCameraRig)target;

            DrawSetupSection();
            DrawFramingAndScalingSection();
            DrawShotSlotsSection();
            DrawActionsSection(rig);

            serializedObject.ApplyModifiedProperties();
        }

        private void InitializeSlotsList()
        {
            _slotsList = new ReorderableList(serializedObject, _slotsProp, true, true, true, true)
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
            EditorGUILayout.PropertyField(_programCameraProp, new GUIContent("Camera"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_performerTargetProp, new GUIContent("Performer Target"));
            EditorGUILayout.PropertyField(_forwardReferenceModeProp, new GUIContent("Forward Mode"));
            if (_forwardReferenceModeProp.enumValueIndex == (int)ForwardReferenceMode.CustomReference)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_customReferenceProp);
                EditorGUI.indentLevel--;
            }

            // Actionable validation messages
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
        }

        private void DrawFramingAndScalingSection()
        {
            _showFramingSettings = EditorGUILayout.Foldout(_showFramingSettings, "Motion Settings", true);
            if (!_showFramingSettings)
            {
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_targetHeightProp);
            EditorGUILayout.PropertyField(_distanceScaleProp);
            EditorGUILayout.PropertyField(_motionScaleProp, s_motionScaleContent);
            EditorGUILayout.PropertyField(_verticalMotionScaleProp);
            EditorGUILayout.PropertyField(_masterPlaybackSpeedProp);

            if (_distanceScaleProp.floatValue <= 0f || _motionScaleProp.floatValue <= 0f || _verticalMotionScaleProp.floatValue < 0f)
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
            for (int i = 0; i < _slotsProp.arraySize; i++)
            {
                SerializedProperty slotElem = _slotsProp.GetArrayElementAtIndex(i);
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
            int count = _slotsProp.arraySize;
            EditorGUI.LabelField(rect, $"Shot Slots  ({count})", EditorStyles.boldLabel);
        }

        private float GetSlotElementHeight(int index)
        {
            return EditorGUIUtility.singleLineHeight + 4f;
        }

        private void DrawSlotElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= _slotsProp.arraySize)
            {
                return;
            }

            SerializedProperty slotElem = _slotsProp.GetArrayElementAtIndex(index);
            SerializedProperty presetProp = slotElem.FindPropertyRelative("_preset");
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
        }

        private void OnAddSlot(ReorderableList list)
        {
            int newIndex = _slotsProp.arraySize;
            _slotsProp.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newElem = _slotsProp.GetArrayElementAtIndex(newIndex);
            newElem.FindPropertyRelative("_preset").objectReferenceValue = null;
            newElem.FindPropertyRelative("_shot").objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
        }

        private void OnRemoveSlot(ReorderableList list)
        {
            int targetIndex = (list.index >= 0 && list.index < _slotsProp.arraySize)
                ? list.index
                : _slotsProp.arraySize - 1;

            if (targetIndex >= 0 && targetIndex < _slotsProp.arraySize)
            {
                int prevCount = _slotsProp.arraySize;
                _slotsProp.DeleteArrayElementAtIndex(targetIndex);
                if (_slotsProp.arraySize == prevCount)
                {
                    _slotsProp.DeleteArrayElementAtIndex(targetIndex);
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
                bool hasTarget = _performerTargetProp.objectReferenceValue != null;
                bool isForwardValid = rig.IsForwardReferenceValid();
                bool canApply = hasTarget && isForwardValid;


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

                bool canRebuildAll = hasTarget && isForwardValid && _slotsProp.arraySize > 0;
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
        }
    }
}
