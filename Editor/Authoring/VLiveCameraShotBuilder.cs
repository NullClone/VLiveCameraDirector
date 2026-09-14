using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// Motion PresetからScene上の専用Shotを生成、修復、再構築します。
    /// </summary>
    internal static class VLiveCameraShotBuilder
    {
        // Methods

        internal static void BuildNew(
            VLiveCameraRig rig,
            VLiveCameraMotionPreset preset,
            int slotIndex,
            Transform shotsContainer,
            Transform splinesContainer,
            Transform targetGroupsContainer,
            VLiveCameraShotSlot slot)
        {
            string shotObjectName = $"Shot {slotIndex + 1} ({preset.DisplayName})";
            var shotObject = new GameObject(shotObjectName);
            shotObject.transform.SetParent(shotsContainer, false);
            Undo.RegisterCreatedObjectUndo(shotObject, "Create Shot GameObject");

            CinemachineTargetGroup targetGroup = VLiveCameraTargetGroupBuilder.Ensure(
                rig, null, slot.Performers, preset.Size, slotIndex, preset.DisplayName, targetGroupsContainer);
            targetGroup.DoUpdate();

            Quaternion orientation = rig.GetReferenceOrientation();
            Vector3 referencePosition = rig.GetReferencePosition();
            Vector3 aimPosition = targetGroup.Targets.Count > 0
                ? targetGroup.transform.position + orientation * preset.AimOffset
                : referencePosition + orientation * preset.AimOffset;

            MotionKnot[] knots = preset.Knots;
            if (knots == null || knots.Length == 0)
            {
                Debug.LogError($"[VLiveCameraShotBuilder] Preset '{preset.DisplayName}' has no knots. A fallback camera position will be used.");
            }

            Vector3 firstPosition = knots != null && knots.Length > 0
                ? knots[0].Position
                : new Vector3(0f, 1.35f, 3.5f);
            Vector3 scaledFirstPosition = VLiveCameraMotionSpace.ScaleStartPosition(firstPosition, rig.DistanceScale);
            shotObject.transform.position = referencePosition + orientation * scaledFirstPosition;
            shotObject.transform.LookAt(aimPosition);

            CinemachineCamera camera = shotObject.AddComponent<CinemachineCamera>();
            ConfigureCameraTargets(camera, targetGroup);
            VLiveCameraRigBuilder.ApplyCommonPhysicalSettings(rig, camera);
            ApplyPresetLens(camera, preset, rig.SensorSize.y);
            camera.Priority = slotIndex == 0 ? 10 : 0;

            CinemachineRotationComposer composer = shotObject.AddComponent<CinemachineRotationComposer>();
            ConfigureComposer(composer, preset);

            CinemachineGroupFraming groupFraming = shotObject.AddComponent<CinemachineGroupFraming>();
            VLiveCameraTargetGroupBuilder.ConfigureFraming(groupFraming, preset.Size);
            groupFraming.CenterOffset = preset.ScreenPosition;

            CinemachineSplineDolly dolly = null;
            if (preset.ShotType == VLiveCameraShotType.Spline)
            {
                string splineObjectName = $"SplinePath_Shot {slotIndex + 1} ({preset.DisplayName})";
                var splineObject = new GameObject(splineObjectName);
                splineObject.transform.SetParent(splinesContainer, false);
                Undo.RegisterCreatedObjectUndo(splineObject, "Create Spline GameObject");

                SplineContainer splineContainer = splineObject.AddComponent<SplineContainer>();
                VLiveCameraSplineBuilder.Populate(splineContainer, rig, preset);

                dolly = shotObject.AddComponent<CinemachineSplineDolly>();
                dolly.Spline = splineContainer;
                dolly.PositionUnits = PathIndexUnit.Distance;
                dolly.CameraPosition = 0f;
                ApplyDollyRotationMode(dolly, preset.RollMode);
            }

            VLiveCameraMotionPlayer player = shotObject.AddComponent<VLiveCameraMotionPlayer>();
            VLiveCameraShot shot = shotObject.AddComponent<VLiveCameraShot>();

            shot.Configure(
                preset.DisplayName, rig, camera, preset.ShotType, dolly, composer,
                targetGroup, groupFraming, player, slot.Performers, preset, orientation,
                rig.DistanceScale, rig.MotionScale, rig.VerticalMotionScale);

            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(composer);
            EditorUtility.SetDirty(groupFraming);
            if (dolly != null)
            {
                EditorUtility.SetDirty(dolly);
            }
            EditorUtility.SetDirty(shot);
            EditorUtility.SetDirty(player);
            slot.SetShot(shot);
        }

        internal static int RepairReferences(
            VLiveCameraRig rig,
            VLiveCameraShot shot,
            VLiveCameraShotSlot slot,
            int slotIndex,
            Transform targetGroupsContainer,
            Transform splinesContainer)
        {
            int repaired = 0;
            Undo.RecordObject(shot, "Repair Shot References");

            CinemachineTargetGroup targetGroup = VLiveCameraTargetGroupBuilder.Ensure(
                rig, shot.TargetGroup, slot.Performers, shot.Size, slotIndex, shot.ShotName, targetGroupsContainer);
            if (targetGroup != shot.TargetGroup)
            {
                repaired++;
            }

            CinemachineCamera camera = shot.CinemachineCamera;
            if (camera == null)
            {
                camera = Undo.AddComponent<CinemachineCamera>(shot.gameObject);
                repaired++;
            }

            Undo.RecordObject(camera, "Update CinemachineCamera Targets");
            ConfigureCameraTargets(camera, targetGroup);

            CinemachineRotationComposer composer = shot.RotationComposer;
            if (composer == null)
            {
                composer = Undo.AddComponent<CinemachineRotationComposer>(shot.gameObject);
                composer.TargetOffset = Vector3.zero;
                repaired++;
            }

            CinemachineGroupFraming groupFraming = shot.GroupFraming;
            if (groupFraming == null)
            {
                groupFraming = Undo.AddComponent<CinemachineGroupFraming>(shot.gameObject);
                VLiveCameraTargetGroupBuilder.ConfigureFraming(groupFraming, shot.Size);
                repaired++;
            }

            CinemachineSplineDolly dolly = shot.SplineDolly;
            if (shot.Type == VLiveCameraShotType.Spline)
            {
                if (dolly == null)
                {
                    dolly = Undo.AddComponent<CinemachineSplineDolly>(shot.gameObject);
                    dolly.PositionUnits = PathIndexUnit.Distance;
                    ApplyDollyRotationMode(dolly, shot.AppliedMotion != null
                        ? shot.AppliedMotion.RollMode
                        : RollMode.MaintainHorizon);
                    repaired++;
                }

                if (dolly.Spline == null)
                {
                    string splineObjectName = $"SplinePath_Shot {slotIndex + 1} ({shot.ShotName})";
                    var splineObject = new GameObject(splineObjectName);
                    splineObject.transform.SetParent(splinesContainer, false);
                    Undo.RegisterCreatedObjectUndo(splineObject, "Create Repaired Spline");

                    SplineContainer splineContainer = splineObject.AddComponent<SplineContainer>();
                    if (shot.AppliedPreset != null)
                    {
                        VLiveCameraSplineBuilder.Populate(splineContainer, rig, shot.AppliedPreset);
                    }

                    dolly.Spline = splineContainer;
                    dolly.PositionUnits = PathIndexUnit.Distance;
                    repaired++;
                }

                if (dolly.PositionUnits != PathIndexUnit.Distance)
                {
                    Undo.RecordObject(dolly, "Set Spline Dolly Position Units");
                    dolly.PositionUnits = PathIndexUnit.Distance;
                }
            }

            VLiveCameraMotionPlayer player = shot.MotionPlayer;
            if (player == null)
            {
                player = Undo.AddComponent<VLiveCameraMotionPlayer>(shot.gameObject);
                repaired++;
            }

            shot.SetCompositionReferences(targetGroup, groupFraming, slot.Performers, shot.Size);
            player.Configure(shot, camera, dolly, composer, groupFraming);

            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(composer);
            EditorUtility.SetDirty(groupFraming);
            if (dolly != null)
            {
                EditorUtility.SetDirty(dolly);
            }
            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(shot);
            return repaired;
        }

        internal static void Rebuild(
            VLiveCameraRig rig,
            VLiveCameraMotionPreset preset,
            VLiveCameraShotSlot slot,
            int slotIndex,
            VLiveCameraShot shot,
            Transform targetGroupsContainer,
            Transform splinesContainer)
        {
            Undo.RecordObject(shot, "Rebuild Shot");

            CinemachineTargetGroup targetGroup = VLiveCameraTargetGroupBuilder.Ensure(
                rig, shot.TargetGroup, slot.Performers, preset.Size, slotIndex, preset.DisplayName, targetGroupsContainer);
            targetGroup.DoUpdate();

            Quaternion orientation = rig.GetReferenceOrientation();
            Vector3 referencePosition = rig.GetReferencePosition();
            Vector3 aimPosition = targetGroup.Targets.Count > 0
                ? targetGroup.transform.position + orientation * preset.AimOffset
                : referencePosition + orientation * preset.AimOffset;

            MotionKnot[] knots = preset.Knots;
            if (knots == null || knots.Length == 0)
            {
                Debug.LogError($"[VLiveCameraShotBuilder] Preset '{preset.DisplayName}' has no knots. A fallback camera position will be used.");
            }

            Vector3 firstPosition = knots != null && knots.Length > 0
                ? knots[0].Position
                : new Vector3(0f, 1.35f, 3.5f);
            Vector3 scaledFirstPosition = VLiveCameraMotionSpace.ScaleStartPosition(firstPosition, rig.DistanceScale);

            Undo.RecordObject(shot.transform, "Rebuild Shot Transform");
            shot.transform.position = referencePosition + orientation * scaledFirstPosition;
            shot.transform.LookAt(aimPosition);

            CinemachineCamera camera = shot.CinemachineCamera;
            if (camera == null)
            {
                camera = Undo.AddComponent<CinemachineCamera>(shot.gameObject);
            }

            Undo.RecordObject(camera, "Rebuild CinemachineCamera");
            ConfigureCameraTargets(camera, targetGroup);
            VLiveCameraRigBuilder.ApplyCommonPhysicalSettings(rig, camera);
            ApplyPresetLens(camera, preset, rig.SensorSize.y);

            CinemachineRotationComposer composer = shot.RotationComposer;
            if (composer == null)
            {
                composer = Undo.AddComponent<CinemachineRotationComposer>(shot.gameObject);
            }

            Undo.RecordObject(composer, "Rebuild Rotation Composer");
            ConfigureComposer(composer, preset);

            CinemachineGroupFraming groupFraming = shot.GroupFraming;
            if (groupFraming == null)
            {
                groupFraming = Undo.AddComponent<CinemachineGroupFraming>(shot.gameObject);
            }

            Undo.RecordObject(groupFraming, "Rebuild Group Framing");
            VLiveCameraTargetGroupBuilder.ConfigureFraming(groupFraming, preset.Size);
            groupFraming.CenterOffset = preset.ScreenPosition;

            CinemachineSplineDolly dolly = RebuildSplineDolly(rig, preset, slotIndex, shot, splinesContainer);

            VLiveCameraMotionPlayer player = shot.MotionPlayer;
            if (player == null)
            {
                player = Undo.AddComponent<VLiveCameraMotionPlayer>(shot.gameObject);
            }

            shot.Configure(
                preset.DisplayName, rig, camera, preset.ShotType, dolly, composer,
                targetGroup, groupFraming, player, slot.Performers, preset, orientation,
                rig.DistanceScale, rig.MotionScale, rig.VerticalMotionScale);
            player.Configure(shot, camera, dolly, composer, groupFraming);
            player.PrepareStart();

            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(composer);
            EditorUtility.SetDirty(groupFraming);
            if (dolly != null)
            {
                EditorUtility.SetDirty(dolly);
            }
            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(shot);
        }

        private static CinemachineSplineDolly RebuildSplineDolly(
            VLiveCameraRig rig,
            VLiveCameraMotionPreset preset,
            int slotIndex,
            VLiveCameraShot shot,
            Transform splinesContainer)
        {
            CinemachineSplineDolly dolly = shot.SplineDolly;
            if (preset.ShotType != VLiveCameraShotType.Spline)
            {
                if (dolly != null)
                {
                    if (dolly.Spline != null && dolly.Spline.transform.IsChildOf(rig.transform))
                    {
                        Undo.DestroyObjectImmediate(dolly.Spline.gameObject);
                    }

                    Undo.DestroyObjectImmediate(dolly);
                }

                return null;
            }

            SplineContainer splineContainer;
            if (dolly != null && dolly.Spline != null && dolly.Spline.transform.IsChildOf(rig.transform))
            {
                splineContainer = dolly.Spline;
            }
            else
            {
                string splineObjectName = $"SplinePath_Shot {slotIndex + 1} ({preset.DisplayName})";
                var splineObject = new GameObject(splineObjectName);
                splineObject.transform.SetParent(splinesContainer, false);
                Undo.RegisterCreatedObjectUndo(splineObject, "Create Spline GameObject");
                splineContainer = splineObject.AddComponent<SplineContainer>();
            }

            Undo.RecordObject(splineContainer, "Rebuild Spline Knots");
            VLiveCameraSplineBuilder.Populate(splineContainer, rig, preset);

            if (dolly == null)
            {
                dolly = Undo.AddComponent<CinemachineSplineDolly>(shot.gameObject);
            }

            Undo.RecordObject(dolly, "Rebuild Spline Dolly");
            dolly.Spline = splineContainer;
            dolly.PositionUnits = PathIndexUnit.Distance;
            dolly.CameraPosition = 0f;
            ApplyDollyRotationMode(dolly, preset.RollMode);
            return dolly;
        }

        private static void ConfigureCameraTargets(CinemachineCamera camera, CinemachineTargetGroup targetGroup)
        {
            camera.Target.TrackingTarget = targetGroup != null ? targetGroup.transform : null;
            camera.Target.LookAtTarget = null;
            camera.Target.CustomLookAtTarget = false;
        }

        private static void ConfigureComposer(CinemachineRotationComposer composer, VLiveCameraMotionPreset preset)
        {
            composer.TargetOffset = preset.AimOffset;
            composer.Composition.ScreenPosition = Vector2.zero;
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
        }

        private static void ApplyPresetLens(CinemachineCamera camera, VLiveCameraMotionPreset preset, float sensorHeight)
        {
            LensSettings lens = camera.Lens;
            lens.FieldOfView = preset.LensMode == LensMode.FocalLength
                ? UnityEngine.Camera.FocalLengthToFieldOfView(preset.FocalLength, Mathf.Max(0.1f, sensorHeight))
                : preset.FieldOfView;
            camera.Lens = lens;
        }

        private static void ApplyDollyRotationMode(CinemachineSplineDolly dolly, RollMode rollMode)
        {
            dolly.CameraRotation = rollMode == RollMode.SplineUp
                ? CinemachineSplineDolly.RotationMode.Spline
                : CinemachineSplineDolly.RotationMode.Default;
        }
    }
}
