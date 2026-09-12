using System;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// PresetからShotへ明示的に適用された再生設定とScene固有の解決値を保持します。
    /// </summary>
    [Serializable]
    public class VLiveCameraAppliedMotion
    {
        // Fields

        [Tooltip("Shotへ適用されたCamera Performanceの独立コピー。")]
        [SerializeField]
        private VLiveCameraMotionPresetData _data = new VLiveCameraMotionPresetData();

        [Tooltip("Scene上で解決された実スプライン長。単位はメートルです。")]
        [Min(0f)]
        [SerializeField]
        private float _resolvedSplineLength;

        [Tooltip("スケール規則を適用した実効再生時間。単位は秒です。")]
        [Min(0.1f)]
        [SerializeField]
        private float _effectiveDuration = 4f;

        [Tooltip("実効再生時間へ変換されたProgram開始時刻。単位は秒です。")]
        [Min(0f)]
        [SerializeField]
        private float _resolvedInTime;

        [Tooltip("実効再生時間へ変換されたProgram終了時刻。単位は秒です。")]
        [Min(0f)]
        [SerializeField]
        private float _resolvedOutTime = 4f;

        [Tooltip("速度変更が目標値へ追従する時間。単位は秒です。")]
        [Min(0.01f)]
        [SerializeField]
        private float _speedResponseTime = 0.35f;

        [Tooltip("Hold入力後に停止するまでの時間。単位は秒です。")]
        [Min(0.01f)]
        [SerializeField]
        private float _holdDecelerationTime = 0.3f;

        [Tooltip("Resume入力後に巡航速度へ戻る時間。単位は秒です。")]
        [Min(0.01f)]
        [SerializeField]
        private float _resumeAccelerationTime = 0.35f;

        [Tooltip("Reverse入力後に停止するまでの時間。単位は秒です。")]
        [Min(0.01f)]
        [SerializeField]
        private float _reverseDecelerationTime = 0.35f;

        [Tooltip("方向反転後に巡航速度へ戻る時間。単位は秒です。")]
        [Min(0.01f)]
        [SerializeField]
        private float _reverseAccelerationTime = 0.4f;


        // Properties

        public VLiveCameraMotionPresetData Data => _data;
        public VLiveCameraShotType ShotType => _data.Identity.ShotType;
        public MotionFamily Family => _data.Identity.Family;
        public float StartDistance => _data.Body.StartDistance;
        public float EndDistance => _data.Body.EndDistance;
        public float ClipDuration => _data.Timing.ClipDuration;
        public AnimationCurve ProgressCurve => _data.Timing.ProgressCurve;
        public ScaleTimingMode ScaleMode => _data.Timing.ScaleMode;
        public float ReferenceSplineLength => _data.Body.ReferenceSplineLength;
        public float ResolvedSplineLength => _resolvedSplineLength;
        public float EffectiveDuration => _effectiveDuration;
        public float MinSpeedMultiplier => _data.Timing.MinSpeedMultiplier;
        public float MaxSpeedMultiplier => _data.Timing.MaxSpeedMultiplier;
        public float SpeedStep => _data.Timing.SpeedStep;

        public EntryMode EntryMode => _data.Activation.EntryMode;
        public float InTime => _resolvedInTime;
        public float OutTime => _resolvedOutTime;
        public ExitBehavior ExitBehavior => _data.Activation.ExitBehavior;

        public Vector3 AimOffset => _data.Aim.AimOffset;
        public AnimationCurve AimOffsetXCurve => _data.Aim.AimOffsetXCurve;
        public AnimationCurve AimOffsetYCurve => _data.Aim.AimOffsetYCurve;
        public AnimationCurve AimOffsetZCurve => _data.Aim.AimOffsetZCurve;
        public Vector2 ScreenPosition => _data.Aim.ScreenPosition;
        public AnimationCurve ScreenPositionXCurve => _data.Aim.ScreenPositionXCurve;
        public AnimationCurve ScreenPositionYCurve => _data.Aim.ScreenPositionYCurve;
        public bool DeadZoneEnabled => _data.Aim.DeadZoneEnabled;
        public Vector2 DeadZoneSize => _data.Aim.DeadZoneSize;
        public bool HardLimitsEnabled => _data.Aim.HardLimitsEnabled;
        public Vector2 HardLimitsSize => _data.Aim.HardLimitsSize;
        public Vector2 HardLimitsOffset => _data.Aim.HardLimitsOffset;
        public Vector2 Damping => _data.Aim.Damping;
        public bool LookaheadEnabled => _data.Aim.LookaheadEnabled;
        public float LookaheadTime => _data.Aim.LookaheadTime;
        public float LookaheadSmoothing => _data.Aim.LookaheadSmoothing;
        public bool CenterOnActivate => _data.Aim.CenterOnActivate;

        public LensMode LensMode => _data.Lens.Mode;
        public float FieldOfView => _data.Lens.FieldOfView;
        public AnimationCurve FieldOfViewCurve => _data.Lens.FieldOfViewCurve;
        public float FocalLength => _data.Lens.FocalLength;
        public AnimationCurve FocalLengthCurve => _data.Lens.FocalLengthCurve;
        public Vector2 SensorSize => _data.Lens.SensorSize;

        public RollMode RollMode => _data.Roll.Mode;
        public AnimationCurve RollCurve => _data.Roll.Curve;

        public float SpeedResponseTime => _speedResponseTime;
        public float HoldDecelerationTime => _holdDecelerationTime;
        public float ResumeAccelerationTime => _resumeAccelerationTime;
        public float ReverseDecelerationTime => _reverseDecelerationTime;
        public float ReverseAccelerationTime => _reverseAccelerationTime;


        // Methods

        /// <summary>
        /// Presetと実スプライン長からShot固有の設定を解決します。
        /// </summary>
        public void ApplyFromPreset(VLiveCameraMotionPreset preset, float resolvedSplineLength, VLiveCameraRigProfile defaultProfile = null)
        {
            if (preset == null)
            {
                return;
            }

            _data = preset.Data.Clone();
            _resolvedSplineLength = Mathf.Max(0f, resolvedSplineLength);

            float timeScale = 1f;
            if (ScaleMode == ScaleTimingMode.PreserveSpeed && ReferenceSplineLength > 0.001f && _resolvedSplineLength > 0.001f)
            {
                timeScale = _resolvedSplineLength / ReferenceSplineLength;
            }

            _effectiveDuration = Mathf.Max(0.1f, ClipDuration * timeScale);
            _resolvedInTime = Mathf.Clamp(_data.Activation.InTime * timeScale, 0f, _effectiveDuration);
            _resolvedOutTime = Mathf.Clamp(_data.Activation.OutTime * timeScale, _resolvedInTime, _effectiveDuration);

            ApplyRigProfile(preset.RigProfile != null ? preset.RigProfile : defaultProfile);
        }

        private void ApplyRigProfile(VLiveCameraRigProfile profile)
        {
            if (profile == null)
            {
                _speedResponseTime = 0.35f;
                _holdDecelerationTime = 0.3f;
                _resumeAccelerationTime = 0.35f;
                _reverseDecelerationTime = 0.35f;
                _reverseAccelerationTime = 0.4f;
                return;
            }

            _speedResponseTime = Mathf.Max(0.01f, profile.SpeedResponseTime);
            _holdDecelerationTime = Mathf.Max(0.01f, profile.HoldDecelerationTime);
            _resumeAccelerationTime = Mathf.Max(0.01f, profile.ResumeAccelerationTime);
            _reverseDecelerationTime = Mathf.Max(0.01f, profile.ReverseDecelerationTime);
            _reverseAccelerationTime = Mathf.Max(0.01f, profile.ReverseAccelerationTime);
        }
    }
}
