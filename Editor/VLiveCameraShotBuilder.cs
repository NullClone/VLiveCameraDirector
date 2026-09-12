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
        internal static void BuildNew(
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

            string aimProxyName = $"AimProxy_Shot {slotIndex + 1} ({preset.DisplayName})";
            var aimProxyGo = new GameObject(aimProxyName);
            aimProxyGo.transform.SetParent(aimProxiesContainer, false);
            Undo.RegisterCreatedObjectUndo(aimProxyGo, "Create Aim Proxy GameObject");

            Quaternion orientation = rig.GetReferenceOrientation();
            Vector3 targetPos = rig.PerformerTarget.position;
            Vector3 initialAimPos = targetPos + Vector3.up * rig.TargetHeight + orientation * preset.AimOffset;
            aimProxyGo.transform.position = initialAimPos;

            MotionKnot[] knots = preset.Knots;
            if (knots == null || knots.Length == 0)
            {
                Debug.LogError($"[VLiveCameraRigBuilder] Preset '{preset.DisplayName}' にKnotが定義されていないため、正常なカメラ配置を行えません。");
            }

            Vector3 p0 = (knots != null && knots.Length > 0) ? knots[0].Position : new Vector3(0f, rig.TargetHeight, 3.5f);
            Vector3 scaledP0 = VLiveCameraMotionSpace.ScaleStartPosition(p0, rig.DistanceScale);
            Vector3 worldP0 = targetPos + orientation * scaledP0;

            shotGo.transform.position = worldP0;
            shotGo.transform.LookAt(initialAimPos);

            var cmCam = shotGo.AddComponent<CinemachineCamera>();
            cmCam.Target.TrackingTarget = rig.PerformerTarget;
            cmCam.Target.LookAtTarget = aimProxyGo.transform;
            cmCam.Lens.FieldOfView = preset.FieldOfView;
            cmCam.Priority = (slotIndex == 0) ? 10 : 0;
            EditorUtility.SetDirty(cmCam);

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

            CinemachineSplineDolly dolly = null;
            if (preset.ShotType == VLiveCameraShotType.Spline)
            {
                string splineGoName = $"SplinePath_Shot {slotIndex + 1} ({preset.DisplayName})";
                var splineGo = new GameObject(splineGoName);
                splineGo.transform.SetParent(splinesContainer, false);
                Undo.RegisterCreatedObjectUndo(splineGo, "Create Spline GameObject");

                var splineContainer = splineGo.AddComponent<SplineContainer>();
                VLiveCameraSplineBuilder.Populate(splineContainer, rig, preset);

                dolly = shotGo.AddComponent<CinemachineSplineDolly>();
                dolly.Spline = splineContainer;
                dolly.PositionUnits = PathIndexUnit.Distance;
                dolly.CameraPosition = 0f;
                ApplyDollyRotationMode(dolly, preset.RollMode);
                EditorUtility.SetDirty(dolly);
            }

            var player = shotGo.AddComponent<VLiveCameraMotionPlayer>();
            var shot = shotGo.AddComponent<VLiveCameraShot>();

            shot.Configure(
                preset.DisplayName,
                rig,
                cmCam,
                preset.ShotType,
                dolly,
                composer,
                aimProxyGo.transform,
                player,
                rig.PerformerTarget,
                preset,
                rig.TargetHeight,
                orientation,
                rig.DistanceScale,
                rig.MotionScale,
                rig.VerticalMotionScale
            );
            EditorUtility.SetDirty(shot);
            EditorUtility.SetDirty(player);

            slot.SetShot(shot);
        }

        internal static int RepairReferences(
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
                shot.SetAimProxy(aimProxy);
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
            if (shot.Type == VLiveCameraShotType.Spline)
            {
                if (dolly == null)
                {
                    dolly = Undo.AddComponent<CinemachineSplineDolly>(shot.gameObject);
                    dolly.PositionUnits = PathIndexUnit.Distance;
                    RollMode rollMode = shot.AppliedMotion != null
                        ? shot.AppliedMotion.RollMode
                        : RollMode.MaintainHorizon;
                    ApplyDollyRotationMode(dolly, rollMode);
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
                        VLiveCameraSplineBuilder.Populate(splineContainer, rig, shot.AppliedPreset);
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

        internal static void Rebuild(
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

            Transform aimProxy = shot.AimProxy;
            if (aimProxy == null)
            {
                string aimProxyName = $"AimProxy_Shot {slotIndex + 1} ({preset.DisplayName})";
                var aimProxyGo = new GameObject(aimProxyName);
                aimProxyGo.transform.SetParent(aimProxiesContainer, false);
                Undo.RegisterCreatedObjectUndo(aimProxyGo, "Create Aim Proxy GameObject");
                aimProxy = aimProxyGo.transform;
                shot.SetAimProxy(aimProxy);
            }

            Undo.RecordObject(aimProxy, "Update Aim Proxy Position");
            Vector3 aimPos = targetPos + Vector3.up * rig.TargetHeight + orientation * preset.AimOffset;
            aimProxy.position = aimPos;

            MotionKnot[] knots = preset.Knots;
            if (knots == null || knots.Length == 0)
            {
                Debug.LogError($"[VLiveCameraRigBuilder] Preset '{preset.DisplayName}' にKnotが定義されていないため、正常なカメラ配置を行えません。");
            }

            Vector3 p0 = (knots != null && knots.Length > 0) ? knots[0].Position : new Vector3(0f, rig.TargetHeight, 3.5f);
            Vector3 scaledP0 = VLiveCameraMotionSpace.ScaleStartPosition(p0, rig.DistanceScale);
            Vector3 worldP0 = targetPos + orientation * scaledP0;

            Undo.RecordObject(shot.transform, "Rebuild Shot Transform");
            shot.transform.position = worldP0;
            shot.transform.LookAt(aimPos);

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

            CinemachineSplineDolly dolly = shot.SplineDolly;
            float resolvedSplineLength = 0f;

            if (preset.ShotType == VLiveCameraShotType.Spline)
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
                VLiveCameraSplineBuilder.Populate(splineContainer, rig, preset);
                resolvedSplineLength = splineContainer.Spline != null ? splineContainer.Spline.GetLength() : 0f;

                if (dolly == null)
                {
                    dolly = Undo.AddComponent<CinemachineSplineDolly>(shot.gameObject);
                }

                Undo.RecordObject(dolly, "Rebuild Spline Dolly");
                dolly.Spline = splineContainer;
                dolly.PositionUnits = PathIndexUnit.Distance;
                dolly.CameraPosition = 0f;
                ApplyDollyRotationMode(dolly, preset.RollMode);
                EditorUtility.SetDirty(dolly);
            }
            else
            {
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

            VLiveCameraMotionPlayer player = shot.MotionPlayer;
            if (player == null)
            {
                player = Undo.AddComponent<VLiveCameraMotionPlayer>(shot.gameObject);
            }

            shot.Configure(
                preset.DisplayName,
                rig,
                cmCam,
                preset.ShotType,
                dolly,
                composer,
                aimProxy,
                player,
                rig.PerformerTarget,
                preset,
                rig.TargetHeight,
                orientation,
                rig.DistanceScale,
                rig.MotionScale,
                rig.VerticalMotionScale
            );

            player.Configure(shot, cmCam, dolly, composer, aimProxy);
            player.PrepareStart();

            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(shot);
        }

        private static void ApplyDollyRotationMode(CinemachineSplineDolly dolly, RollMode rollMode)
        {
            dolly.CameraRotation = rollMode == RollMode.SplineUp
                ? CinemachineSplineDolly.RotationMode.Spline
                : CinemachineSplineDolly.RotationMode.Default;
        }
    }
}
