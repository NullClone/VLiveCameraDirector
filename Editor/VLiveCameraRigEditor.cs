using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraRig用のカスタムインスペクター。
    /// Target設定、正面基準、スケール、順序付きShot Slotsの編集、同期・再構築操作、Preset保存、Motion診断を提供します。
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
        private SerializedProperty _slotsProp;

        private int _selectedSlotForOperation = 0;
        private VLiveCameraMotionValidator.ValidationReport _diagnosticReport;


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
            _slotsProp = serializedObject.FindProperty("_slots");
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var rig = (VLiveCameraRig)target;

            DrawHeader(rig);
            EditorGUILayout.Space(6);

            DrawSetupAndFramingSection(rig);
            EditorGUILayout.Space(6);

            DrawShotSlotsSection(rig);
            EditorGUILayout.Space(6);

            DrawOperationsSection(rig);
            EditorGUILayout.Space(6);

            DrawRuntimeSection(rig);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(VLiveCameraRig rig)
        {
            var rect = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.09f, 0.13f));

            var titleRect = new Rect(rect.x + 12, rect.y + 6, rect.width - 24, 20);
            var subRect = new Rect(rect.x + 12, rect.y + 26, rect.width - 24, 16);

            EditorGUI.LabelField(titleRect, "VLIVE CAMERA RIG", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            });

            string targetName = rig.PerformerTarget != null ? rig.PerformerTarget.name : "None";
            EditorGUI.LabelField(subRect, $"Target: {targetName}  |  Slots: {rig.SlotCount}  |  Mode: {rig.ForwardMode}", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.65f, 0.85f, 1f) }
            });
        }

        private void DrawSetupAndFramingSection(VLiveCameraRig rig)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Setup & Framing (対象・正面・スケール設定)", EditorStyles.boldLabel);

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
            EditorGUILayout.PropertyField(_motionScaleProp);

            // 検証メッセージ
            if (_performerTargetProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Performer Target が未設定です。Apply / Sync には演者Transformの指定が必須です。", MessageType.Warning);
            }
            else if (_forwardReferenceModeProp.enumValueIndex == (int)ForwardReferenceMode.TargetForward)
            {
                var targetTransform = (Transform)_performerTargetProp.objectReferenceValue;
                if (targetTransform != null)
                {
                    Vector3 fwdXZ = Vector3.ProjectOnPlane(targetTransform.forward, Vector3.up);
                    if (fwdXZ.sqrMagnitude < 0.0001f)
                    {
                        EditorGUILayout.HelpBox("Target の forward を XZ 平面に射影した長さがほぼ 0 です。正面方向として World +Z を代用します。", MessageType.Warning);
                    }
                }
            }

            if (_forwardReferenceModeProp.enumValueIndex == (int)ForwardReferenceMode.CustomReference)
            {
                if (_customReferenceProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Custom Reference が未設定です。CustomReference モードでは参照 Transform の指定が必須です（Apply / Rebuild は無効化されます）。", MessageType.Error);
                }
                else
                {
                    var customTransform = (Transform)_customReferenceProp.objectReferenceValue;
                    Vector3 fwdXZ = Vector3.ProjectOnPlane(customTransform.forward, Vector3.up);
                    if (fwdXZ.sqrMagnitude < 0.0001f)
                    {
                        EditorGUILayout.HelpBox("Custom Reference の forward が垂直方向（真上または真下）を向いているため、水平正面方向を決定できません（Apply / Rebuild は無効化されます）。", MessageType.Error);
                    }
                }
            }

            if (_distanceScaleProp.floatValue <= 0f || _motionScaleProp.floatValue <= 0f)
            {
                EditorGUILayout.HelpBox("Distance Scale および Motion Scale は 0 より大きい値を指定してください。", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawShotSlotsSection(VLiveCameraRig rig)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Shot Slots (ショットスロット構成)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Rigが所有するショット構成の正本です。スロット順がショット番号（キー1〜N）となります。", MessageType.None);

            int slotCount = _slotsProp.arraySize;

            for (int i = 0; i < slotCount; i++)
            {
                SerializedProperty slotElem = _slotsProp.GetArrayElementAtIndex(i);
                SerializedProperty presetProp = slotElem.FindPropertyRelative("_preset");
                SerializedProperty shotProp = slotElem.FindPropertyRelative("_shot");

                EditorGUILayout.BeginVertical("helpBox");

                // スロットヘッダー（移動・除外ボタン）
                EditorGUILayout.BeginHorizontal();
                string slotTitle = $"Slot {i + 1} (Key {i + 1})";
                EditorGUILayout.LabelField(slotTitle, EditorStyles.boldLabel, GUILayout.Width(110));

                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(i == 0 || Application.isPlaying);
                if (GUILayout.Button("▲", GUILayout.Width(22), GUILayout.Height(18)))
                {
                    _slotsProp.MoveArrayElement(i, i - 1);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }

                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(i == slotCount - 1 || Application.isPlaying);
                if (GUILayout.Button("▼", GUILayout.Width(22), GUILayout.Height(18)))
                {
                    _slotsProp.MoveArrayElement(i, i + 1);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }

                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
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

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                // PresetとShot参照
                EditorGUILayout.PropertyField(presetProp, new GUIContent("Preset"));

                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(shotProp, new GUIContent("Shot"));
                }

                var shotObj = (VLiveCameraShot)shotProp.objectReferenceValue;
                var presetObj = (VLiveCameraMotionPreset)presetProp.objectReferenceValue;

                if (shotObj != null)
                {
                    bool canRebuildSingle = !Application.isPlaying && _performerTargetProp.objectReferenceValue != null && rig.IsForwardReferenceValid();
                    using (new EditorGUI.DisabledScope(!canRebuildSingle))
                    {
                        if (GUILayout.Button("Rebuild", GUILayout.Width(62), GUILayout.Height(18)))
                        {
                            if (EditorUtility.DisplayDialog(
                                    "Rebuild Shot From Preset",
                                    $"Slot {i + 1} のカメラ位置、Lens、Aim、Spline、移動設定を Preset 初期値から再構築します。\n手動調整は上書きされます。続行しますか？",
                                    "Rebuild",
                                    "Cancel"))
                            {
                                serializedObject.ApplyModifiedProperties();
                                VLiveCameraRigBuilder.RebuildShotFromPreset(rig, i);
                                serializedObject.Update();
                                GUIUtility.ExitGUI();
                            }
                        }
                    }

                    using (new EditorGUI.DisabledScope(Application.isPlaying))
                    {
                        if (GUILayout.Button("Delete", GUILayout.Width(52), GUILayout.Height(18)))
                        {
                            string shotName = shotObj != null ? shotObj.name : $"Shot {i + 1}";
                            if (EditorUtility.DisplayDialog(
                                    "Delete Shot GameObject",
                                    $"Shot GameObject '{shotName}' および Spline、Aim Proxy を Scene から削除しますか？\n（Undo 可能です）",
                                    "Delete",
                                    "Cancel"))
                            {
                                serializedObject.ApplyModifiedProperties();
                                VLiveCameraRigBuilder.DeleteShotGameObject(rig, i);
                                serializedObject.Update();
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();

                // Preset参照とShot適用済み設定の相違警告
                if (shotObj != null && presetObj != null && shotObj.AppliedPreset != null && shotObj.AppliedPreset != presetObj)
                {
                    EditorGUILayout.HelpBox($"⚠️ SlotのPreset参照とShotの適用済み設定が異なります（適用中: {shotObj.AppliedPreset.DisplayName}）。\n変更を反映するには Rebuild を実行してください。", MessageType.Warning);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            if (GUILayout.Button("+ Add Shot Slot", GUILayout.Height(24)))
            {
                _slotsProp.InsertArrayElementAtIndex(_slotsProp.arraySize);
                SerializedProperty newElem = _slotsProp.GetArrayElementAtIndex(_slotsProp.arraySize - 1);
                newElem.FindPropertyRelative("_preset").objectReferenceValue = null;
                newElem.FindPropertyRelative("_shot").objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawOperationsSection(VLiveCameraRig rig)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Operations (リグ操作)", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode 中は Rig の生成・同期・再構築・削除は無効化されます。", MessageType.Info);
            }

            bool hasTarget = _performerTargetProp.objectReferenceValue != null;
            bool isForwardValid = rig.IsForwardReferenceValid();
            bool canApply = !Application.isPlaying && hasTarget && isForwardValid;

            EditorGUI.BeginDisabledGroup(!canApply);
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.35f);
            if (GUILayout.Button("Apply / Sync", GUILayout.Height(34)))
            {
                serializedObject.ApplyModifiedProperties();
                VLiveCameraRigBuilder.ApplySync(rig);
                serializedObject.Update();
                GUIUtility.ExitGUI();
            }

            GUI.backgroundColor = prevColor;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);

            bool canRebuild = !Application.isPlaying && hasTarget && isForwardValid && _slotsProp.arraySize > 0;

            // Rebuild Selected & Rebuild All
            int slotCount = rig.SlotCount;
            if (slotCount > 0)
            {
                var slotNames = new string[slotCount];
                for (int s = 0; s < slotCount; s++)
                {
                    var sl = rig.Slots[s];
                    string sName = (sl != null && sl.Shot != null) ? sl.Shot.ShotName : (sl != null && sl.Preset != null ? sl.Preset.DisplayName : "None");
                    slotNames[s] = $"Slot {s + 1}: {sName}";
                }

                _selectedSlotForOperation = Mathf.Clamp(_selectedSlotForOperation, 0, slotCount - 1);
                _selectedSlotForOperation = EditorGUILayout.Popup("Target Slot (対象スロット):", _selectedSlotForOperation, slotNames);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginDisabledGroup(!canRebuild);
                    if (GUILayout.Button($"Rebuild Slot {_selectedSlotForOperation + 1} From Preset", GUILayout.Height(24)))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Rebuild Selected Shot From Preset",
                                $"Slot {_selectedSlotForOperation + 1} のカメラ位置、Lens、Aim、Spline、移動設定を現在の Rig 設定と Preset 初期値から再構築します。\n手動で行った調整は上書きされます。続行しますか？",
                                "Rebuild",
                                "Cancel"))
                        {
                            serializedObject.ApplyModifiedProperties();
                            VLiveCameraRigBuilder.RebuildShotFromPreset(rig, _selectedSlotForOperation);
                            serializedObject.Update();
                            GUIUtility.ExitGUI();
                        }
                    }

                    if (GUILayout.Button("Rebuild All From Presets", GUILayout.Height(24)))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Rebuild All Shots From Presets",
                                "すべての Shot のカメラ位置、Lens、Aim、Spline、移動設定を現在の Rig 設定と Preset 初期値から再構築します。\n手動で行った調整は上書きされます。続行しますか？",
                                "Rebuild All",
                                "Cancel"))
                        {
                            serializedObject.ApplyModifiedProperties();
                            VLiveCameraRigBuilder.RebuildAllShotsFromPreset(rig);
                            serializedObject.Update();
                            GUIUtility.ExitGUI();
                        }
                    }

                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.Space(4);

                // Save Shot As New Preset
                var selectedSlot = rig.Slots[_selectedSlotForOperation];
                bool canBake = selectedSlot != null && selectedSlot.Shot != null;
                EditorGUI.BeginDisabledGroup(!canBake);
                if (GUILayout.Button($"Save Slot {_selectedSlotForOperation + 1} As New Preset (新規Preset保存)", GUILayout.Height(24)))
                {
                    VLiveCameraPresetBaker.BakePresetFromShot(rig, selectedSlot.Shot);
                }

                EditorGUI.EndDisabledGroup();

                // Motion Diagnostics
                EditorGUILayout.Space(4);
                if (GUILayout.Button($"Run Diagnostics on Slot {_selectedSlotForOperation + 1} (運動診断)", GUILayout.Height(24)))
                {
                    if (selectedSlot != null && selectedSlot.Shot != null)
                    {
                        _diagnosticReport = VLiveCameraMotionValidator.ValidateShot(selectedSlot.Shot);
                    }
                    else if (selectedSlot != null && selectedSlot.Preset != null)
                    {
                        _diagnosticReport = VLiveCameraMotionValidator.ValidatePreset(selectedSlot.Preset);
                    }
                }

                if (_diagnosticReport != null)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.BeginVertical("helpBox");
                    EditorGUILayout.LabelField("Motion Diagnostics Report:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Duration: {_diagnosticReport.Duration:F2}s  |  Spline Length: {_diagnosticReport.SplineLength:F2}m", EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField($"Peak Speed: {_diagnosticReport.MaxSpeed:F2} m/s  |  Peak Acc: {_diagnosticReport.MaxAcceleration:F2} m/s²  |  Peak Jerk: {_diagnosticReport.MaxJerk:F2} m/s³", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"FOV Range: {_diagnosticReport.MinFieldOfView:F1}° 〜 {_diagnosticReport.MaxFieldOfView:F1}°", EditorStyles.miniLabel);

                    EditorGUILayout.Space(2);
                    foreach (var msg in _diagnosticReport.Messages)
                    {
                        MessageType msgType = msg.Severity switch
                        {
                            VLiveCameraMotionValidator.DiagnosticSeverity.Error => MessageType.Error,
                            VLiveCameraMotionValidator.DiagnosticSeverity.Warning => MessageType.Warning,
                            _ => MessageType.Info
                        };

                        EditorGUILayout.HelpBox($"[{msg.Category}] {msg.Message}", msgType);
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "・ Apply / Sync: 不足Shot、Aim Proxy、Splineの生成と参照・順序の修復を行います（手動調整は維持されます）。\n" +
                "・ Rebuild: 手動調整を上書きし、Preset初期値と現在のRigスケールから再構築します。\n" +
                "・ Save As New Preset: 調整済みShotから完全なKnot、Tangent、Trackを持つ新しいPreset Assetを作成します。\n" +
                "・ ✕ボタン: Slotから外す操作（GameObjectは削除されません）。\n" +
                "・ Deleteボタン: 生成済みGameObject、Spline、Aim ProxyをSceneから明示的に削除します。",
                MessageType.None
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeSection(VLiveCameraRig rig)
        {
            var switcher = rig.GetComponent<VLiveCameraSwitcher>();
            if (switcher == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Program Monitor & Control (Live制御)", EditorStyles.boldLabel);

            string liveShotName = switcher.CurrentProgramShot != null ? switcher.CurrentProgramShot.ShotName : "未選択";
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Current Program:", GUILayout.Width(110));
                var style = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = switcher.CurrentProgramShot != null ? new Color(1f, 0.35f, 0.35f) : Color.gray }
                };
                EditorGUILayout.LabelField(liveShotName, style);
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Direct Cut (ショット切り替え):", EditorStyles.miniBoldLabel);

                int slotCount = rig.SlotCount;
                int columns = 3;
                for (int i = 0; i < slotCount; i += columns)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int col = 0; col < columns; col++)
                        {
                            int index = i + col;
                            if (index < slotCount)
                            {
                                var slot = rig.Slots[index];
                                string label = (slot != null && slot.Shot != null) ? $"{index + 1}: {slot.Shot.ShotName}" : $"{index + 1}: (None)";
                                bool isCurrent = slot != null && slot.Shot != null && slot.Shot == switcher.CurrentProgramShot;

                                var prevBg = GUI.backgroundColor;
                                if (isCurrent)
                                {
                                    GUI.backgroundColor = new Color(0.9f, 0.25f, 0.25f);
                                }

                                if (GUILayout.Button(label, GUILayout.Height(26)))
                                {
                                    switcher.CutToShot(index + 1);
                                }

                                GUI.backgroundColor = prevBg;
                            }
                        }
                    }
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Motion Control (現在のLive Shot操作):", EditorStyles.miniBoldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Speed -"))
                    {
                        switcher.SpeedDown();
                    }

                    if (GUILayout.Button("Speed +"))
                    {
                        switcher.SpeedUp();
                    }

                    if (GUILayout.Button("Reverse"))
                    {
                        switcher.Reverse();
                    }

                    if (switcher.CurrentProgramShot != null && switcher.CurrentProgramShot.IsHolding)
                    {
                        if (GUILayout.Button("Resume"))
                        {
                            switcher.Resume();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Hold"))
                        {
                            switcher.Hold();
                        }
                    }

                    if (GUILayout.Button("Freeze"))
                    {
                        switcher.Freeze();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Play Mode 中に直接Cutボタンおよび手動操作ボタンが表示されます。", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
