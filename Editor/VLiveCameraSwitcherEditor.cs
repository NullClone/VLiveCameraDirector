using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraSwitcher用のカスタムインスペクター。
    /// ショット一覧の確認・検証および実行中のProgram切り替えと手動操作を提供します。
    /// </summary>
    [CustomEditor(typeof(VLiveCameraSwitcher))]
    public class VLiveCameraSwitcherEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _cinemachineBrainProp;
        private SerializedProperty _shotsProp;


        // Methods

        private void OnEnable()
        {
            _cinemachineBrainProp = serializedObject.FindProperty("_cinemachineBrain");
            _shotsProp = serializedObject.FindProperty("_shots");
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

            DrawOutputSection();
            EditorGUILayout.Space(6);

            DrawShotsSection();
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

        private void DrawOutputSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Cinemachine Output (出力設定)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_cinemachineBrainProp);

            if (_cinemachineBrainProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("CinemachineBrain が未設定です。Program映像出力が行えません。", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawShotsSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Registered Shots (登録ショット一覧)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_shotsProp, true);

            if (_shotsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("ショットが登録されていません。'Tools/VLive Camera/VLive Camera Setup' から設定してください。", MessageType.Info);
            }
            else
            {
                bool hasNull = false;
                for (int i = 0; i < _shotsProp.arraySize; i++)
                {
                    if (_shotsProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        hasNull = true;
                        break;
                    }
                }

                if (hasNull)
                {
                    EditorGUILayout.HelpBox("ショット一覧に未割り当てのスロットが含まれています。", MessageType.Warning);
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
                                var shot = switcher.Shots[index];
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
