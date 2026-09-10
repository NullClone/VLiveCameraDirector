using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraUnitのRig構築およびショット構成のセットアップを行うエディタウィンドウ。
    /// </summary>
    public class VLiveCameraSetupWindow : EditorWindow
    {
        // Fields

        private const string RigGameObjectName = "VLiveCameraRig";
        private const string ShotsContainerName = "Shots";
        private const string SplinesContainerName = "Splines";

        [SerializeField]
        private Transform _performerTarget;

        [SerializeField]
        private UnityEngine.Camera _outputCamera;

        [SerializeField]
        private List<VLiveCameraMotionPreset> _palettePresets = new List<VLiveCameraMotionPreset>();

        private Vector2 _scrollPosition;


        // Methods

        /// <summary>
        /// Setup Windowを開きます。
        /// </summary>
        [MenuItem("Tools/VLive Camera/VLive Camera Setup", priority = 10)]
        public static void OpenWindow()
        {
            var window = GetWindow<VLiveCameraSetupWindow>("VLive Camera Setup");
            window.minSize = new Vector2(400f, 500f);
            window.Show();
        }

        private void OnEnable()
        {
            AutoDetectSceneState();

            if (_palettePresets == null || _palettePresets.Count == 0)
            {
                LoadDefaultPalette();
            }
        }

        private void AutoDetectSceneState()
        {
            // 既存RigからTarget、Camera、登録Presetを検出
            var switcher = FindFirstObjectByType<VLiveCameraSwitcher>(FindObjectsInactive.Include);
            if (switcher != null)
            {
                if (_outputCamera == null && switcher.CinemachineBrain != null)
                {
                    _outputCamera = switcher.CinemachineBrain.GetComponent<UnityEngine.Camera>();
                }

                if (_performerTarget == null && switcher.Shots != null && switcher.Shots.Count > 0)
                {
                    foreach (var s in switcher.Shots)
                    {
                        if (s != null && s.CinemachineCamera != null && s.CinemachineCamera.Target.TrackingTarget != null)
                        {
                            _performerTarget = s.CinemachineCamera.Target.TrackingTarget;
                            break;
                        }
                    }
                }

                // 既存RigのShotsからPreset一覧と順序を復元
                if ((_palettePresets == null || _palettePresets.Count == 0) && switcher.Shots != null && switcher.Shots.Count > 0)
                {
                    _palettePresets = new List<VLiveCameraMotionPreset>();
                    foreach (var s in switcher.Shots)
                    {
                        if (s != null && s.Preset != null)
                        {
                            _palettePresets.Add(s.Preset);
                        }
                    }
                }
            }
        }

        private void LoadDefaultPalette()
        {
            _palettePresets = new List<VLiveCameraMotionPreset>();

            string[] defaultPresetNames = new string[]
            {
                "FixedMedium",
                "PushIn",
                "PullOut",
                "TruckLeft",
                "TruckRight",
                "ArcAround"
            };

            foreach (string presetName in defaultPresetNames)
            {
                string path = $"{VLiveCameraPresetAssetCreator.PresetFolderPath}/{presetName}.asset";
                var preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(path);
                if (preset == null)
                {
                    string[] guids = AssetDatabase.FindAssets($"{presetName} t:VLiveCameraMotionPreset");
                    if (guids != null && guids.Length > 0)
                    {
                        preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    }
                }

                if (preset != null)
                {
                    _palettePresets.Add(preset);
                }
            }

            // 見つからない場合は自動生成を試みる
            if (_palettePresets.Count < 6)
            {
                VLiveCameraPresetAssetCreator.CreateOrUpdateDefaultPresets();
                _palettePresets.Clear();
                foreach (string presetName in defaultPresetNames)
                {
                    string path = $"{VLiveCameraPresetAssetCreator.PresetFolderPath}/{presetName}.asset";
                    var preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(path);
                    if (preset == null)
                    {
                        string[] guids = AssetDatabase.FindAssets($"{presetName} t:VLiveCameraMotionPreset");
                        if (guids != null && guids.Length > 0)
                        {
                            preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(AssetDatabase.GUIDToAssetPath(guids[0]));
                        }
                    }

                    if (preset != null)
                    {
                        _palettePresets.Add(preset);
                    }
                }
            }

            // それでも見つからない場合はプロジェクト全体から検索
            if (_palettePresets.Count == 0)
            {
                string[] guids = AssetDatabase.FindAssets("t:VLiveCameraMotionPreset");
                for (int i = 0; i < guids.Length && i < 6; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(path);
                    if (preset != null)
                    {
                        _palettePresets.Add(preset);
                    }
                }
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(6);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawTargetSection();
            EditorGUILayout.Space(8);

            DrawPaletteSection();
            EditorGUILayout.Space(8);

            DrawValidationAndAction();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            var rect = GUILayoutUtility.GetRect(0, 62, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.09f, 0.13f));

            var titleRect = new Rect(rect.x + 12, rect.y + 8, rect.width - 24, 24);
            var subRect = new Rect(rect.x + 12, rect.y + 34, rect.width - 24, 18);

            EditorGUI.LabelField(titleRect, "VLIVE CAMERA SETUP", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                normal = { textColor = Color.white }
            });

            EditorGUI.LabelField(subRect, "1 Target・複数Shot・Motion Palette セットアップ", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.65f, 0.85f, 1f) }
            });
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Target & Output (対象と出力設定)", EditorStyles.boldLabel);

            _performerTarget = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("Performer Target", "カメラワークの注視・追従基準となる演者Transform。必須。"),
                _performerTarget,
                typeof(Transform),
                true
            );

            _outputCamera = (UnityEngine.Camera)EditorGUILayout.ObjectField(
                new GUIContent("Output Camera", "Program映像出力カメラ。空の場合は'Program Camera'を新規作成します。"),
                _outputCamera,
                typeof(UnityEngine.Camera),
                true
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawPaletteSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Motion Palette & Shot Order (ショット構成)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("キー1〜9に対応するShot順序です（初期Palette: 1〜6）。", MessageType.None);

            if (_palettePresets == null)
            {
                _palettePresets = new List<VLiveCameraMotionPreset>();
            }

            int count = _palettePresets.Count;
            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField($"Shot {i + 1} (Key {i + 1})", GUILayout.Width(95));

                _palettePresets[i] = (VLiveCameraMotionPreset)EditorGUILayout.ObjectField(
                    _palettePresets[i],
                    typeof(VLiveCameraMotionPreset),
                    false
                );

                EditorGUI.BeginDisabledGroup(i == 0);
                if (GUILayout.Button("▲", GUILayout.Width(24)))
                {
                    var temp = _palettePresets[i];
                    _palettePresets[i] = _palettePresets[i - 1];
                    _palettePresets[i - 1] = temp;
                    GUIUtility.ExitGUI();
                }

                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(i == count - 1);
                if (GUILayout.Button("▼", GUILayout.Width(24)))
                {
                    var temp = _palettePresets[i];
                    _palettePresets[i] = _palettePresets[i + 1];
                    _palettePresets[i + 1] = temp;
                    GUIUtility.ExitGUI();
                }

                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    _palettePresets.RemoveAt(i);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(_palettePresets.Count >= 9);
            if (GUILayout.Button("+ Add Shot Slot", GUILayout.Height(24)))
            {
                _palettePresets.Add(null);
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Load Default 6 Presets", GUILayout.Height(24)))
            {
                VLiveCameraPresetAssetCreator.CreateOrUpdateDefaultPresets();
                LoadDefaultPalette();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationAndAction()
        {
            bool hasTarget = _performerTarget != null;
            bool hasPresets = _palettePresets != null && _palettePresets.Count > 0;
            bool allPresetsValid = true;

            if (hasPresets)
            {
                foreach (var p in _palettePresets)
                {
                    if (p == null)
                    {
                        allPresetsValid = false;
                        break;
                    }
                }
            }

            if (!hasTarget)
            {
                EditorGUILayout.HelpBox("Performer Target を指定してください。", MessageType.Warning);
            }

            if (!hasPresets)
            {
                EditorGUILayout.HelpBox("少なくとも1つのMotion Presetを指定してください。", MessageType.Warning);
            }
            else if (!allPresetsValid)
            {
                EditorGUILayout.HelpBox("未設定のShotスロットがあります。Presetを割り当てるかスロットを削除してください。", MessageType.Warning);
            }

            // 既存Rigの存在確認（非アクティブも含めて安全に判定）
            GameObject existingRig = null;
            var activeScene = SceneManager.GetActiveScene();
            foreach (var root in activeScene.GetRootGameObjects())
            {
                if (root.name == RigGameObjectName)
                {
                    existingRig = root;
                    break;
                }
            }

            if (existingRig == null)
            {
                var existingSwitcher = FindFirstObjectByType<VLiveCameraSwitcher>(FindObjectsInactive.Include);
                if (existingSwitcher != null)
                {
                    existingRig = existingSwitcher.gameObject;
                }
            }

            if (existingRig != null)
            {
                EditorGUILayout.HelpBox($"既存の '{RigGameObjectName}' を検出しました。'Create / Update Camera Rig' で順序や不足Shotを更新します（ユーザー調整値は保護されます）。", MessageType.Info);
            }

            EditorGUILayout.Space(4);

            bool canExecute = hasTarget && hasPresets && allPresetsValid;
            EditorGUI.BeginDisabledGroup(!canExecute);

            string buttonText = (existingRig != null) ? "Update Camera Rig" : "Create / Update Camera Rig";
            if (GUILayout.Button(buttonText, GUILayout.Height(38)))
            {
                ExecuteSetup();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void ExecuteSetup()
        {
            if (_performerTarget == null || _palettePresets == null || _palettePresets.Count == 0)
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Create / Update Camera Rig");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Root Rig GameObject（非アクティブを含めて重複生成を防止）
            GameObject rigGo = null;
            var currentScene = SceneManager.GetActiveScene();
            foreach (var root in currentScene.GetRootGameObjects())
            {
                if (root.name == RigGameObjectName)
                {
                    rigGo = root;
                    break;
                }
            }

            if (rigGo == null)
            {
                var existingSwitcher = FindFirstObjectByType<VLiveCameraSwitcher>(FindObjectsInactive.Include);
                if (existingSwitcher != null)
                {
                    rigGo = existingSwitcher.gameObject;
                }
            }

            if (rigGo == null)
            {
                rigGo = new GameObject(RigGameObjectName);
                Undo.RegisterCreatedObjectUndo(rigGo, "Create Camera Rig Root");
            }
            else
            {
                Undo.RecordObject(rigGo, "Update Camera Rig Root");
            }

            // 2. Switcher & KeyboardInput
            var switcher = rigGo.GetComponent<VLiveCameraSwitcher>();
            if (switcher == null)
            {
                switcher = Undo.AddComponent<VLiveCameraSwitcher>(rigGo);
            }
            else
            {
                Undo.RecordObject(switcher, "Update Switcher");
            }

            var keyboardInput = rigGo.GetComponent<VLiveCameraKeyboardInput>();
            if (keyboardInput == null)
            {
                keyboardInput = Undo.AddComponent<VLiveCameraKeyboardInput>(rigGo);
            }
            else
            {
                Undo.RecordObject(keyboardInput, "Update KeyboardInput");
            }

            // 3. Program Camera & Brain
            UnityEngine.Camera programCamera = _outputCamera;
            if (programCamera == null)
            {
                Transform camChild = rigGo.transform.Find("Program Camera");
                if (camChild != null)
                {
                    programCamera = camChild.GetComponent<UnityEngine.Camera>();
                }

                if (programCamera == null && switcher.CinemachineBrain != null)
                {
                    programCamera = switcher.CinemachineBrain.GetComponent<UnityEngine.Camera>();
                }

                if (programCamera == null)
                {
                    var camGo = new GameObject("Program Camera");
                    camGo.transform.SetParent(rigGo.transform, false);
                    programCamera = camGo.AddComponent<UnityEngine.Camera>();
                    programCamera.tag = "MainCamera";

                    Undo.RegisterCreatedObjectUndo(camGo, "Create Program Camera");
                }
            }

            CinemachineBrain brain = programCamera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = Undo.AddComponent<CinemachineBrain>(programCamera.gameObject);
            }
            else
            {
                Undo.RecordObject(brain, "Update CinemachineBrain");
            }

            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            EditorUtility.SetDirty(brain);

            // 4. Containers
            Transform shotsContainer = rigGo.transform.Find(ShotsContainerName);
            if (shotsContainer == null)
            {
                var shotsGo = new GameObject(ShotsContainerName);
                shotsGo.transform.SetParent(rigGo.transform, false);
                shotsContainer = shotsGo.transform;
                Undo.RegisterCreatedObjectUndo(shotsGo, "Create Shots Container");
            }

            Transform splinesContainer = rigGo.transform.Find(SplinesContainerName);
            if (splinesContainer == null)
            {
                var splinesGo = new GameObject(SplinesContainerName);
                splinesGo.transform.SetParent(rigGo.transform, false);
                splinesContainer = splinesGo.transform;
                Undo.RegisterCreatedObjectUndo(splinesGo, "Create Splines Container");
            }

            // 5. Build or Update Shots
            VLiveCameraShot[] existingShots = rigGo.GetComponentsInChildren<VLiveCameraShot>(true);
            List<VLiveCameraShot> updatedShots = new List<VLiveCameraShot>();

            for (int i = 0; i < _palettePresets.Count; i++)
            {
                VLiveCameraMotionPreset preset = _palettePresets[i];
                if (preset == null)
                {
                    continue;
                }

                // 既存Shotとの照合（Preset参照の一致）
                VLiveCameraShot matchedShot = null;
                if (existingShots != null)
                {
                    foreach (var s in existingShots)
                    {
                        if (s != null && s.Preset == preset && !updatedShots.Contains(s))
                        {
                            matchedShot = s;
                            break;
                        }
                    }
                }

                if (matchedShot != null)
                {
                    // 既存Shot: ユーザーの構図・Lens・Spline・速度調整は保持し、Targetと参照のみ同期
                    Undo.RecordObject(matchedShot, "Update Shot References");
                    EditorUtility.SetDirty(matchedShot);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(matchedShot);

                    if (matchedShot.CinemachineCamera != null)
                    {
                        Undo.RecordObject(matchedShot.CinemachineCamera, "Update Shot Target");
                        matchedShot.CinemachineCamera.Target.TrackingTarget = _performerTarget;
                        matchedShot.CinemachineCamera.Target.LookAtTarget = _performerTarget;
                        EditorUtility.SetDirty(matchedShot.CinemachineCamera);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(matchedShot.CinemachineCamera);
                    }

                    updatedShots.Add(matchedShot);
                }
                else
                {
                    // 新規Shot: Preset初期値から作成
                    string shotGoName = $"Shot {i + 1} ({preset.DisplayName})";
                    var shotGo = new GameObject(shotGoName);
                    shotGo.transform.SetParent(shotsContainer, false);
                    Undo.RegisterCreatedObjectUndo(shotGo, "Create Shot GameObject");

                    Vector3 initialWorldPos = (preset.ControlPoints != null && preset.ControlPoints.Length > 0)
                        ? _performerTarget.TransformPoint(preset.ControlPoints[0])
                        : _performerTarget.TransformPoint(new Vector3(0f, 1.3f, -3.5f));
                    shotGo.transform.position = initialWorldPos;
                    shotGo.transform.LookAt(_performerTarget.position + preset.TargetOffset);

                    var cmCam = shotGo.AddComponent<CinemachineCamera>();
                    cmCam.Target.TrackingTarget = _performerTarget;
                    cmCam.Target.LookAtTarget = _performerTarget;
                    cmCam.Lens.FieldOfView = preset.FieldOfView;
                    cmCam.Priority = (i == 0) ? 10 : 0;
                    EditorUtility.SetDirty(cmCam);

                    var composer = shotGo.AddComponent<CinemachineRotationComposer>();
                    composer.TargetOffset = preset.TargetOffset;
                    EditorUtility.SetDirty(composer);

                    CinemachineSplineDolly dolly = null;
                    if (preset.ShotType == VLiveCameraShot.ShotType.Spline)
                    {
                        string splineGoName = $"SplinePath_Shot {i + 1} ({preset.DisplayName})";
                        var splineGo = new GameObject(splineGoName);
                        splineGo.transform.SetParent(splinesContainer, false);
                        Undo.RegisterCreatedObjectUndo(splineGo, "Create Spline GameObject");

                        var splineContainer = splineGo.AddComponent<SplineContainer>();
                        var spline = splineContainer.Spline;
                        spline.Clear();

                        if (preset.ControlPoints != null && preset.ControlPoints.Length > 0)
                        {
                            for (int p = 0; p < preset.ControlPoints.Length; p++)
                            {
                                Vector3 worldPoint = _performerTarget.TransformPoint(preset.ControlPoints[p]);
                                Vector3 localPoint = splineContainer.transform.InverseTransformPoint(worldPoint);
                                spline.Add(new BezierKnot((float3)localPoint));
                            }
                        }
                        else
                        {
                            Vector3 p1 = _performerTarget.TransformPoint(new Vector3(0f, 1.3f, -4f));
                            Vector3 p2 = _performerTarget.TransformPoint(new Vector3(0f, 1.3f, -2f));
                            spline.Add(new BezierKnot((float3)splineContainer.transform.InverseTransformPoint(p1)));
                            spline.Add(new BezierKnot((float3)splineContainer.transform.InverseTransformPoint(p2)));
                        }

                        spline.SetTangentMode(TangentMode.AutoSmooth);
                        EditorUtility.SetDirty(splineContainer);

                        dolly = shotGo.AddComponent<CinemachineSplineDolly>();
                        dolly.Spline = splineContainer;
                        dolly.PositionUnits = PathIndexUnit.Normalized;
                        dolly.CameraPosition = 0f;
                        EditorUtility.SetDirty(dolly);
                    }

                    var shot = shotGo.AddComponent<VLiveCameraShot>();
                    shot.Configure(
                        preset.DisplayName,
                        preset,
                        cmCam,
                        preset.ShotType,
                        dolly,
                        preset.InitialSpeed,
                        preset.DecelerationDistance
                    );
                    EditorUtility.SetDirty(shot);

                    updatedShots.Add(shot);
                }
            }

            // 6. SwitcherのShot一覧とBrain参照を反映
            var soSwitcher = new SerializedObject(switcher);
            soSwitcher.FindProperty("_cinemachineBrain").objectReferenceValue = brain;
            var shotsProp = soSwitcher.FindProperty("_shots");
            shotsProp.ClearArray();
            for (int i = 0; i < updatedShots.Count; i++)
            {
                shotsProp.InsertArrayElementAtIndex(i);
                shotsProp.GetArrayElementAtIndex(i).objectReferenceValue = updatedShots[i];
            }

            soSwitcher.ApplyModifiedProperties();

            // 7. KeyboardInput参照を反映
            var soInput = new SerializedObject(keyboardInput);
            soInput.FindProperty("_switcher").objectReferenceValue = switcher;
            soInput.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(currentScene);

            Selection.activeGameObject = rigGo;
            EditorGUIUtility.PingObject(rigGo);

            Debug.Log($"[VLiveCameraSetupWindow] Camera Rig completed with {updatedShots.Count} shots (Target: {_performerTarget.name}).");
        }
    }
}
