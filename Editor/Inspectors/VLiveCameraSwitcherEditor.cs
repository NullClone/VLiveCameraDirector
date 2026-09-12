using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraSwitcher用のカスタムインスペクター。
    /// Rig参照の確認および実行中のProgram切り替えと手動操作を提供します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraSwitcher))]
    public class VLiveCameraSwitcherEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _rigProp;


        // Methods

        private void OnEnable()
        {
            _rigProp = serializedObject.FindProperty("_rig");
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var switcher = (VLiveCameraSwitcher)target;

            DrawRigSection(switcher);
            EditorGUILayout.Space(8);

            DrawShotsSection(switcher);
            EditorGUILayout.Space(8);

            DrawProgramMonitorSection(switcher);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRigSection(VLiveCameraSwitcher switcher)
        {
            EditorGUILayout.LabelField("Target Rig", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_rigProp);

            if (_rigProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("VLiveCameraRig is unassigned. Shot slots and Program output are resolved from the Rig.", MessageType.Warning);
            }
            else if (switcher.Rig != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Program Camera", switcher.Rig.ProgramCamera, typeof(UnityEngine.Camera), true);
                    EditorGUILayout.ObjectField("Cinemachine Brain", switcher.CinemachineBrain, typeof(CinemachineBrain), true);
                }
            }
        }

        private void DrawShotsSection(VLiveCameraSwitcher switcher)
        {
            EditorGUILayout.LabelField("Registered Shots", EditorStyles.boldLabel);

            if (switcher.Rig == null || switcher.Rig.Slots == null || switcher.Rig.Slots.Count == 0)
            {
                EditorGUILayout.HelpBox("No shots registered. Configure Shot Slots in the VLiveCameraRig Inspector.", MessageType.Info);
            }
            else
            {
                int count = switcher.Rig.Slots.Count;
                for (int i = 0; i < count; i++)
                {
                    var slot = switcher.Rig.Slots[i];
                    string presetName = (slot != null && slot.Preset != null) ? slot.Preset.DisplayName : "(No Preset)";
                    string shotName = (slot != null && slot.Shot != null) ? slot.Shot.ShotName : "(No Shot)";

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Shot {i + 1} (Key {i + 1})", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"[{presetName}]  ->  {shotName}");
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private void DrawProgramMonitorSection(VLiveCameraSwitcher switcher)
        {
            EditorGUILayout.LabelField("Program Monitor", EditorStyles.boldLabel);

            string liveShotName = switcher.CurrentProgramShot != null ? switcher.CurrentProgramShot.ShotName : "None";
            EditorGUILayout.LabelField("Current Program", liveShotName);

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Direct Cut", EditorStyles.miniBoldLabel);

                int shotCount = switcher.ShotCount;
                int columns = 3;
                for (int i = 0; i < shotCount; i += columns)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int col = 0; col < columns; col++)
                        {
                            int index = i + col;
                            if (index < shotCount)
                            {
                                var slot = switcher.Slots != null && index < switcher.Slots.Count ? switcher.Slots[index] : null;
                                var shot = slot != null ? slot.Shot : null;
                                string label = shot != null ? $"{index + 1}: {shot.ShotName}" : $"{index + 1}: (Null)";

                                if (GUILayout.Button(label, GUILayout.Height(24)))
                                {
                                    switcher.CutToShot(index + 1);
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Motion Control", EditorStyles.miniBoldLabel);

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
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Direct Cut and motion control buttons are active during Play Mode.", MessageType.None);
            }
        }
    }
}
