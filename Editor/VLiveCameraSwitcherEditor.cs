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

            DrawHeader(switcher);
            EditorGUILayout.Space(6);

            DrawRigSection(switcher);
            EditorGUILayout.Space(6);

            DrawShotsSection(switcher);
            EditorGUILayout.Space(6);

            DrawRuntimeSection(switcher);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(VLiveCameraSwitcher switcher)
        {
            var rect = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.09f, 0.13f));

            var titleRect = new Rect(rect.x + 12, rect.y + 6, rect.width - 24, 20);
            var subRect = new Rect(rect.x + 12, rect.y + 26, rect.width - 24, 16);

            EditorGUI.LabelField(titleRect, "VLIVE CAMERA SWITCHER", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            });

            string programName = switcher.CurrentProgramShot != null ? switcher.CurrentProgramShot.ShotName : "None";
            EditorGUI.LabelField(subRect, $"PROGRAM: {programName}  |  Shots: {switcher.ShotCount}", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.65f, 0.85f, 1f) }
            });
        }

        private void DrawRigSection(VLiveCameraSwitcher switcher)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Target Rig (参照リグ)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_rigProp);

            if (_rigProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("VLiveCameraRig が未設定です。Shot構成およびProgram出力はRigから取得されます。", MessageType.Warning);
            }
            else if (switcher.Rig != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Program Camera", switcher.Rig.ProgramCamera, typeof(UnityEngine.Camera), true);
                    EditorGUILayout.ObjectField("Cinemachine Brain", switcher.CinemachineBrain, typeof(Unity.Cinemachine.CinemachineBrain), true);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawShotsSection(VLiveCameraSwitcher switcher)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Registered Shots (Rigのショット構成)", EditorStyles.boldLabel);

            if (switcher.Rig == null || switcher.Rig.Slots == null || switcher.Rig.Slots.Count == 0)
            {
                EditorGUILayout.HelpBox("ショットが登録されていません。VLiveCameraRig の Inspector で Shot Slots を設定してください。", MessageType.Info);
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

            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeSection(VLiveCameraSwitcher switcher)
        {
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
                                bool isCurrent = shot != null && shot == switcher.CurrentProgramShot;

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
