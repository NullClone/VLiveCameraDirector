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
        public const string AimProxiesContainerName = "Aim Proxies";
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

            var aimProxiesGo = new GameObject(AimProxiesContainerName);
            aimProxiesGo.transform.SetParent(rigGo.transform, false);
            Undo.RegisterCreatedObjectUndo(aimProxiesGo, "Create Aim Proxies Container");

            // 4. Default Preset Slots
            List<VLiveCameraMotionPreset> presets = LoadDefaultPresets();
            foreach (var preset in presets)
            {
                rig.AddSlot(new VLiveCameraShotSlot(preset, null));
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
        /// Rigの設定に基づき、不足Shot・Aim Proxy・Spline・MotionPlayerの生成、参照修復、順序同期を実行します。
        /// 既存Shotの手動調整値は保持されます。
        /// </summary>
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

            if (!rig.IsForwardReferenceValid())
            {
                Debug.LogError("[VLiveCameraRigBuilder] 正面基準設定が無効（Custom Reference が未設定または垂直方向）のため Apply / Sync を中断しました。");
                return false;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Apply / Sync Camera Rig");
            int undoGroup = Undo.GetCurrentGroup();

            Undo.RecordObject(rig, "Apply / Sync Camera Rig");

            // 1. Containersの存在確認と確保
            Transform shotsContainer = EnsureContainer(rig.transform, ShotsContainerName);
            Transform splinesContainer = EnsureContainer(rig.transform, SplinesContainerName);
            Transform aimProxiesContainer = EnsureContainer(rig.transform, AimProxiesContainerName);

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
                    slot.SetShot(null);
                }

                // Rig所有でない外部Shotが割り当てられている場合は変更せず、専用Shotを新規生成
                if (slot.Shot != null && !IsShotOwnedByRig(rig, slot.Shot))
                {
                    Debug.LogWarning($"[VLiveCameraRigBuilder] Slot {i + 1} の Shot '{slot.Shot.name}' はこのRig所有の生成物ではないため変更しません。専用Shotを新規生成します。");
                    slot.SetShot(null);
                }

                if (slot.Shot != null)
                {
                    assignedShots.Add(slot.Shot);

                    // 既存Shot: 手動調整（Transform, Lens, Spline, 速度）を保持し、Targetと破損参照のみ修復
                    repairedCount += RepairExistingShotReferences(rig, slot.Shot, i, aimProxiesContainer, splinesContainer);
                }
                else
                {
                    // 不足Shot: Preset初期値から新規生成
                    BuildNewShotForSlot(rig, preset, i, shotsContainer, splinesContainer, aimProxiesContainer, slot);
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

            if (!rig.IsForwardReferenceValid())
            {
                Debug.LogError("[VLiveCameraRigBuilder] 正面基準設定が無効のため Rebuild できません。");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName($"Rebuild Shot {slotIndex + 1} From Preset");
            int undoGroup = Undo.GetCurrentGroup();
            Undo.RecordObject(rig, $"Rebuild Shot {slotIndex + 1} From Preset");

            Transform shotsContainer = EnsureContainer(rig.transform, ShotsContainerName);
            Transform splinesContainer = EnsureContainer(rig.transform, SplinesContainerName);
            Transform aimProxiesContainer = EnsureContainer(rig.transform, AimProxiesContainerName);

            if (slot.Shot == null || !IsShotOwnedByRig(rig, slot.Shot))
            {
                if (slot.Shot != null)
                {
                    Debug.LogWarning($"[VLiveCameraRigBuilder] Slot {slotIndex + 1} の Shot '{slot.Shot.name}' はこのRig所有の生成物ではないため変更しません。専用Shotを新規生成します。");
                    slot.SetShot(null);
                }

                BuildNewShotForSlot(rig, slot.Preset, slotIndex, shotsContainer, splinesContainer, aimProxiesContainer, slot);
            }
            else
            {
                RebuildExistingShot(rig, slot.Preset, slotIndex, slot.Shot, aimProxiesContainer, splinesContainer);
            }

            EditorUtility.SetDirty(rig);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);

            Debug.Log($"[VLiveCameraRigBuilder] Shot {slotIndex + 1} ({slot.Preset.DisplayName}) を Preset 初期値から再構築しました。");
        }

        /// <summary>
        /// すべてのスロットのShotをPreset初期値から一括再構築します（手動調整は上書きされます）。
        /// </summary>
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

            if (!rig.IsForwardReferenceValid())
            {
                Debug.LogError("[VLiveCameraRigBuilder] 正面基準設定が無効のため Rebuild できません。");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Rebuild All Shots From Presets");
            int undoGroup = Undo.GetCurrentGroup();
            Undo.RecordObject(rig, "Rebuild All Shots From Presets");

            Transform shotsContainer = EnsureContainer(rig.transform, ShotsContainerName);
            Transform splinesContainer = EnsureContainer(rig.transform, SplinesContainerName);
            Transform aimProxiesContainer = EnsureContainer(rig.transform, AimProxiesContainerName);

            for (int i = 0; i < rig.Slots.Count; i++)
            {
                VLiveCameraShotSlot slot = rig.Slots[i];
                if (slot == null || slot.Preset == null)
                {
                    continue;
                }

                if (slot.Shot == null || !IsShotOwnedByRig(rig, slot.Shot))
                {
                    if (slot.Shot != null)
                    {
                        Debug.LogWarning($"[VLiveCameraRigBuilder] Slot {i + 1} の Shot '{slot.Shot.name}' はこのRig所有の生成物ではないため変更しません。専用Shotを新規生成します。");
                        slot.SetShot(null);
                    }

                    BuildNewShotForSlot(rig, slot.Preset, i, shotsContainer, splinesContainer, aimProxiesContainer, slot);
                }
                else
                {
                    RebuildExistingShot(rig, slot.Preset, i, slot.Shot, aimProxiesContainer, splinesContainer);
                }
            }

            EditorUtility.SetDirty(rig);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);

            Debug.Log("[VLiveCameraRigBuilder] すべてのShotをPreset初期値から再構築しました。");
        }

        /// <summary>
        /// 指定スロットに対応するShot GameObject、Spline、Aim ProxyをSceneから明示的に削除し、スロット参照をクリアします。
        /// </summary>
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

            if (!IsShotOwnedByRig(rig, shot))
            {
                Debug.LogWarning($"[VLiveCameraRigBuilder] Slot {slotIndex + 1} の Shot '{shot.name}' はこのRig所有の生成物ではないためSceneから削除しません。スロット参照のみクリアします。");
                Undo.RecordObject(rig, "Clear Shot Reference in Slot");
                slot.SetShot(null);
                EditorUtility.SetDirty(rig);
                Undo.CollapseUndoOperations(undoGroup);
                EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
                return;
            }

            Undo.RecordObject(rig, "Delete Shot GameObject");

            // Spline削除
            if (shot.SplineDolly != null && shot.SplineDolly.Spline != null)
            {
                if (shot.SplineDolly.Spline.transform.IsChildOf(rig.transform))
                {
                    Undo.DestroyObjectImmediate(shot.SplineDolly.Spline.gameObject);
                }
            }

            // Aim Proxy削除
            if (shot.AimProxy != null && shot.AimProxy.IsChildOf(rig.transform))
            {
                Undo.DestroyObjectImmediate(shot.AimProxy.gameObject);
            }

            // Shot GameObject削除
            if (shot.transform.IsChildOf(rig.transform))
            {
                Undo.DestroyObjectImmediate(shot.gameObject);
            }

            slot.SetShot(null);
            EditorUtility.SetDirty(rig);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);

            Debug.Log($"[VLiveCameraRigBuilder] Shot {slotIndex + 1} のGameObjectを一式削除しました。");
        }

        /// <summary>
        /// 指定されたShotがこのRig配下に生成・所有されたものであるかを判定します。
        /// </summary>
        public static bool IsShotOwnedByRig(VLiveCameraRig rig, VLiveCameraShot shot)
        {
            if (rig == null || shot == null)
            {
                return false;
            }

            return shot.transform.IsChildOf(rig.transform) && shot.gameObject != rig.gameObject;
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
            Transform aimProxiesContainer,
            VLiveCameraShotSlot slot)
        {
            string shotGoName = $"Shot {slotIndex + 1} ({preset.DisplayName})";
            var shotGo = new GameObject(shotGoName);
            shotGo.transform.SetParent(shotsContainer, false);
            Undo.RegisterCreatedObjectUndo(shotGo, "Create Shot GameObject");

            // 1. Aim Proxy生成
            string aimProxyName = $"AimProxy_Shot {slotIndex + 1} ({preset.DisplayName})";
            var aimProxyGo = new GameObject(aimProxyName);
            aimProxyGo.transform.SetParent(aimProxiesContainer, false);
            Undo.RegisterCreatedObjectUndo(aimProxyGo, "Create Aim Proxy GameObject");

            Quaternion orientation = rig.GetReferenceOrientation();
            Vector3 targetPos = rig.PerformerTarget.position;
            Vector3 initialAimPos = targetPos + Vector3.up * rig.TargetHeight + orientation * preset.AimOffset;
            aimProxyGo.transform.position = initialAimPos;

            // 2. Camera配置計算
            MotionKnot[] knots = preset.Knots;
            Vector3 p0 = (knots != null && knots.Length > 0) ? knots[0].Position : new Vector3(0f, 1.3f, 3.5f);
            Vector3 scaledP0 = new Vector3(p0.x * rig.DistanceScale, p0.y, p0.z * rig.DistanceScale);
            Vector3 worldP0 = targetPos + orientation * scaledP0;

            shotGo.transform.position = worldP0;
            shotGo.transform.LookAt(initialAimPos);

            // 3. CinemachineCamera
            var cmCam = shotGo.AddComponent<CinemachineCamera>();
            cmCam.Target.TrackingTarget = rig.PerformerTarget;
            cmCam.Target.LookAtTarget = aimProxyGo.transform;
            cmCam.Lens.FieldOfView = preset.FieldOfView;
            cmCam.Priority = (slotIndex == 0) ? 10 : 0;
            EditorUtility.SetDirty(cmCam);

            // 4. CinemachineRotationComposer
            var composer = shotGo.AddComponent<CinemachineRotationComposer>();
            composer.TargetOffset = Vector3.zero;
            composer.Composition.ScreenPosition = preset.ScreenPosition;
            composer.Composition.DeadZone.Enabled = preset.DeadZoneEnabled;
            composer.Composition.DeadZone.Size = preset.DeadZoneSize;
            composer.Composition.HardLimits.Enabled = preset.HardLimitsEnabled;
            composer.Composition.HardLimits.Size = preset.HardLimitsSize;
            composer.Composition.HardLimits.Offset = preset.HardLimitsOffset;
            composer.Damping = preset.Damping;
            composer.Lookahead.Enabled = preset.LookaheadEnabled;
            composer.Lookahead.Time = preset.LookaheadTime;
            composer.Lookahead.Smoothing = preset.LookaheadSmoothing;
            composer.CenterOnActivate = preset.CenterOnActivate;
            EditorUtility.SetDirty(composer);

            // 5. Spline Dolly
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
                dolly.PositionUnits = PathIndexUnit.Distance;
                dolly.CameraPosition = 0f;
                EditorUtility.SetDirty(dolly);
            }

            // 6. Motion Player & Shot
            var player = shotGo.AddComponent<VLiveCameraMotionPlayer>();
            var shot = shotGo.AddComponent<VLiveCameraShot>();

            shot.Configure(
                preset.DisplayName,
                cmCam,
                preset.ShotType,
                dolly,
                composer,
                aimProxyGo.transform,
                player,
                rig.PerformerTarget,
                preset,
                rig.TargetHeight,
                orientation
            );
            EditorUtility.SetDirty(shot);
            EditorUtility.SetDirty(player);

            slot.SetShot(shot);
        }

        private static int RepairExistingShotReferences(
            VLiveCameraRig rig,
            VLiveCameraShot shot,
            int slotIndex,
            Transform aimProxiesContainer,
            Transform splinesContainer)
        {
            int repaired = 0;
            Undo.RecordObject(shot, "Repair Shot References");

            shot.PerformerTarget = rig.PerformerTarget;

            // Aim Proxy修復
            Transform aimProxy = shot.AimProxy;
            if (aimProxy == null)
            {
                string aimProxyName = $"AimProxy_Shot {slotIndex + 1} ({shot.ShotName})";
                var aimProxyGo = new GameObject(aimProxyName);
                aimProxyGo.transform.SetParent(aimProxiesContainer, false);
                Undo.RegisterCreatedObjectUndo(aimProxyGo, "Create Repaired Aim Proxy");
                aimProxy = aimProxyGo.transform;
                repaired++;
            }

            // CinemachineCamera修復
            CinemachineCamera cmCam = shot.CinemachineCamera;
            if (cmCam == null)
            {
                cmCam = Undo.AddComponent<CinemachineCamera>(shot.gameObject);
                repaired++;
            }

            Undo.RecordObject(cmCam, "Update CinemachineCamera Targets");
            cmCam.Target.TrackingTarget = rig.PerformerTarget;
            cmCam.Target.LookAtTarget = aimProxy;
            EditorUtility.SetDirty(cmCam);

            // Rotation Composer修復
            CinemachineRotationComposer composer = shot.RotationComposer;
            if (composer == null)
            {
                composer = Undo.AddComponent<CinemachineRotationComposer>(shot.gameObject);
                composer.TargetOffset = Vector3.zero;
                repaired++;
            }

            // Spline Dolly修復
            CinemachineSplineDolly dolly = shot.SplineDolly;
            if (shot.Type == VLiveCameraShot.ShotType.Spline)
            {
                if (dolly == null)
                {
                    dolly = Undo.AddComponent<CinemachineSplineDolly>(shot.gameObject);
                    dolly.PositionUnits = PathIndexUnit.Distance;
                    repaired++;
                }

                if (dolly.Spline == null)
                {
                    string splineGoName = $"SplinePath_Shot {slotIndex + 1} ({shot.ShotName})";
                    var splineGo = new GameObject(splineGoName);
                    splineGo.transform.SetParent(splinesContainer, false);
                    Undo.RegisterCreatedObjectUndo(splineGo, "Create Repaired Spline");

                    var splineContainer = splineGo.AddComponent<SplineContainer>();
                    if (shot.AppliedPreset != null)
                    {
                        PopulateSplineKnots(splineContainer, rig, shot.AppliedPreset);
                    }

                    dolly.Spline = splineContainer;
                    dolly.PositionUnits = PathIndexUnit.Distance;
                    repaired++;
                }

                if (dolly.PositionUnits != PathIndexUnit.Distance)
                {
                    Undo.RecordObject(dolly, "Set Spline Dolly PositionUnits Distance");
                    dolly.PositionUnits = PathIndexUnit.Distance;
                    EditorUtility.SetDirty(dolly);
                }
            }

            // Motion Player修復
            VLiveCameraMotionPlayer player = shot.MotionPlayer;
            if (player == null)
            {
                player = Undo.AddComponent<VLiveCameraMotionPlayer>(shot.gameObject);
                repaired++;
            }

            player.Configure(shot, cmCam, dolly, composer, aimProxy);
            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(shot);

            return repaired;
        }

        private static void RebuildExistingShot(
            VLiveCameraRig rig,
            VLiveCameraMotionPreset preset,
            int slotIndex,
            VLiveCameraShot shot,
            Transform aimProxiesContainer,
            Transform splinesContainer)
        {
            Undo.RecordObject(shot, "Rebuild Shot");

            Quaternion orientation = rig.GetReferenceOrientation();
            Vector3 targetPos = rig.PerformerTarget.position;

            // 1. Aim Proxy更新
            Transform aimProxy = shot.AimProxy;
            if (aimProxy == null)
            {
                string aimProxyName = $"AimProxy_Shot {slotIndex + 1} ({preset.DisplayName})";
                var aimProxyGo = new GameObject(aimProxyName);
                aimProxyGo.transform.SetParent(aimProxiesContainer, false);
                Undo.RegisterCreatedObjectUndo(aimProxyGo, "Create Aim Proxy GameObject");
                aimProxy = aimProxyGo.transform;
            }

            Undo.RecordObject(aimProxy, "Update Aim Proxy Position");
            Vector3 aimPos = targetPos + Vector3.up * rig.TargetHeight + orientation * preset.AimOffset;
            aimProxy.position = aimPos;

            // 2. Camera Transform配置
            MotionKnot[] knots = preset.Knots;
            Vector3 p0 = (knots != null && knots.Length > 0) ? knots[0].Position : new Vector3(0f, 1.3f, 3.5f);
            Vector3 scaledP0 = new Vector3(p0.x * rig.DistanceScale, p0.y, p0.z * rig.DistanceScale);
            Vector3 worldP0 = targetPos + orientation * scaledP0;

            Undo.RecordObject(shot.transform, "Rebuild Shot Transform");
            shot.transform.position = worldP0;
            shot.transform.LookAt(aimPos);

            // 3. CinemachineCamera
            CinemachineCamera cmCam = shot.CinemachineCamera;
            if (cmCam == null)
            {
                cmCam = Undo.AddComponent<CinemachineCamera>(shot.gameObject);
            }

            Undo.RecordObject(cmCam, "Rebuild CinemachineCamera");
            cmCam.Target.TrackingTarget = rig.PerformerTarget;
            cmCam.Target.LookAtTarget = aimProxy;
            cmCam.Lens.FieldOfView = preset.FieldOfView;
            EditorUtility.SetDirty(cmCam);

            // 4. Rotation Composer
            CinemachineRotationComposer composer = shot.RotationComposer;
            if (composer == null)
            {
                composer = Undo.AddComponent<CinemachineRotationComposer>(shot.gameObject);
            }

            Undo.RecordObject(composer, "Rebuild Rotation Composer");
            composer.TargetOffset = Vector3.zero;
            composer.Composition.ScreenPosition = preset.ScreenPosition;
            composer.Composition.DeadZone.Enabled = preset.DeadZoneEnabled;
            composer.Composition.DeadZone.Size = preset.DeadZoneSize;
            composer.Composition.HardLimits.Enabled = preset.HardLimitsEnabled;
            composer.Composition.HardLimits.Size = preset.HardLimitsSize;
            composer.Composition.HardLimits.Offset = preset.HardLimitsOffset;
            composer.Damping = preset.Damping;
            composer.Lookahead.Enabled = preset.LookaheadEnabled;
            composer.Lookahead.Time = preset.LookaheadTime;
            composer.Lookahead.Smoothing = preset.LookaheadSmoothing;
            composer.CenterOnActivate = preset.CenterOnActivate;
            EditorUtility.SetDirty(composer);

            // 5. Spline
            CinemachineSplineDolly dolly = shot.SplineDolly;
            float resolvedSplineLength = 0f;

            if (preset.ShotType == VLiveCameraShot.ShotType.Spline)
            {
                SplineContainer splineContainer = null;
                if (dolly != null && dolly.Spline != null && dolly.Spline.transform.IsChildOf(rig.transform))
                {
                    splineContainer = dolly.Spline;
                }
                else
                {
                    string splineGoName = $"SplinePath_Shot {slotIndex + 1} ({preset.DisplayName})";
                    var splineGo = new GameObject(splineGoName);
                    splineGo.transform.SetParent(splinesContainer, false);
                    Undo.RegisterCreatedObjectUndo(splineGo, "Create Spline GameObject");
                    splineContainer = splineGo.AddComponent<SplineContainer>();
                }

                Undo.RecordObject(splineContainer, "Rebuild Spline Knots");
                PopulateSplineKnots(splineContainer, rig, preset);
                resolvedSplineLength = splineContainer.Spline != null ? splineContainer.Spline.GetLength() : 0f;

                if (dolly == null)
                {
                    dolly = Undo.AddComponent<CinemachineSplineDolly>(shot.gameObject);
                }

                Undo.RecordObject(dolly, "Rebuild Spline Dolly");
                dolly.Spline = splineContainer;
                dolly.PositionUnits = PathIndexUnit.Distance;
                dolly.CameraPosition = 0f;
                EditorUtility.SetDirty(dolly);
            }
            else
            {
                // Fixed Shot: 余分なSplineを削除
                if (dolly != null)
                {
                    if (dolly.Spline != null && dolly.Spline.transform.IsChildOf(rig.transform))
                    {
                        Undo.DestroyObjectImmediate(dolly.Spline.gameObject);
                    }

                    Undo.DestroyObjectImmediate(dolly);
                    dolly = null;
                }
            }

            // 6. Motion Player & Shot設定再同期
            VLiveCameraMotionPlayer player = shot.MotionPlayer;
            if (player == null)
            {
                player = Undo.AddComponent<VLiveCameraMotionPlayer>(shot.gameObject);
            }

            shot.Configure(
                preset.DisplayName,
                cmCam,
                preset.ShotType,
                dolly,
                composer,
                aimProxy,
                player,
                rig.PerformerTarget,
                preset,
                rig.TargetHeight,
                orientation
            );

            player.Configure(shot, cmCam, dolly, composer, aimProxy);
            player.PrepareStart();

            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(shot);
        }

        private static void PopulateSplineKnots(SplineContainer splineContainer, VLiveCameraRig rig, VLiveCameraMotionPreset preset)
        {
            var spline = splineContainer.Spline;
            spline.Clear();

            MotionKnot[] knots = preset.Knots;
            if (knots == null || knots.Length == 0)
            {
                knots = new MotionKnot[]
                {
                    new MotionKnot(new Vector3(0f, 1.3f, 4.5f)),
                    new MotionKnot(new Vector3(0f, 1.3f, 1.8f))
                };
            }

            spline.Closed = preset.IsClosed;

            Vector3 p0 = knots[0].Position;
            Vector3 scaledP0 = new Vector3(p0.x * rig.DistanceScale, p0.y, p0.z * rig.DistanceScale);
            Quaternion orientation = rig.GetReferenceOrientation();
            Vector3 targetPos = rig.PerformerTarget != null ? rig.PerformerTarget.position : Vector3.zero;

            for (int i = 0; i < knots.Length; i++)
            {
                MotionKnot knotData = knots[i];
                Vector3 scaledPoint;
                if (i == 0)
                {
                    scaledPoint = scaledP0;
                }
                else
                {
                    Vector3 delta = knotData.Position - p0;
                    scaledPoint = scaledP0 + delta * rig.MotionScale;
                }

                Vector3 worldPoint = targetPos + orientation * scaledPoint;
                Vector3 localPoint = splineContainer.transform.InverseTransformPoint(worldPoint);

                // Tangents: MotionScaleを適用してローカルへ変換
                Vector3 worldTanIn = orientation * (knotData.TangentIn * rig.MotionScale);
                Vector3 localTanIn = splineContainer.transform.InverseTransformVector(worldTanIn);

                Vector3 worldTanOut = orientation * (knotData.TangentOut * rig.MotionScale);
                Vector3 localTanOut = splineContainer.transform.InverseTransformVector(worldTanOut);

                // Rotation
                Quaternion worldRot = orientation * knotData.Rotation;
                Quaternion localRot = Quaternion.Inverse(splineContainer.transform.rotation) * worldRot;

                var bezierKnot = new BezierKnot((float3)localPoint, (float3)localTanIn, (float3)localTanOut, (quaternion)localRot);
                spline.Add(bezierKnot);

                // 全KnotをAutoSmoothへ強制せず、Presetが定義したTangentModeを個別に設定
                spline.SetTangentMode(i, knotData.TangentMode);

                // AutoSmooth以外のモードでは指定された明示Tangent値を厳密に維持
                if (knotData.TangentMode != TangentMode.AutoSmooth)
                {
                    spline[i] = bezierKnot;
                }
            }

            EditorUtility.SetDirty(splineContainer);
        }

        private static List<VLiveCameraMotionPreset> LoadDefaultPresets()
        {
            var list = new List<VLiveCameraMotionPreset>();

            foreach (string presetName in DefaultPresetNames)
            {
                string path = $"{VLiveCameraPresetAssetCreator.PresetFolderPath}/{presetName}.asset";
                var preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(path);
                if (preset != null)
                {
                    list.Add(preset);
                }
            }

            if (list.Count < DefaultPresetNames.Length)
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
                    else
                    {
                        Debug.LogError($"[VLiveCameraRigBuilder] 既定 Preset '{presetName}' をロードできませんでした（パス: {path}）。");
                    }
                }
            }

            return list;
        }
    }
}
