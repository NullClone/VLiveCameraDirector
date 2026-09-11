using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// Scene上で調整されたShot（Camera、Spline、Aim Proxy、適用済みMotion設定）から
    /// 新しい再利用可能Motion Preset Assetを生成・保存するEditor専用ベイカー。
    /// </summary>
    public static class VLiveCameraPresetBaker
    {
        // Fields

        public const string DefaultPresetFolder = "Assets/toshi.VLiveKit/VLiveCameraUnit/Presets";


        // Methods

        /// <summary>
        /// Scene上のShotから新しいMotion Preset Assetを作成・保存します。
        /// 既存Assetを暗黙に上書きせず、ユニークなパスで新規作成します。
        /// </summary>
        /// <param name="rig">所属するVLiveCameraRig。</param>
        /// <param name="shot">元となるVLiveCameraShot。</param>
        /// <param name="targetAssetPath">保存先パス（省略時はPresetsフォルダ内に自動生成）。</param>
        /// <param name="showConfirmationDialog">保存実行前に確認ダイアログを表示するかどうか。</param>
        /// <returns>生成されたVLiveCameraMotionPreset。</returns>
        public static VLiveCameraMotionPreset BakePresetFromShot(
            VLiveCameraRig rig,
            VLiveCameraShot shot,
            string targetAssetPath = null,
            bool showConfirmationDialog = true)
        {
            if (shot == null)
            {
                Debug.LogError("[VLiveCameraPresetBaker] Shot が指定されていないため Preset を作成できません。");
                return null;
            }

            Transform targetTransform = (shot.PerformerTarget != null) ? shot.PerformerTarget : (rig != null ? rig.PerformerTarget : null);
            Vector3 targetPos = (targetTransform != null) ? targetTransform.position : Vector3.zero;

            Quaternion orientation = (shot.AppliedRigOrientation != Quaternion.identity)
                ? shot.AppliedRigOrientation
                : ((rig != null) ? rig.GetReferenceOrientation() : Quaternion.identity);

            float distanceScale = (shot.AppliedDistanceScale > 0.001f)
                ? shot.AppliedDistanceScale
                : ((rig != null && rig.DistanceScale > 0.001f) ? rig.DistanceScale : 1.0f);

            float motionScale = (shot.AppliedMotionScale > 0.001f)
                ? shot.AppliedMotionScale
                : ((rig != null && rig.MotionScale > 0.001f) ? rig.MotionScale : 1.0f);

            float targetHeight = (shot.AppliedTargetHeight > 0.001f)
                ? shot.AppliedTargetHeight
                : ((rig != null) ? rig.TargetHeight : 1.4f);

            VLiveCameraAppliedMotion applied = shot.AppliedMotion ?? new VLiveCameraAppliedMotion();

            // 1. Body Track (Spline Knots)
            MotionKnot[] knots;
            float refSplineLength = 0f;
            bool isClosed = false;

            if (shot.Type == VLiveCameraShot.ShotType.Spline && shot.SplineDolly != null && shot.SplineDolly.Spline != null)
            {
                SplineContainer container = shot.SplineDolly.Spline;
                Spline spline = container.Spline;
                isClosed = spline.Closed;

                int knotCount = spline.Count;
                knots = new MotionKnot[knotCount];

                if (knotCount > 0)
                {
                    Vector3 worldP0 = container.transform.TransformPoint((Vector3)spline[0].Position);
                    Vector3 relWorld0 = worldP0 - targetPos;
                    Vector3 scaledP0 = Quaternion.Inverse(orientation) * relWorld0;
                    Vector3 p0 = new Vector3(scaledP0.x / distanceScale, scaledP0.y, scaledP0.z / distanceScale);

                    for (int i = 0; i < knotCount; i++)
                    {
                        Vector3 pi;
                        if (i == 0)
                        {
                            pi = p0;
                        }
                        else
                        {
                            Vector3 worldPi = container.transform.TransformPoint((Vector3)spline[i].Position);
                            Vector3 relWorldPi = worldPi - targetPos;
                            Vector3 scaledPi = Quaternion.Inverse(orientation) * relWorldPi;
                            Vector3 deltaScaled = scaledPi - scaledP0;
                            pi = p0 + deltaScaled / motionScale;
                        }

                        // Tangents: BezierKnotのTangentIn/Outはknot.Rotationのローカル空間で定義されているため、
                        // orientationによる回転を重ねて逆変換せず、MotionScaleのみを逆適用する
                        Vector3 presetTanIn = ((Vector3)spline[i].TangentIn) / motionScale;
                        Vector3 presetTanOut = ((Vector3)spline[i].TangentOut) / motionScale;

                        TangentMode mode = spline.GetTangentMode(i);
                        Quaternion worldRot = container.transform.rotation * (Quaternion)spline[i].Rotation;
                        Quaternion presetRot = Quaternion.Inverse(orientation) * worldRot;

                        knots[i] = new MotionKnot(pi, presetTanIn, presetTanOut, mode, presetRot);
                    }

                    // 基準スプライン長はベイク後の未スケールknot列から正確に再計算
                    var unscaledSpline = new Spline();
                    for (int i = 0; i < knotCount; i++)
                    {
                        unscaledSpline.Add(new BezierKnot(knots[i].Position, knots[i].TangentIn, knots[i].TangentOut, knots[i].Rotation), knots[i].TangentMode);
                    }
                    unscaledSpline.Closed = isClosed;
                    refSplineLength = unscaledSpline.GetLength();
                }
            }
            else
            {
                // Fixed Shot
                Vector3 camWorld = shot.CinemachineCamera != null ? shot.CinemachineCamera.transform.position : shot.transform.position;
                Vector3 relWorld = camWorld - targetPos;
                Vector3 scaledP0 = Quaternion.Inverse(orientation) * relWorld;
                Vector3 p0 = new Vector3(scaledP0.x / distanceScale, scaledP0.y, scaledP0.z / distanceScale);

                Quaternion camRot = shot.CinemachineCamera != null ? shot.CinemachineCamera.transform.rotation : shot.transform.rotation;
                Quaternion presetRot = Quaternion.Inverse(orientation) * camRot;

                knots = new MotionKnot[]
                {
                    new MotionKnot(p0, Vector3.zero, Vector3.zero, TangentMode.Linear, presetRot)
                };
                refSplineLength = 0f;
            }

            // 2. Aim Offset
            Vector3 presetAimOffset = applied.AimOffset;
            if (shot.AimProxy != null)
            {
                Vector3 aimWorld = shot.AimProxy.position;
                Vector3 relAim = aimWorld - targetPos;
                Vector3 aboveHeight = relAim - Vector3.up * targetHeight;
                presetAimOffset = Quaternion.Inverse(orientation) * aboveHeight;
            }

            // 3. Screen Position & RotationComposer Settings
            Vector2 screenPos = applied.ScreenPosition;
            bool deadZoneEnabled = applied.DeadZoneEnabled;
            Vector2 deadZoneSize = applied.DeadZoneSize;
            bool hardLimitsEnabled = applied.HardLimitsEnabled;
            Vector2 hardLimitsSize = applied.HardLimitsSize;
            Vector2 hardLimitsOffset = applied.HardLimitsOffset;
            Vector2 damping = applied.Damping;
            bool lookaheadEnabled = applied.LookaheadEnabled;
            float lookaheadTime = applied.LookaheadTime;
            float lookaheadSmoothing = applied.LookaheadSmoothing;
            bool centerOnActivate = applied.CenterOnActivate;

            if (shot.RotationComposer != null)
            {
                screenPos = shot.RotationComposer.Composition.ScreenPosition;
                deadZoneEnabled = shot.RotationComposer.Composition.DeadZone.Enabled;
                deadZoneSize = shot.RotationComposer.Composition.DeadZone.Size;
                hardLimitsEnabled = shot.RotationComposer.Composition.HardLimits.Enabled;
                hardLimitsSize = shot.RotationComposer.Composition.HardLimits.Size;
                hardLimitsOffset = shot.RotationComposer.Composition.HardLimits.Offset;
                damping = shot.RotationComposer.Damping;
                lookaheadEnabled = shot.RotationComposer.Lookahead.Enabled;
                lookaheadTime = shot.RotationComposer.Lookahead.Time;
                lookaheadSmoothing = shot.RotationComposer.Lookahead.Smoothing;
                centerOnActivate = shot.RotationComposer.CenterOnActivate;
            }

            // 4. Lens Settings
            LensMode lensMode = applied.LensMode;
            float fov = applied.FieldOfView;
            float focalLength = applied.FocalLength;
            Vector2 sensorSize = applied.SensorSize;

            if (shot.CinemachineCamera != null)
            {
                fov = shot.CinemachineCamera.Lens.FieldOfView;
                if (shot.CinemachineCamera.Lens.PhysicalProperties.SensorSize.sqrMagnitude > 0.01f)
                {
                    sensorSize = shot.CinemachineCamera.Lens.PhysicalProperties.SensorSize;
                }

                if (shot.CinemachineCamera.Lens.ModeOverride == LensSettings.OverrideModes.Physical)
                {
                    lensMode = LensMode.FocalLength;
                    if (sensorSize.y > 0.001f)
                    {
                        focalLength = UnityEngine.Camera.FieldOfViewToFocalLength(fov, sensorSize.y);
                    }
                }
                else if (shot.CinemachineCamera.Lens.ModeOverride == LensSettings.OverrideModes.Perspective)
                {
                    lensMode = LensMode.FieldOfView;
                }
                else if (lensMode == LensMode.FocalLength && sensorSize.y > 0.001f)
                {
                    focalLength = UnityEngine.Camera.FieldOfViewToFocalLength(fov, sensorSize.y);
                }
            }

            // 5. Identity & Intent
            ShotSize shotSize = (shot.AppliedPreset != null) ? shot.AppliedPreset.Size : ShotSize.Medium;
            ShotEnergy energy = (shot.AppliedPreset != null) ? shot.AppliedPreset.Energy : ShotEnergy.Normal;
            MotionFamily family = (shot.AppliedPreset != null) ? shot.AppliedPreset.Family : applied.Family;
            VLiveCameraRigProfile rigProfile = (shot.AppliedPreset != null) ? shot.AppliedPreset.RigProfile : null;

            string displayName = shot.ShotName + " (Baked)";
            string description = (shot.AppliedPreset != null)
                ? $"Baked from shot '{shot.ShotName}' based on '{shot.AppliedPreset.DisplayName}'"
                : $"Baked from scene Shot '{shot.ShotName}'";

            // 6. Asset Path
            if (string.IsNullOrEmpty(targetAssetPath))
            {
                if (!Directory.Exists(DefaultPresetFolder))
                {
                    Directory.CreateDirectory(DefaultPresetFolder);
                }

                string safeShotName = string.IsNullOrEmpty(shot.ShotName) ? "NewPreset" : shot.ShotName.Replace(" ", "_");
                targetAssetPath = $"{DefaultPresetFolder}/{safeShotName}_Preset.asset";
            }

            targetAssetPath = AssetDatabase.GenerateUniqueAssetPath(targetAssetPath);

            // 7. 確認ダイアログ (Source Shotと保存先パスを表示)
            if (showConfirmationDialog)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Bake Preset (プリセット保存確認)",
                    $"Source Shot: {shot.ShotName}\n保存先パス: {targetAssetPath}\n\nこのShotから新規Motion Presetを生成して保存しますか？",
                    "保存 (Save)",
                    "キャンセル (Cancel)"
                );

                if (!proceed)
                {
                    return null;
                }
            }

            // 8. Presetインスタンス作成と完全保存
            var preset = ScriptableObject.CreateInstance<VLiveCameraMotionPreset>();
            preset.Initialize(
                displayName,
                shot.Type,
                family,
                shotSize,
                energy,
                description,
                rigProfile,
                knots,
                isClosed,
                refSplineLength,
                applied.ClipDuration,
                applied.ProgressCurve,
                applied.ScaleMode,
                applied.MinSpeedMultiplier,
                applied.MaxSpeedMultiplier,
                applied.SpeedStep,
                presetAimOffset,
                applied.AimOffsetXCurve,
                applied.AimOffsetYCurve,
                applied.AimOffsetZCurve,
                screenPos,
                applied.ScreenPositionXCurve,
                applied.ScreenPositionYCurve,
                deadZoneEnabled,
                deadZoneSize,
                hardLimitsEnabled,
                hardLimitsSize,
                hardLimitsOffset,
                damping,
                lookaheadEnabled,
                lookaheadTime,
                lookaheadSmoothing,
                centerOnActivate,
                lensMode,
                fov,
                applied.FieldOfViewCurve,
                focalLength,
                applied.FocalLengthCurve,
                sensorSize,
                applied.RollMode,
                applied.RollCurve,
                applied.EntryMode,
                applied.InTime,
                applied.OutTime,
                applied.ExitBehavior,
                applied.StartDistance,
                applied.EndDistance
            );

            AssetDatabase.CreateAsset(preset, targetAssetPath);
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssetIfDirty(preset);

            Debug.Log($"[VLiveCameraPresetBaker] Shot '{shot.ShotName}' から新しい Motion Preset を保存しました: {targetAssetPath}");
            return preset;
        }
    }
}
