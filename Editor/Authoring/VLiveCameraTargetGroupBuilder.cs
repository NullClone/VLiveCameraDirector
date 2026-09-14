using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// Shot Sizeと演者一覧からCinemachine Target Groupを構築します。
    /// </summary>
    internal static class VLiveCameraTargetGroupBuilder
    {
        // Methods

        internal static CinemachineTargetGroup Ensure(
            VLiveCameraRig rig,
            CinemachineTargetGroup current,
            IReadOnlyList<VLivePerformer> performers,
            ShotSize shotSize,
            int slotIndex,
            string shotName,
            Transform container)
        {
            CinemachineTargetGroup group = current;
            if (group == null || !group.transform.IsChildOf(rig.transform))
            {
                var groupObject = new GameObject($"SubjectGroup_Shot {slotIndex + 1} ({shotName})");
                groupObject.transform.SetParent(container, false);
                Undo.RegisterCreatedObjectUndo(groupObject, "Create Subject Group");
                group = groupObject.AddComponent<CinemachineTargetGroup>();
            }

            Undo.RecordObject(group, "Update Subject Group");
            Undo.RecordObject(group.transform, "Update Subject Group Orientation");

            group.PositionMode = CinemachineTargetGroup.PositionModes.GroupCenter;
            group.RotationMode = CinemachineTargetGroup.RotationModes.Manual;
            group.UpdateMethod = CinemachineTargetGroup.UpdateMethods.LateUpdate;
            group.transform.rotation = rig.GetReferenceOrientation();
            group.Targets ??= new List<CinemachineTargetGroup.Target>();
            group.Targets.Clear();

            if (performers != null)
            {
                for (int i = 0; i < performers.Count; i++)
                {
                    VLivePerformer performer = performers[i];
                    if (performer != null)
                    {
                        AddPerformer(group, performer, shotSize);
                    }
                }
            }

            if (group.Targets.Count == 0)
            {
                Debug.LogWarning(
                    $"[VLiveCameraTargetGroupBuilder] Shot {slotIndex + 1} '{shotName}' has no valid Humanoid framing bones. Assign a VLivePerformer with a valid Humanoid Animator.",
                    rig);
            }

            group.DoUpdate();
            EditorUtility.SetDirty(group);
            return group;
        }

        internal static void ConfigureFraming(CinemachineGroupFraming framing, ShotSize shotSize)
        {
            if (framing == null)
            {
                return;
            }

            framing.FramingMode = CinemachineGroupFraming.FramingModes.HorizontalAndVertical;
            framing.FramingSize = GetFramingSize(shotSize);
            framing.Damping = 1f;
            framing.SizeAdjustment = CinemachineGroupFraming.SizeAdjustmentModes.DollyOnly;
            framing.LateralAdjustment = CinemachineGroupFraming.LateralAdjustmentModes.ChangeRotation;
            framing.DollyRange = new Vector2(-5f, 5f);
        }

        private static void AddPerformer(
            CinemachineTargetGroup group,
            VLivePerformer performer,
            ShotSize shotSize)
        {
            if (performer.TryGetHead(out Transform head))
            {
                float headRadius = shotSize == ShotSize.FaceUp
                    ? performer.HeadRadius
                    : performer.HeadRadius * 1.1f;
                AddUnique(group, head, 1.2f, headRadius);
            }

            if (shotSize == ShotSize.FaceUp)
            {
                return;
            }

            if (performer.TryGetBust(out Transform bust))
            {
                float radius = shotSize == ShotSize.CloseUp
                    ? performer.BustRadius * 0.6f
                    : performer.BustRadius;
                AddUnique(group, bust, 1f, radius);
            }

            if (shotSize != ShotSize.Full && shotSize != ShotSize.Wide)
            {
                return;
            }

            if (performer.TryGetHips(out Transform hips))
            {
                AddUnique(group, hips, 0.9f, performer.BodyRadius);
            }
        }

        private static void AddUnique(
            CinemachineTargetGroup group,
            Transform target,
            float weight,
            float radius)
        {
            int existingIndex = group.FindMember(target);
            if (existingIndex >= 0)
            {
                CinemachineTargetGroup.Target existing = group.Targets[existingIndex];
                existing.Weight = Mathf.Max(existing.Weight, weight);
                existing.Radius = Mathf.Max(existing.Radius, radius);
                return;
            }

            group.AddMember(target, weight, radius);
        }

        private static float GetFramingSize(ShotSize shotSize)
        {
            return shotSize switch
            {
                ShotSize.Wide => 0.55f,
                ShotSize.Full => 0.72f,
                ShotSize.BustUp => 0.8f,
                ShotSize.CloseUp => 0.9f,
                ShotSize.FaceUp => 1f,
                _ => 0.8f
            };
        }
    }
}
