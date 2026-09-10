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
    /// VLiveCameraRigの生成、同期、再構築、削除を集約して担当するエディタ専用ビルダー。
    /// Setup WindowおよびRig Inspectorから共通利用されます。
    /// </summary>
    public static class VLiveCameraRigBuilder
    {
        // Fields

        public const string RigGameObjectName = "VLive Camera Rig";
        public const string ShotsContainerName = "Shots";
        public const string SplinesContainerName = "Splines";
        public const string ProgramCameraName = "Program Camera";

        private static readonly string[] DefaultPresetNames = new string[]
        {
            "FixedMedium",
            "PushIn",
            "PullOut",
            "TruckLeft",
            "TruckRight",
            "ArcAround"
        };


        // Methods

        /// <summary>
        /// GameObjectメニューから新しいCamera Rigを作成します。
        /// </summary>
        [MenuItem("GameObject/VLiveKit/Camera Rig", false, 10)]
        public static void CreateRigFromMenu()
        {
            CreateRig();
        }

        /// <summary>
        /// 現在のSceneに新しいVLiveCameraRig一式を作成します。
        /// </summary>
        /// <param name="performerTarget">演者Target（任意）。</param>
        /// <param name="outputCamera">Program出力Camera（任意）。</param>
        /// <returns>生成されたVLiveCameraRig。</returns>
        public static VLiveCameraRig CreateRig(Transform performerTarget = null, UnityEngine.Camera outputCamera = null)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[VLiveCameraRigBuilder] Play Mode中はCamera Rigを作成できません。");
                return null;
            }

            Scene currentScene = SceneManager.GetActiveScene();

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Create Camera Rig");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Root GameObject & Components
            var rigGo = new GameObject(RigGameObjectName);
            Undo.RegisterCreatedObjectUndo(rigGo, "Create Camera Rig Root");

            var rig = rigGo.AddComponent<VLiveCameraRig>();
            var switcher = rigGo.AddComponent<VLiveCameraSwitcher>();
            var keyboardInput = rigGo.AddComponent<VLiveCameraKeyboardInput>();

            switcher.SetRig(rig);
            keyboardInput.SetSwitcher(switcher);

            // 2. Program Camera & CinemachineBrain
            UnityEngine.Camera programCam = outputCamera;
            if (programCam == null)
            {
                var camGo = new GameObject(ProgramCameraName);
                camGo.transform.SetParent(rigGo.transform, false);
                programCam = camGo.AddComponent<UnityEngine.Camera>();

                // MainCameraタグ: Scene内に既存のMainCameraがない場合のみ設定
                bool hasExistingMainCamera = false;
                var allCameras = Object.FindObjectsByType<UnityEngine.Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var cam in allCameras)
                {
                    if (cam.gameObject != camGo && cam.CompareTag("MainCamera"))
                    {
                        hasExistingMainCamera = true;
                        break;
                    }
                }

                if (!hasExistingMainCamera)
                {
                    camGo.tag = "MainCamera";
                }

                Undo.RegisterCreatedObjectUndo(camGo, "Create Program Camera");
            }

            var brain = programCam.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = Undo.AddComponent<CinemachineBrain>(programCam.gameObject);
            }
            else
            {
                Undo.RecordObject(brain, "Update CinemachineBrain Blend");
            }

            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            EditorUtility.SetDirty(brain);

            rig.ProgramCamera = programCam;
            if (performerTarget != null)
            {
                rig.PerformerTarget = performerTarget;
            }

            // 3. Containers
            var shotsGo = new GameObject(ShotsContainerName);
            shotsGo.transform.SetParent(rigGo.transform, false);
            Undo.RegisterCreatedObjectUndo(shotsGo, "Create Shots Container");

            var splinesGo = new GameObject(SplinesContainerName);
            splinesGo.transform.SetParent(rigGo.transform, false);
            Undo.RegisterCreatedObjectUndo(splinesGo, "Create Splines Container");

            // 4. Default 6 Preset Slots
            List<VLiveCameraMotionPreset> presets = LoadDefaultPresets();
            foreach (var preset in presets)
            {
                rig.Slots.Add(new VLiveCameraShotSlot(preset, null));
            }

            EditorUtility.SetDirty(rig);
            EditorUtility.SetDirty(switcher);
            EditorUtility.SetDirty(keyboardInput);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(currentScene);

            Selection.activeGameObject = rigGo;
            EditorGUIUtility.PingObject(rigGo);

            Debug.Log("[VLiveCameraRigBuilder] VLive Camera Rig を作成しました。InspectorでTargetを設定し、'Apply / Sync' を実行してください。");
            return rig;
        }

        /// <summary>
        /// Rigの設定に基づき、不足Shotの生成、参照修復、順序同期を実行します。既存の手動調整値は保持されます。
        /// </summary>
        /// <param name="rig">対象のVLiveCameraRig。</param>
        /// <returns>同期が成功した場合はtrue。</returns>
        public static bool ApplySync(VLiveCameraRig rig)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[VLiveCameraRigBuilder] Play Mode中はApply / Syncを実行できません。");
                return false;
            }

            if (rig == null)
            {
                return false;
            }

            if (rig.PerformerTarget == null)
            {
                Debug.LogWarning("[VLiveCameraRigBuilder] Performer Target が設定されていないため Apply / Sync を中断しました。");
                return false;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Apply / Sync Camera Rig");
            int undoGroup = Undo.GetCurrentGroup();

            Undo.RecordObject(rig, "Apply / Sync Camera Rig");

            // 1. Containersの存在確認と確保
            Transform shotsContainer = EnsureContainer(rig.transform, ShotsContainerName);
            Transform splinesContainer = EnsureContainer(rig.transform, SplinesContainerName);

            // 2. Program Camera & Brain確保
            if (rig.ProgramCamera == null)
            {
                Transform camChild = rig.transform.Find(ProgramCameraName);
                if (camChild != null)
                {
                    rig.ProgramCamera = camChild.GetComponent<UnityEngine.Camera>();
                }

                if (rig.ProgramCamera == null)
                {
                    var camGo = new GameObject(ProgramCameraName);
                    camGo.transform.SetParent(rig.transform, false);
                    rig.ProgramCamera = camGo.AddComponent<UnityEngine.Camera>();
                    Undo.RegisterCreatedObjectUndo(camGo, "Create Program Camera");
                }
            }

            CinemachineBrain brain = rig.CinemachineBrain;
            if (brain == null && rig.ProgramCamera != null)
            {
                brain = Undo.AddComponent<CinemachineBrain>(rig.ProgramCamera.gameObject);
            }

            if (brain != null)
            {
                Undo.RecordObject(brain, "Update CinemachineBrain Blend");
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
                EditorUtility.SetDirty(brain);
            }

            // 3. Switcher & KeyboardInput確保と参照連携
            var switcher = rig.GetComponent<VLiveCameraSwitcher>();
            if (switcher == null)
            {
                switcher = Undo.AddComponent<VLiveCameraSwitcher>(rig.gameObject);
            }

            switcher.SetRig(rig);
            EditorUtility.SetDirty(switcher);

            var keyboardInput = rig.GetComponent<VLiveCameraKeyboardInput>();
            if (keyboardInput == null)
            {
                keyboardInput = Undo.AddComponent<VLiveCameraKeyboardInput>(rig.gameObject);
            }

            keyboardInput.SetSwitcher(switcher);
            EditorUtility.SetDirty(keyboardInput);

            // 4. Slotsの同期
            int createdCount = 0;
            int repairedCount = 0;
            var assignedShots = new HashSet<VLiveCameraShot>();

            for (int i = 0; i < rig.Slots.Count; i++)
            {
                VLiveCameraShotSlot slot = rig.Slots[i];
                if (slot == null || slot.Preset == null)
                {
                    continue;
                }

                VLiveCameraMotionPreset preset = slot.Preset;

                // 既に別のSlotに割り当て済みのShotは重複共有せず、専用Shotの新規生成へ
                if (slot.Shot != null && assignedShots.Contains(slot.Shot))
                {
                    slot.Shot = null;
                }

                if (slot.Shot != null)
                {
                    assignedShots.Add(slot.Shot);

                    // 既存Shot: 手動調整（Transform, Lens, Spline, 速度）を保持し、Target参照のみ修復
                    Undo.RecordObject(slot.Shot, "Update Shot Target Reference");
                    if (slot.Shot.CinemachineCamera == null)
                    {
                        var cmCam = Undo.AddComponent<CinemachineCamera>(slot.Shot.gameObject);
                        cmCam.Target.TrackingTarget = rig.PerformerTarget;
                        cmCam.Target.LookAtTarget = rig.PerformerTarget;
                        cmCam.Lens.FieldOfView = preset.FieldOfView;
                        cmCam.Priority = (i == 0) ? 10 : 0;
                        EditorUtility.SetDirty(cmCam);
                        repairedCount++;
                    }
                    else
                    {
                        Undo.RecordObject(slot.Shot.CinemachineCamera, "Update CinemachineCamera Target");
                        slot.Shot.CinemachineCamera.Target.TrackingTarget = rig.PerformerTarget;
                        slot.Shot.CinemachineCamera.Target.LookAtTarget = rig.PerformerTarget;
                        EditorUtility.SetDirty(slot.Shot.CinemachineCamera);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(slot.Shot.CinemachineCamera);
                    }

                    // 壊れたRotationComposerの修復
                    var composer = slot.Shot.GetComponent<CinemachineRotationComposer>();
                    if (composer == null)
                    {
                        composer = Undo.AddComponent<CinemachineRotationComposer>(slot.Shot.gameObject);
                        composer.TargetOffset = new Vector3(preset.TargetOffset.x, rig.TargetHeight + preset.TargetOffset.y, preset.TargetOffset.z);
                        EditorUtility.SetDirty(composer);
                        repairedCount++;
                    }

                    // 壊れたSpline参照の修復
                    if (preset.ShotType == VLiveCameraShot.ShotType.Spline)
                    {
                        if (slot.Shot.SplineDolly == null || slot.Shot.SplineDolly.Spline == null)
                        {
                            RepairSplineForShot(rig, preset, i, splinesContainer, slot.Shot);
                            repairedCount++;
                        }
                    }

                    EditorUtility.SetDirty(slot.Shot);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(slot.Shot);
                }
                else
                {
                    // 不足Shot: Preset初期値から新規生成
                    BuildNewShotForSlot(rig, preset, i, shotsContainer, splinesContainer, slot);
                    if (slot.Shot != null)
                    {
                        assignedShots.Add(slot.Shot);
                    }
                    createdCount++;
                }
            }

            EditorUtility.SetDirty(rig);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rig);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);

            Debug.Log($"[VLiveCameraRigBuilder] Apply / Sync 完了 (新規Shot生成: {createdCount}, 参照修復: {repairedCount})。");
            return true;
        }

        /// <summary>
        /// 指定されたスロットのShotをPreset初期値から再構築します（手動調整は上書きされます）。
        /// </summary>
        /// <param name="rig">対象のVLiveCameraRig。</param>
        /// <param name="slotIndex">再構築するスロット番号（0始まり）。</param>
        public static void RebuildShotFromPreset(VLiveCameraRig rig, int slotIndex)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[VLiveCameraRigBuilder] Play Mode中はRebuildを実行できません。");
                return;
            }

            if (rig == null || rig.Slots == null || slotIndex < 0 || slotIndex >= rig.Slots.Count)
            {
                return;
            }

            VLiveCameraShotSlot slot = rig.Slots[slotIndex];
            if (slot == null || slot.Preset == null)
            {
                return;
            }

            if (rig.PerformerTarget == null)
            {
                Debug.LogWarning("[VLiveCameraRigBuilder] Performer Target が設定されていないため Rebuild できません。");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName($"Rebuild Shot {slotIndex + 1} From Preset");
            int undoGroup = Undo.GetCurrentGroup();

            if (slot.Shot == null)
            {
                Transform shotsContainer = EnsureContainer(rig.transform, ShotsContainerName);
                Transform splinesContainer = EnsureContainer(rig.transform, SplinesContainerName);
                BuildNewShotForSlot(rig, slot.Preset, slotIndex, shotsContainer, splinesContainer, slot);
            }
            else
            {
                RebuildExistingShot(rig, slot.Preset, slotIndex, slot.Shot);
            }

            EditorUtility.SetDirty(rig);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);

            Debug.Log($"[VLiveCameraRigBuilder] Shot {slotIndex + 1} ({slot.Preset.DisplayName}) を Preset 初期値から再構築しました。");
        }

        /// <summary>
        /// すべてのスロットのShotをPreset初期値から一括再構築します（手動調整は上書きされます）。
        /// </summary>
        /// <param name="rig">対象のVLiveCameraRig。</param>
        public static void RebuildAllShotsFromPreset(VLiveCameraRig rig)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[VLiveCameraRigBuilder] Play Mode中はRebuildを実行できません。");
                return;
            }

            if (rig == null || rig.Slots == null)
            {
                return;
            }

            if (rig.PerformerTarget == null)
            {
                Debug.LogWarning("[VLiveCameraRigBuilder] Performer Target が設定されていないため Rebuild できません。");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Rebuild All Shots From Presets");
            int undoGroup = Undo.GetCurrentGroup();

            Transform shotsContainer = EnsureContainer(rig.transform, ShotsContainerName);
            Transform splinesContainer = EnsureContainer(rig.transform, SplinesContainerName);

            for (int i = 0; i < rig.Slots.Count; i++)
            {
                VLiveCameraShotSlot slot = rig.Slots[i];
                if (slot == null || slot.Preset == null)
                {
                    continue;
                }

                if (slot.Shot == null)
                {
                    BuildNewShotForSlot(rig, slot.Preset, i, shotsContainer, splinesContainer, slot);
                }
                else
                {
                    RebuildExistingShot(rig, slot.Preset, i, slot.Shot);
                }
            }

            EditorUtility.SetDirty(rig);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);

            Debug.Log("[VLiveCameraRigBuilder] すべてのShotをPreset初期値から再構築しました。");
        }

        /// <summary>
        /// 指定スロットに対応するShot GameObjectおよびSplineをSceneから明示的に削除し、スロットの参照をクリアします。
        /// </summary>
        /// <param name="rig">対象のVLiveCameraRig。</param>
        /// <param name="slotIndex">削除対象のスロット番号（0始まり）。</param>
        public static void DeleteShotGameObject(VLiveCameraRig rig, int slotIndex)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[VLiveCameraRigBuilder] Play Mode中は削除を実行できません。");
                return;
            }

            if (rig == null || rig.Slots == null || slotIndex < 0 || slotIndex >= rig.Slots.Count)
            {
                return;
            }

            VLiveCameraShotSlot slot = rig.Slots[slotIndex];
            if (slot == null || slot.Shot == null)
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName($"Delete Shot {slotIndex + 1} GameObject");
            int undoGroup = Undo.GetCurrentGroup();

            VLiveCameraShot shot = slot.Shot;

            if (shot.SplineDolly != null && shot.SplineDolly.Spline != null)
            {
                // Rig配下のSplineのみ削除（外部アセットの誤削除を防止）
                if (shot.SplineDolly.Spline.transform.IsChildOf(rig.transform))
                {
                    Undo.DestroyObjectImmediate(shot.SplineDolly.Spline.gameObject);
                }
            }

            // Rig配下のShot GameObjectのみ削除
            if (shot.transform.IsChildOf(rig.transform))
            {
                Undo.DestroyObjectImmediate(shot.gameObject);
            }

            Undo.RecordObject(rig, "Clear Shot Reference in Slot");
            slot.Shot = null;
            EditorUtility.SetDirty(rig);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);

            Debug.Log($"[VLiveCameraRigBuilder] Shot {slotIndex + 1} のGameObjectを削除しました。");
        }

        private static Transform EnsureContainer(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                child = go.transform;
                Undo.RegisterCreatedObjectUndo(go, $"Create {name} Container");
            }

            return child;
        }

        private static void BuildNewShotForSlot(
            VLiveCameraRig rig,
            VLiveCameraMotionPreset preset,
            int slotIndex,
            Transform shotsContainer,
            Transform splinesContainer,
            VLiveCameraShotSlot slot)
        {
            string shotGoName = $"Shot {slotIndex + 1} ({preset.DisplayName})";
            var shotGo = new GameObject(shotGoName);
            shotGo.transform.SetParent(shotsContainer, false);
            Undo.RegisterCreatedObjectUndo(shotGo, "Create Shot GameObject");

            Vector3[] points = preset.ControlPoints;
            Vector3 p0 = (points != null && points.Length > 0) ? points[0] : new Vector3(0f, 1.3f, 3.5f);
            Vector3 scaledP0 = new Vector3(p0.x * rig.DistanceScale, p0.y, p0.z * rig.DistanceScale);

            Quaternion orientation = rig.GetReferenceOrientation();
            Vector3 targetPos = rig.PerformerTarget.position;
            Vector3 worldP0 = targetPos + orientation * scaledP0;

            shotGo.transform.position = worldP0;
            Vector3 lookTarget = targetPos + Vector3.up * rig.TargetHeight + orientation * preset.TargetOffset;
            shotGo.transform.LookAt(lookTarget);

            var cmCam = shotGo.AddComponent<CinemachineCamera>();
            cmCam.Target.TrackingTarget = rig.PerformerTarget;
            cmCam.Target.LookAtTarget = rig.PerformerTarget;
            cmCam.Lens.FieldOfView = preset.FieldOfView;
            cmCam.Priority = (slotIndex == 0) ? 10 : 0;
            EditorUtility.SetDirty(cmCam);

            var composer = shotGo.AddComponent<CinemachineRotationComposer>();
            composer.TargetOffset = new Vector3(preset.TargetOffset.x, rig.TargetHeight + preset.TargetOffset.y, preset.TargetOffset.z);
            EditorUtility.SetDirty(composer);

            CinemachineSplineDolly dolly = null;
            if (preset.ShotType == VLiveCameraShot.ShotType.Spline)
            {
                string splineGoName = $"SplinePath_Shot {slotIndex + 1} ({preset.DisplayName})";
                var splineGo = new GameObject(splineGoName);
                splineGo.transform.SetParent(splinesContainer, false);
                Undo.RegisterCreatedObjectUndo(splineGo, "Create Spline GameObject");

                var splineContainer = splineGo.AddComponent<SplineContainer>();
                PopulateSplineKnots(splineContainer, rig, preset);

                dolly = shotGo.AddComponent<CinemachineSplineDolly>();
                dolly.Spline = splineContainer;
                dolly.PositionUnits = PathIndexUnit.Normalized;
                dolly.CameraPosition = 0f;
                EditorUtility.SetDirty(dolly);
            }

            var shot = shotGo.AddComponent<VLiveCameraShot>();
            shot.Configure(
                preset.DisplayName,
                cmCam,
                preset.ShotType,
                dolly,
                preset.InitialSpeed,
                preset.DecelerationDistance
            );
            EditorUtility.SetDirty(shot);

            slot.Shot = shot;
        }

        private static void RebuildExistingShot(
            VLiveCameraRig rig,
            VLiveCameraMotionPreset preset,
            int slotIndex,
            VLiveCameraShot shot)
        {
            Vector3[] points = preset.ControlPoints;
            Vector3 p0 = (points != null && points.Length > 0) ? points[0] : new Vector3(0f, 1.3f, 3.5f);
            Vector3 scaledP0 = new Vector3(p0.x * rig.DistanceScale, p0.y, p0.z * rig.DistanceScale);

            Quaternion orientation = rig.GetReferenceOrientation();
            Vector3 targetPos = rig.PerformerTarget.position;
            Vector3 worldP0 = targetPos + orientation * scaledP0;

            Undo.RecordObject(shot.transform, "Rebuild Shot Transform");
            shot.transform.position = worldP0;
            Vector3 lookTarget = targetPos + Vector3.up * rig.TargetHeight + orientation * preset.TargetOffset;
            shot.transform.LookAt(lookTarget);

            if (shot.CinemachineCamera != null)
            {
                Undo.RecordObject(shot.CinemachineCamera, "Rebuild CinemachineCamera Settings");
                shot.CinemachineCamera.Target.TrackingTarget = rig.PerformerTarget;
                shot.CinemachineCamera.Target.LookAtTarget = rig.PerformerTarget;
                shot.CinemachineCamera.Lens.FieldOfView = preset.FieldOfView;
                EditorUtility.SetDirty(shot.CinemachineCamera);
            }

            var composer = shot.GetComponent<CinemachineRotationComposer>();
            if (composer == null)
            {
                composer = Undo.AddComponent<CinemachineRotationComposer>(shot.gameObject);
            }
            else
            {
                Undo.RecordObject(composer, "Rebuild Rotation Composer");
            }

            composer.TargetOffset = new Vector3(preset.TargetOffset.x, rig.TargetHeight + preset.TargetOffset.y, preset.TargetOffset.z);
            EditorUtility.SetDirty(composer);

            if (preset.ShotType == VLiveCameraShot.ShotType.Spline)
            {
                if (shot.SplineDolly != null && shot.SplineDolly.Spline != null)
                {
                    Undo.RecordObject(shot.SplineDolly.Spline, "Rebuild Spline Knots");
                    PopulateSplineKnots(shot.SplineDolly.Spline, rig, preset);

                    Undo.RecordObject(shot.SplineDolly, "Reset Spline Dolly");
                    shot.SplineDolly.CameraPosition = 0f;
                    EditorUtility.SetDirty(shot.SplineDolly);
                }
                else
                {
                    Transform splinesContainer = EnsureContainer(rig.transform, SplinesContainerName);
                    RepairSplineForShot(rig, preset, slotIndex, splinesContainer, shot);
                }
            }
            else
            {
                // Fixed ShotへのRebuild時、残存しているSplineDollyと専用Splineをクリーンアップ
                if (shot.SplineDolly != null)
                {
                    if (shot.SplineDolly.Spline != null && shot.SplineDolly.Spline.transform.IsChildOf(rig.transform))
                    {
                        Undo.DestroyObjectImmediate(shot.SplineDolly.Spline.gameObject);
                    }

                    Undo.DestroyObjectImmediate(shot.SplineDolly);
                }
            }

            Undo.RecordObject(shot, "Rebuild Shot Settings");
            shot.Configure(
                preset.DisplayName,
                shot.CinemachineCamera,
                preset.ShotType,
                shot.SplineDolly,
                preset.InitialSpeed,
                preset.DecelerationDistance
            );
            EditorUtility.SetDirty(shot);
        }

        private static void RepairSplineForShot(
            VLiveCameraRig rig,
            VLiveCameraMotionPreset preset,
            int slotIndex,
            Transform splinesContainer,
            VLiveCameraShot shot)
        {
            string splineGoName = $"SplinePath_Shot {slotIndex + 1} ({preset.DisplayName})";
            var splineGo = new GameObject(splineGoName);
            splineGo.transform.SetParent(splinesContainer, false);
            Undo.RegisterCreatedObjectUndo(splineGo, "Create Spline GameObject");

            var splineContainer = splineGo.AddComponent<SplineContainer>();
            PopulateSplineKnots(splineContainer, rig, preset);

            if (shot.SplineDolly == null && shot.CinemachineCamera != null)
            {
                Undo.AddComponent<CinemachineSplineDolly>(shot.CinemachineCamera.gameObject);
            }

            if (shot.SplineDolly != null)
            {
                Undo.RecordObject(shot.SplineDolly, "Repair Spline Dolly Reference");
                shot.SplineDolly.Spline = splineContainer;
                shot.SplineDolly.PositionUnits = PathIndexUnit.Normalized;
                shot.SplineDolly.CameraPosition = 0f;
                EditorUtility.SetDirty(shot.SplineDolly);
            }
        }

        private static void PopulateSplineKnots(SplineContainer splineContainer, VLiveCameraRig rig, VLiveCameraMotionPreset preset)
        {
            var spline = splineContainer.Spline;
            spline.Clear();

            Vector3[] points = preset.ControlPoints;
            if (points == null || points.Length == 0)
            {
                points = new Vector3[]
                {
                    new Vector3(0f, 1.3f, 4.5f),
                    new Vector3(0f, 1.3f, 1.8f)
                };
            }

            Vector3 p0 = points[0];
            Vector3 scaledP0 = new Vector3(p0.x * rig.DistanceScale, p0.y, p0.z * rig.DistanceScale);
            Quaternion orientation = rig.GetReferenceOrientation();
            Vector3 targetPos = rig.PerformerTarget != null ? rig.PerformerTarget.position : Vector3.zero;

            for (int p = 0; p < points.Length; p++)
            {
                Vector3 scaledPoint;
                if (p == 0)
                {
                    scaledPoint = scaledP0;
                }
                else
                {
                    Vector3 delta = points[p] - p0;
                    scaledPoint = scaledP0 + delta * rig.MotionScale;
                }

                Vector3 worldPoint = targetPos + orientation * scaledPoint;
                Vector3 localPoint = splineContainer.transform.InverseTransformPoint(worldPoint);
                spline.Add(new BezierKnot((float3)localPoint));
            }

            spline.SetTangentMode(TangentMode.AutoSmooth);
            EditorUtility.SetDirty(splineContainer);
        }

        private static List<VLiveCameraMotionPreset> LoadDefaultPresets()
        {
            var list = new List<VLiveCameraMotionPreset>();

            foreach (string presetName in DefaultPresetNames)
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
                    list.Add(preset);
                }
            }

            if (list.Count < 6)
            {
                VLiveCameraPresetAssetCreator.CreateMissingDefaultPresets();
                list.Clear();
                foreach (string presetName in DefaultPresetNames)
                {
                    string path = $"{VLiveCameraPresetAssetCreator.PresetFolderPath}/{presetName}.asset";
                    var preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(path);
                    if (preset != null)
                    {
                        list.Add(preset);
                    }
                }
            }

            return list;
        }
    }
}
