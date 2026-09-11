using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// 解決済みのMotion設定と時刻からMotion Sampleを決定的に計算する純粋評価クラス。
    /// Runtime再生とEditor診断で同一の評価ロジックを共有します。
    /// </summary>
    public static class VLiveCameraMotionEvaluator
    {
        // Methods

        /// <summary>
        /// 適用済みMotion設定、再生時刻、実スプライン長からMotion Sampleを評価します。
        /// </summary>
        /// <param name="motion">適用済みMotion設定。</param>
        /// <param name="time">クリップ再生時刻（秒単位）。</param>
        /// <param name="splineLength">実スプライン長（メートル単位）。</param>
        /// <returns>評価されたMotion Sample。</returns>
        public static VLiveCameraMotionSample Evaluate(VLiveCameraAppliedMotion motion, float time, float splineLength)
        {
            var sample = new VLiveCameraMotionSample
            {
                PlaybackTime = time,
                Phase = 0f,
                Progress = 0f,
                SplineDistance = 0f,
                AimOffset = Vector3.zero,
                ScreenPosition = Vector2.zero,
                FieldOfView = 40f,
                FocalLength = 50f,
                Roll = 0f,
                IsValid = true
            };

            if (motion == null)
            {
                sample.IsValid = false;
                return sample;
            }

            float duration = Mathf.Max(0.001f, motion.EffectiveDuration);
            float clampedTime = Mathf.Clamp(time, 0f, duration);
            float phase = Mathf.Clamp01(clampedTime / duration);
            sample.Phase = phase;

            // 1. Body Track / Progress
            float progress = phase;
            if (motion.ProgressCurve != null && motion.ProgressCurve.length > 0)
            {
                progress = motion.ProgressCurve.Evaluate(phase);
            }

            sample.Progress = progress;

            // distance = startDistance + travelDistance * progress
            float startDist = Mathf.Clamp(motion.StartDistance, 0f, splineLength);
            float endDist = (motion.EndDistance > startDist) ? Mathf.Min(motion.EndDistance, splineLength) : splineLength;
            float travelDist = Mathf.Max(0f, endDist - startDist);
            sample.SplineDistance = Mathf.Clamp(startDist + travelDist * Mathf.Clamp01(progress), 0f, splineLength);

            // 2. Aim Offset
            Vector3 aimOffset = motion.AimOffset;
            if (motion.AimOffsetXCurve != null && motion.AimOffsetXCurve.length > 0)
            {
                aimOffset.x = motion.AimOffsetXCurve.Evaluate(phase);
            }

            if (motion.AimOffsetYCurve != null && motion.AimOffsetYCurve.length > 0)
            {
                aimOffset.y = motion.AimOffsetYCurve.Evaluate(phase);
            }

            if (motion.AimOffsetZCurve != null && motion.AimOffsetZCurve.length > 0)
            {
                aimOffset.z = motion.AimOffsetZCurve.Evaluate(phase);
            }

            sample.AimOffset = aimOffset;

            // 3. Screen Position
            Vector2 screenPos = motion.ScreenPosition;
            if (motion.ScreenPositionXCurve != null && motion.ScreenPositionXCurve.length > 0)
            {
                screenPos.x = motion.ScreenPositionXCurve.Evaluate(phase);
            }

            if (motion.ScreenPositionYCurve != null && motion.ScreenPositionYCurve.length > 0)
            {
                screenPos.y = motion.ScreenPositionYCurve.Evaluate(phase);
            }

            sample.ScreenPosition = screenPos;

            // 4. Lens Track
            float fov = motion.FieldOfView;
            if (motion.FieldOfViewCurve != null && motion.FieldOfViewCurve.length > 0)
            {
                fov = motion.FieldOfViewCurve.Evaluate(phase);
            }

            float focal = motion.FocalLength;
            if (motion.FocalLengthCurve != null && motion.FocalLengthCurve.length > 0)
            {
                focal = motion.FocalLengthCurve.Evaluate(phase);
            }

            if (motion.LensMode == LensMode.FocalLength && focal > 0.001f)
            {
                float sensorY = motion.SensorSize.y > 0.001f ? motion.SensorSize.y : 24f;
                fov = 2.0f * Mathf.Rad2Deg * Mathf.Atan((sensorY * 0.5f) / focal);
            }

            sample.FieldOfView = Mathf.Clamp(fov, 1f, 179f);
            sample.FocalLength = Mathf.Max(1f, focal);

            // 5. Roll Track
            float roll = 0f;
            if (motion.RollMode == RollMode.RollCurve && motion.RollCurve != null && motion.RollCurve.length > 0)
            {
                roll = motion.RollCurve.Evaluate(phase);
            }

            sample.Roll = roll;

            // 6. Hard Safety Check
            if (float.IsNaN(sample.Phase) || float.IsInfinity(sample.Phase) ||
                float.IsNaN(sample.Progress) || float.IsInfinity(sample.Progress) ||
                float.IsNaN(sample.SplineDistance) || float.IsInfinity(sample.SplineDistance) ||
                float.IsNaN(sample.AimOffset.x) || float.IsInfinity(sample.AimOffset.x) ||
                float.IsNaN(sample.AimOffset.y) || float.IsInfinity(sample.AimOffset.y) ||
                float.IsNaN(sample.AimOffset.z) || float.IsInfinity(sample.AimOffset.z) ||
                float.IsNaN(sample.ScreenPosition.x) || float.IsInfinity(sample.ScreenPosition.x) ||
                float.IsNaN(sample.ScreenPosition.y) || float.IsInfinity(sample.ScreenPosition.y) ||
                float.IsNaN(sample.FieldOfView) || float.IsInfinity(sample.FieldOfView) ||
                float.IsNaN(sample.Roll) || float.IsInfinity(sample.Roll))
            {
                sample.IsValid = false;
                sample.SplineDistance = 0f;
                sample.AimOffset = Vector3.zero;
                sample.ScreenPosition = Vector2.zero;
                sample.FieldOfView = 40f;
                sample.Roll = 0f;
            }

            return sample;
        }

        /// <summary>
        /// Motion Preset原本から直接Motion Sampleを評価します（Editor Validator診断用）。
        /// </summary>
        public static VLiveCameraMotionSample EvaluatePreset(VLiveCameraMotionPreset preset, float time, float splineLength)
        {
            if (preset == null)
            {
                return new VLiveCameraMotionSample { IsValid = false, FieldOfView = 40f };
            }

            var tempApplied = new VLiveCameraAppliedMotion();
            tempApplied.ApplyFromPreset(preset, splineLength, preset.RigProfile);
            return Evaluate(tempApplied, time, splineLength);
        }
    }
}
