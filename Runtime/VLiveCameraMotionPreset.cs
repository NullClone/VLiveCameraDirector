using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Body、Timing、Aim、Lens、Roll、Activationを単一時間軸で同期評価するCamera Performanceアセットです。
    /// </summary>
    [CreateAssetMenu(fileName = "MotionPreset", menuName = "VLiveKit/Camera/Motion Preset", order = 100)]
    public class VLiveCameraMotionPreset : ScriptableObject
    {
        // Fields

        [Tooltip("Camera Performanceを構成する全トラック。")]
        [SerializeField]
        private VLiveCameraMotionPresetData _data = new VLiveCameraMotionPresetData();


        // Properties

        public VLiveCameraMotionPresetData Data => _data;
        public string DisplayName => _data.Identity.DisplayName;
        public VLiveCameraShotType ShotType => _data.Identity.ShotType;
        public MotionFamily Family => _data.Identity.Family;
        public ShotSize Size => _data.Identity.Size;
        public ShotEnergy Energy => _data.Identity.Energy;
        public string Description => _data.Identity.Description;
        public VLiveCameraRigProfile RigProfile => _data.RigProfile;

        public MotionKnot[] Knots => _data.Body.Knots;
        public bool IsClosed => _data.Body.IsClosed;
        public float ReferenceSplineLength => _data.Body.ReferenceSplineLength;
        public float StartDistance => _data.Body.StartDistance;
        public float EndDistance => _data.Body.EndDistance;

        public float ClipDuration => _data.Timing.ClipDuration;
        public AnimationCurve ProgressCurve => _data.Timing.ProgressCurve;
        public ScaleTimingMode ScaleMode => _data.Timing.ScaleMode;
        public float MinSpeedMultiplier => _data.Timing.MinSpeedMultiplier;
        public float MaxSpeedMultiplier => _data.Timing.MaxSpeedMultiplier;
        public float SpeedStep => _data.Timing.SpeedStep;

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

        public EntryMode EntryMode => _data.Activation.EntryMode;
        public float InTime => _data.Activation.InTime;
        public float OutTime => _data.Activation.OutTime;
        public ExitBehavior ExitBehavior => _data.Activation.ExitBehavior;


        // Methods

        /// <summary>
        /// CreatorまたはBakerが作成した全トラックを複製して設定します。
        /// </summary>
        public void Initialize(VLiveCameraMotionPresetData data)
        {
            _data = data != null ? data.Clone() : new VLiveCameraMotionPresetData();
            _data.Sanitize();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _data ??= new VLiveCameraMotionPresetData();
            _data.Sanitize();
        }
#endif
    }
}
