using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// PresetまたはShotの運動値（速度、加速度、Jerk）、構図、Progress Curve、In/Out Pointを診断するEditor専用バリデータ。
    /// 診断結果のみを返し、AssetやSceneを暗黙に変更しません。
    /// </summary>
    public static class VLiveCameraMotionValidator
    {
        // Fields

        private const int SampleCount = 60;


        // Methods

        /// <summary>
        /// Scene上のShotインスタンスをサンプリング診断します。
        /// </summary>
        public static VLiveCameraMotionValidationReport ValidateShot(VLiveCameraShot shot)
        {
            var report = new VLiveCameraMotionValidationReport();

            if (shot == null)
            {
                report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Error, "General", "Shot component is null."));
                report.HasErrors = true;
                return report;
            }

            if (shot.CinemachineCamera == null)
            {
                report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Error, "Camera", "Dedicated CinemachineCamera is unassigned."));
                report.HasErrors = true;
            }

            if (shot.TargetGroup == null || shot.TargetGroup.IsEmpty)
            {
                report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Warning, "Composition", "Subject Target Group has no valid Humanoid framing bones."));
                report.HasWarnings = true;
            }

            VLiveCameraAppliedMotion motion = shot.AppliedMotion;
            if (motion == null)
            {
                report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Error, "Motion", "Applied Motion configuration is missing."));
                report.HasErrors = true;
                return report;
            }

            float splineLen = 0f;
            if (shot.Type == VLiveCameraShotType.Spline)
            {
                if (shot.SplineDolly == null || shot.SplineDolly.Spline == null)
                {
                    report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Error, "Spline", "Spline Dolly or SplineContainer is unassigned."));
                    report.HasErrors = true;
                }
                else
                {
                    var spline = shot.SplineDolly.Spline.Spline;
                    if (spline != null)
                    {
                        splineLen = spline.GetLength();
                    }
                }
            }

            if (shot.CinemachineCamera != null && shot.TargetGroup != null && !shot.TargetGroup.IsEmpty)
            {
                float nearClip = shot.CinemachineCamera.Lens.NearClipPlane;
                BoundingSphere subjectBounds = shot.TargetGroup.Sphere;
                float distanceToBounds = Mathf.Max(
                    0f,
                    Vector3.Distance(shot.CinemachineCamera.transform.position, subjectBounds.position) - subjectBounds.radius);
                if (distanceToBounds <= nearClip)
                {
                    report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Warning, "Composition", $"Distance to subject bounds ({distanceToBounds:F2}m) is within Near Clip Plane ({nearClip:F2}m). Subject may be clipped."));
                    report.HasWarnings = true;
                }
            }

            VLiveCameraRigProfile profile = shot.AppliedPreset != null ? shot.AppliedPreset.RigProfile : null;
            float sensorHeight = shot.CinemachineCamera != null
                ? shot.CinemachineCamera.Lens.PhysicalProperties.SensorSize.y
                : 24f;
            RunMotionDiagnostics(motion, splineLen, profile, report, sensorHeight);
            return report;
        }

        /// <summary>
        /// Motion Preset原本をサンプリング診断します。
        /// </summary>
        public static VLiveCameraMotionValidationReport ValidatePreset(VLiveCameraMotionPreset preset, float optionalSplineLength = 0f)
        {
            var report = new VLiveCameraMotionValidationReport();

            if (preset == null)
            {
                report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Error, "General", "Motion Preset is null."));
                report.HasErrors = true;
                return report;
            }

            float splineLen = optionalSplineLength > 0.001f ? optionalSplineLength : preset.ReferenceSplineLength;
            if (splineLen <= 0.001f && preset.ShotType == VLiveCameraShotType.Spline && preset.Knots != null && preset.Knots.Length > 1)
            {
                for (int i = 1; i < preset.Knots.Length; i++)
                {
                    splineLen += Vector3.Distance(preset.Knots[i - 1].Position, preset.Knots[i].Position);
                }
            }

            var tempMotion = new VLiveCameraAppliedMotion();
            tempMotion.ApplyFromPreset(preset, splineLen, preset.RigProfile);

            RunMotionDiagnostics(tempMotion, splineLen, preset.RigProfile, report, 24f);
            return report;
        }

        private static void RunMotionDiagnostics(
            VLiveCameraAppliedMotion motion,
            float splineLength,
            VLiveCameraRigProfile profile,
            VLiveCameraMotionValidationReport report,
            float sensorHeight)
        {
            report.Duration = motion.EffectiveDuration;
            report.SplineLength = splineLength;
            report.MinFieldOfView = float.MaxValue;
            report.MaxFieldOfView = float.MinValue;

            if (motion.InTime < 0f)
            {
                report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Error, "Timing", $"In Time is negative ({motion.InTime:F2}s)."));
                report.HasErrors = true;
            }

            if (motion.OutTime > motion.EffectiveDuration + 0.001f)
            {
                report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Error, "Timing", $"Out Time ({motion.OutTime:F2}s) exceeds Duration ({motion.EffectiveDuration:F2}s)."));
                report.HasErrors = true;
            }

            if (motion.InTime >= motion.OutTime)
            {
                report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Error, "Timing", $"In Time ({motion.InTime:F2}s) is greater than or equal to Out Time ({motion.OutTime:F2}s)."));
                report.HasErrors = true;
            }

            float recMaxSpeed = profile != null ? profile.RecommendedMaxSpeed : 5.0f;
            float recMaxAcc = profile != null ? profile.RecommendedMaxAcceleration : 10.0f;
            float recMaxJerk = profile != null ? profile.RecommendedMaxJerk : 50.0f;

            float duration = Mathf.Max(0.01f, motion.EffectiveDuration);
            float dt = duration / (SampleCount - 1);

            var distances = new float[SampleCount];
            var velocities = new float[SampleCount];
            var accelerations = new float[SampleCount];
            var jerks = new float[SampleCount];

            float prevProgress = 0f;
            bool monotonicityWarned = false;
            bool compositionInfoAdded = false;

            for (int i = 0; i < SampleCount; i++)
            {
                float t = i * dt;
                VLiveCameraMotionSample sample = VLiveCameraMotionEvaluator.Evaluate(
                    motion,
                    t,
                    splineLength,
                    sensorHeight);

                if (!sample.IsValid)
                {
                    report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Error, "Safety", $"Detected NaN or Infinity value at time {t:F2}s."));
                    report.HasErrors = true;
                }

                if (i > 0 && sample.Progress < prevProgress - 0.005f && !monotonicityWarned)
                {
                    report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Warning, "Curve", $"Progress Curve is non-monotonic (reversal detected at time {t:F2}s)."));
                    report.HasWarnings = true;
                    monotonicityWarned = true;
                }

                prevProgress = sample.Progress;
                distances[i] = sample.SplineDistance;

                if (sample.FieldOfView < report.MinFieldOfView)
                {
                    report.MinFieldOfView = sample.FieldOfView;
                }

                if (sample.FieldOfView > report.MaxFieldOfView)
                {
                    report.MaxFieldOfView = sample.FieldOfView;
                }

                if (!compositionInfoAdded &&
                    (Mathf.Abs(sample.ScreenPosition.x) > 0.5f || Mathf.Abs(sample.ScreenPosition.y) > 0.5f))
                {
                    report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Info, "Composition", $"Screen Position ({sample.ScreenPosition.x:F2}, {sample.ScreenPosition.y:F2}) exceeds standard framing bounds (±0.5)."));
                    compositionInfoAdded = true;
                }
            }

            for (int i = 0; i < SampleCount; i++)
            {
                if (i == 0)
                {
                    velocities[i] = (distances[1] - distances[0]) / dt;
                }
                else if (i == SampleCount - 1)
                {
                    velocities[i] = (distances[i] - distances[i - 1]) / dt;
                }
                else
                {
                    velocities[i] = (distances[i + 1] - distances[i - 1]) / (2f * dt);
                }

                float absV = Mathf.Abs(velocities[i]);
                if (absV > report.MaxSpeed)
                {
                    report.MaxSpeed = absV;
                }
            }

            for (int i = 0; i < SampleCount; i++)
            {
                if (i == 0)
                {
                    accelerations[i] = (velocities[1] - velocities[0]) / dt;
                }
                else if (i == SampleCount - 1)
                {
                    accelerations[i] = (velocities[i] - velocities[i - 1]) / dt;
                }
                else
                {
                    accelerations[i] = (velocities[i + 1] - velocities[i - 1]) / (2f * dt);
                }

                float absA = Mathf.Abs(accelerations[i]);
                if (absA > report.MaxAcceleration)
                {
                    report.MaxAcceleration = absA;
                }
            }

            for (int i = 0; i < SampleCount; i++)
            {
                if (i == 0)
                {
                    jerks[i] = (accelerations[1] - accelerations[0]) / dt;
                }
                else if (i == SampleCount - 1)
                {
                    jerks[i] = (accelerations[i] - accelerations[i - 1]) / dt;
                }
                else
                {
                    jerks[i] = (accelerations[i + 1] - accelerations[i - 1]) / (2f * dt);
                }

                float absJ = Mathf.Abs(jerks[i]);
                if (absJ > report.MaxJerk)
                {
                    report.MaxJerk = absJ;
                }
            }

            if (motion.ShotType == VLiveCameraShotType.Spline)
            {
                if (report.MaxSpeed > recMaxSpeed)
                {
                    report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Warning, "Dynamics", $"Peak speed ({report.MaxSpeed:F2} m/s) exceeds recommended limit ({recMaxSpeed:F2} m/s)."));
                    report.HasWarnings = true;
                }

                if (report.MaxAcceleration > recMaxAcc)
                {
                    report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Warning, "Dynamics", $"Peak acceleration ({report.MaxAcceleration:F2} m/s²) exceeds recommended limit ({recMaxAcc:F2} m/s²)."));
                    report.HasWarnings = true;
                }

                if (report.MaxJerk > recMaxJerk)
                {
                    report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Info, "Dynamics", $"Peak jerk ({report.MaxJerk:F2} m/s³) exceeds recommended limit ({recMaxJerk:F2} m/s³)."));
                }
            }

            if (motion.ShotType == VLiveCameraShotType.Spline)
            {
                int inIndex = Mathf.Clamp(Mathf.RoundToInt((motion.InTime / duration) * (SampleCount - 1)), 0, SampleCount - 1);
                float inSpeed = Mathf.Abs(velocities[inIndex]);

                if (motion.EntryMode == EntryMode.Rolling && inSpeed < 0.05f)
                {
                    report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Warning, "Entry", $"Rolling Entry configured but has near-zero speed ({inSpeed:F2} m/s) at In Time."));
                    report.HasWarnings = true;
                }
            }

            if (report.MinFieldOfView < 5f || report.MaxFieldOfView > 140f)
            {
                report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Warning, "Lens", $"Extreme field of view detected (Min: {report.MinFieldOfView:F1}°, Max: {report.MaxFieldOfView:F1}°)."));
                report.HasWarnings = true;
            }

            if (report.Messages.Count == 0)
            {
                report.Messages.Add(new VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity.Info, "OK", "All motion, composition, and timing values are within valid ranges."));
            }
        }
    }
}
