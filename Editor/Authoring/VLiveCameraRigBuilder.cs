using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// VLiveCameraRigの初期作成と、Shot同期・再構築・削除の明示操作を調整するEditor専用オーケストレーター。
    /// </summary>
    public static class VLiveCameraRigBuilder
    {
        // Fields

        public const string RigGameObjectName = "Virtual Camera";
        public const string ShotsContainerName = "Shots";
        public const string SplinesContainerName = "Splines";
        public const string AimProxiesContainerName = "Aim Proxies";

        private static readonly string[] DefaultPresetNames = new string[]
        {
            "FixedMedium",
            "PushIn",
            "PullOut",
            "TruckLeft",
            "TruckRight",
            "ArcAround",
            "PushInRolling",
            "CraneRise",
            "CraneDrop",
            "PedestalRise"
        };


        // Methods

        /// <summary>
        /// GameObjectメニューから新しいCamera Rigを作成します。
        /// </summary>
        [MenuItem("GameObject/VLiveKit/" + RigGameObjectName, false, 10)]
        public static void CreateRigFromMenu()
        {
            CreateRig(null, null);
        }

        /// <summary>
        /// 現在のSceneに新しいVLiveCameraRig一式を作成します。
        /// </summary>
        public static VLiveCameraRig CreateRig(Transform performerTarget = null, UnityEngine.Camera outputCamera = null)
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[VLiveCameraRigBuilder] Cannot create Camera Rig during Play Mode.");
                return null;
            }

            var currentScene = SceneManager.GetActiveScene();

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Create Camera Rig");
            int undoGroup = Undo.GetCurrentGroup();


            var rigGo = new GameObject(RigGameObjectName);
            Undo.RegisterCreatedObjectUndo(rigGo, "Create Camera Rig Root");

            var rig = rigGo.AddComponent<VLiveCameraRig>();
            var switcher = rigGo.AddComponent<VLiveCameraSwitcher>();
            var keyboardInput = rigGo.AddComponent<VLiveCameraKeyboardInput>();

            switcher.SetRig(rig);
            keyboardInput.SetSwitcher(switcher);


            rig.ProgramCamera = outputCamera;

            var shotsGo = new GameObject(ShotsContainerName);
            shotsGo.transform.SetParent(rigGo.transform, false);
            Undo.RegisterCreatedObjectUndo(shotsGo, "Create Shots Container");

            var splinesGo = new GameObject(SplinesContainerName);
            splinesGo.transform.SetParent(rigGo.transform, false);
            Undo.RegisterCreatedObjectUndo(splinesGo, "Create Splines Container");

            var aimProxiesGo = new GameObject(AimProxiesContainerName);
            aimProxiesGo.transform.SetParent(rigGo.transform, false);
            Undo.RegisterCreatedObjectUndo(aimProxiesGo, "Create Aim Proxies Container");

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

            Debug.Log("[VLiveCameraRigBuilder] VLive Camera Rig created. Assign Target and Program Camera in Inspector, then click 'Apply / Sync'.");
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

            Transform shotsContainer = EnsureContainer(rig.transform, ShotsContainerName);
            Transform splinesContainer = EnsureContainer(rig.transform, SplinesContainerName);
            Transform aimProxiesContainer = EnsureContainer(rig.transform, AimProxiesContainerName);

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
                    repairedCount += VLiveCameraShotBuilder.RepairReferences(rig, slot.Shot, i, aimProxiesContainer, splinesContainer);
                }
                else
                {
                    // 不足Shot: Preset初期値から新規生成
                    VLiveCameraShotBuilder.BuildNew(rig, preset, i, shotsContainer, splinesContainer, aimProxiesContainer, slot);
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

            Debug.Log($"[VLiveCameraRigBuilder] Apply / Sync completed (New shots created: {createdCount}, References repaired: {repairedCount}).");
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

                VLiveCameraShotBuilder.BuildNew(rig, slot.Preset, slotIndex, shotsContainer, splinesContainer, aimProxiesContainer, slot);
            }
            else
            {
                VLiveCameraShotBuilder.Rebuild(rig, slot.Preset, slotIndex, slot.Shot, aimProxiesContainer, splinesContainer);
            }

            EditorUtility.SetDirty(rig);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);

            Debug.Log($"[VLiveCameraRigBuilder] Shot {slotIndex + 1} ({slot.Preset.DisplayName}) rebuilt from preset.");
        }

        /// <summary>
        /// Scene上の指定ShotをPreset初期値から再構築します（手動調整は上書きされます）。
        /// </summary>
        public static void RebuildShotFromPreset(VLiveCameraShot shot)
        {
            if (shot == null)
            {
                return;
            }

            var rig = shot.GetComponentInParent<VLiveCameraRig>();
            if (rig == null)
            {
                Debug.LogWarning("[VLiveCameraRigBuilder] Shot does not belong to a VLiveCameraRig.");
                return;
            }

            int slotIndex = -1;
            if (rig.Slots != null)
            {
                for (int i = 0; i < rig.Slots.Count; i++)
                {
                    if (rig.Slots[i] != null && rig.Slots[i].Shot == shot)
                    {
                        slotIndex = i;
                        break;
                    }
                }
            }

            if (slotIndex < 0)
            {
                Debug.LogWarning($"[VLiveCameraRigBuilder] Shot '{shot.name}' is not assigned to any slot in Rig.");
                return;
            }

            RebuildShotFromPreset(rig, slotIndex);
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

                    VLiveCameraShotBuilder.BuildNew(rig, slot.Preset, i, shotsContainer, splinesContainer, aimProxiesContainer, slot);
                }
                else
                {
                    VLiveCameraShotBuilder.Rebuild(rig, slot.Preset, i, slot.Shot, aimProxiesContainer, splinesContainer);
                }
            }

            EditorUtility.SetDirty(rig);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);

            Debug.Log("[VLiveCameraRigBuilder] All shots rebuilt from presets.");
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

            Debug.Log($"[VLiveCameraRigBuilder] Deleted Shot {slotIndex + 1} GameObject and associated components.");
        }

        /// <summary>
        /// 指定されたShot GameObject、Spline、Aim ProxyをSceneから明示的に削除します。
        /// </summary>
        public static void DeleteShotGameObject(VLiveCameraShot shot)
        {
            if (shot == null)
            {
                return;
            }

            var rig = shot.GetComponentInParent<VLiveCameraRig>();
            if (rig == null)
            {
                Debug.LogWarning("[VLiveCameraRigBuilder] Shot does not belong to a VLiveCameraRig.");
                return;
            }

            int slotIndex = -1;
            if (rig.Slots != null)
            {
                for (int i = 0; i < rig.Slots.Count; i++)
                {
                    if (rig.Slots[i] != null && rig.Slots[i].Shot == shot)
                    {
                        slotIndex = i;
                        break;
                    }
                }
            }

            if (slotIndex >= 0)
            {
                DeleteShotGameObject(rig, slotIndex);
            }
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

        private static List<VLiveCameraMotionPreset> LoadDefaultPresets()
        {
            var list = new List<VLiveCameraMotionPreset>();

            foreach (string presetName in DefaultPresetNames)
            {
                string path = $"{VLiveCameraPresetAssetCreator.MotionPresetFolderPath}/{presetName}.asset";
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
                    string path = $"{VLiveCameraPresetAssetCreator.MotionPresetFolderPath}/{presetName}.asset";
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
