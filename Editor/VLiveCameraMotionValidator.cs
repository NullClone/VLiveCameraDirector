using System.Collections.Generic;
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

        public enum DiagnosticSeverity
        {
            Info,
            Warning,
            Error
        }

        public struct DiagnosticMessage
        {
            public DiagnosticSeverity Severity;
            public string Category;
            public string Message;

            public DiagnosticMessage(DiagnosticSeverity severity, string category, string message)
            {
                Severity = severity;
                Category = category;
                Message = message;
            }
        }

        public class ValidationReport
        {
            public List<DiagnosticMessage> Messages = new List<DiagnosticMessage>();
            public float Duration;
            public float SplineLength;
            public float MaxSpeed;
            public float MaxAcceleration;
            public float MaxJerk;
            public float MinFieldOfView;
            public float MaxFieldOfView;
            public bool HasErrors;
            public bool HasWarnings;
        }

        private const int SampleCount = 60;


        // Methods

        /// <summary>
        /// Scene上のShotインスタンスをサンプリング診断します。
        /// </summary>
        public static ValidationReport ValidateShot(VLiveCameraShot shot)
        {
            var report = new ValidationReport();

            if (shot == null)
            {
                report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Error, "General", "Shot コンポーネントが null です。"));
                report.HasErrors = true;
                return report;
            }

            if (shot.CinemachineCamera == null)
            {
                report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Error, "Camera", "専用 CinemachineCamera が未割り当てです。"));
                report.HasErrors = true;
            }

            if (shot.AimProxy == null)
            {
                report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Warning, "Aim", "専用 Aim Proxy が未設定です。"));
                report.HasWarnings = true;
            }

            VLiveCameraAppliedMotion motion = shot.AppliedMotion;
            if (motion == null)
            {
                report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Error, "Motion", "適用済み Motion 設定が存在しません。"));
                report.HasErrors = true;
                return report;
            }

            float splineLen = 0f;
            if (shot.Type == VLiveCameraShot.ShotType.Spline)
            {
                if (shot.SplineDolly == null || shot.SplineDolly.Spline == null)
                {
                    report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Error, "Spline", "Spline Dolly または SplineContainer が未設定です。"));
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

            if (shot.CinemachineCamera != null && shot.PerformerTarget != null)
            {
                float nearClip = shot.CinemachineCamera.Lens.NearClipPlane;
                float distToTarget = Vector3.Distance(shot.CinemachineCamera.transform.position, shot.PerformerTarget.position);
                if (distToTarget <= nearClip)
                {
                    report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Warning, "Composition", $"Target までの距離 ({distToTarget:F2}m) がカメラの Near Clip Plane ({nearClip:F2}m) 以下です。被写体がクリップされる可能性があります。"));
                    report.HasWarnings = true;
                }
            }

            VLiveCameraRigProfile profile = shot.AppliedPreset != null ? shot.AppliedPreset.RigProfile : null;
            RunMotionDiagnostics(motion, splineLen, profile, report);
            return report;
        }

        /// <summary>
        /// Motion Preset原本をサンプリング診断します。
        /// </summary>
        public static ValidationReport ValidatePreset(VLiveCameraMotionPreset preset, float optionalSplineLength = 0f)
        {
            var report = new ValidationReport();

            if (preset == null)
            {
                report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Error, "General", "Motion Preset が null です。"));
                report.HasErrors = true;
                return report;
            }

            float splineLen = optionalSplineLength > 0.001f ? optionalSplineLength : preset.ReferenceSplineLength;
            if (splineLen <= 0.001f && preset.ShotType == VLiveCameraShot.ShotType.Spline && preset.Knots != null && preset.Knots.Length > 1)
            {
                for (int i = 1; i < preset.Knots.Length; i++)
                {
                    splineLen += Vector3.Distance(preset.Knots[i - 1].Position, preset.Knots[i].Position);
                }
            }

            var tempMotion = new VLiveCameraAppliedMotion();
            tempMotion.ApplyFromPreset(preset, splineLen, preset.RigProfile);

            RunMotionDiagnostics(tempMotion, splineLen, preset.RigProfile, report);
            return report;
        }

        private static void RunMotionDiagnostics(
            VLiveCameraAppliedMotion motion,
            float splineLength,
            VLiveCameraRigProfile profile,
            ValidationReport report)
        {
            report.Duration = motion.EffectiveDuration;
            report.SplineLength = splineLength;
            report.MinFieldOfView = float.MaxValue;
            report.MaxFieldOfView = float.MinValue;

            // 1. In / Out Point 診断
            if (motion.InTime < 0f)
            {
                report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Error, "Timing", $"In Time が 0 未満です ({motion.InTime:F2}s)。"));
                report.HasErrors = true;
            }

            if (motion.OutTime > motion.EffectiveDuration + 0.001f)
            {
                report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Error, "Timing", $"Out Time ({motion.OutTime:F2}s) が Duration ({motion.EffectiveDuration:F2}s) を超えています。"));
                report.HasErrors = true;
            }

            if (motion.InTime >= motion.OutTime)
            {
                report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Error, "Timing", $"In Time ({motion.InTime:F2}s) が Out Time ({motion.OutTime:F2}s) 以上です。"));
                report.HasErrors = true;
            }

            // 2. Profile制約
            float recMaxSpeed = profile != null ? profile.RecommendedMaxSpeed : 5.0f;
            float recMaxAcc = profile != null ? profile.RecommendedMaxAcceleration : 10.0f;
            float recMaxJerk = profile != null ? profile.RecommendedMaxJerk : 50.0f;

            // 3. サンプリング計算
            float duration = Mathf.Max(0.01f, motion.EffectiveDuration);
            float dt = duration / (SampleCount - 1);

            var distances = new float[SampleCount];
            var velocities = new float[SampleCount];
            var accelerations = new float[SampleCount];
            var jerks = new float[SampleCount];

            float prevProgress = 0f;
            bool monotonicityWarned = false;

            for (int i = 0; i < SampleCount; i++)
            {
                float t = i * dt;
                VLiveCameraMotionSample sample = VLiveCameraMotionEvaluator.Evaluate(motion, t, splineLength);

                if (!sample.IsValid)
                {
                    report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Error, "Safety", $"時刻 {t:F2}s で NaN または Infinity の異常値を検出しました。"));
                    report.HasErrors = true;
                }

                // Progress単調性チェック
                if (i > 0 && sample.Progress < prevProgress - 0.005f && !monotonicityWarned)
                {
                    report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Warning, "Curve", $"Progress Curve が非単調です（時刻 {t:F2}s で後退区間が存在します）。"));
                    report.HasWarnings = true;
                    monotonicityWarned = true;
                }

                prevProgress = sample.Progress;
                distances[i] = sample.SplineDistance;

                // Lens FOV
                if (sample.FieldOfView < report.MinFieldOfView)
                {
                    report.MinFieldOfView = sample.FieldOfView;
                }

                if (sample.FieldOfView > report.MaxFieldOfView)
                {
                    report.MaxFieldOfView = sample.FieldOfView;
                }

                // Screen Position
                if (Mathf.Abs(sample.ScreenPosition.x) > 0.5f || Mathf.Abs(sample.ScreenPosition.y) > 0.5f)
                {
                    if (!report.HasWarnings)
                    {
                        report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Info, "Composition", $"Screen Position ({sample.ScreenPosition.x:F2}, {sample.ScreenPosition.y:F2}) が通常枠 (±0.5) を越えています。"));
                    }
                }
            }

            // 速度計算
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

            // 加速度計算
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

            // Jerk計算
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

            // 4. 動特性の推奨値比較
            if (motion.ShotType == VLiveCameraShot.ShotType.Spline)
            {
                if (report.MaxSpeed > recMaxSpeed)
                {
                    report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Warning, "Dynamics", $"最大移動速度 ({report.MaxSpeed:F2} m/s) が推奨値 ({recMaxSpeed:F2} m/s) を超えています。"));
                    report.HasWarnings = true;
                }

                if (report.MaxAcceleration > recMaxAcc)
                {
                    report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Warning, "Dynamics", $"最大加速度 ({report.MaxAcceleration:F2} m/s²) が推奨値 ({recMaxAcc:F2} m/s²) を超えています。"));
                    report.HasWarnings = true;
                }

                if (report.MaxJerk > recMaxJerk)
                {
                    report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Info, "Dynamics", $"最大Jerk ({report.MaxJerk:F2} m/s³) が推奨値 ({recMaxJerk:F2} m/s³) を超えています。"));
                }
            }

            // 5. Entry Mode検証
            if (motion.ShotType == VLiveCameraShot.ShotType.Spline)
            {
                int inIndex = Mathf.Clamp(Mathf.RoundToInt((motion.InTime / duration) * (SampleCount - 1)), 0, SampleCount - 1);
                float inSpeed = Mathf.Abs(velocities[inIndex]);

                if (motion.EntryMode == EntryMode.Static && inSpeed > 0.5f)
                {
                    report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Warning, "Entry", $"Static Entry ですが In Time で非ゼロの進行速度 ({inSpeed:F2} m/s) を持ちます。"));
                    report.HasWarnings = true;
                }
                else if (motion.EntryMode == EntryMode.Rolling && inSpeed < 0.05f)
                {
                    report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Warning, "Entry", $"Rolling Entry ですが In Time での進行速度がほぼゼロ ({inSpeed:F2} m/s) です。"));
                    report.HasWarnings = true;
                }
            }

            // 6. Lens検証
            if (report.MinFieldOfView < 5f || report.MaxFieldOfView > 140f)
            {
                report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Warning, "Lens", $"画角が極端な値です (最小: {report.MinFieldOfView:F1}°, 最大: {report.MaxFieldOfView:F1}°)。"));
                report.HasWarnings = true;
            }

            if (report.Messages.Count == 0)
            {
                report.Messages.Add(new DiagnosticMessage(DiagnosticSeverity.Info, "OK", "診断結果: すべての運動・構図・タイミング値が正常範囲内です。"));
            }
        }
    }
}
