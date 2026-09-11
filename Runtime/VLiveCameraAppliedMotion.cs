using System;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Motion Presetから解決され、Scene上の個別Shotへ具体的に適用された再生設定。
    /// Runtime再生状態（時刻や速度）とは独立してShotが所有します。
    /// </summary>
    [Serializable]
    public class VLiveCameraAppliedMotion
    {
        // Fields

        [Header("Shot Classification (ショット分類)")]
        [Tooltip("ショットの種別（Fixed: 固定構図, Spline: スプライン移動）。")]
        [SerializeField]
        private VLiveCameraShot.ShotType _shotType = VLiveCameraShot.ShotType.Fixed;

        [Tooltip("カメラワークの動作系統分類。")]
        [SerializeField]
        private MotionFamily _motionFamily = MotionFamily.Fixed;

        [Header("Body Track (移動軌道)")]
        [Tooltip("スプライン開始距離（メートル単位、0で始点）。")]
        [Min(0f)]
        [SerializeField]
        private float _startDistance = 0f;

        [Tooltip("スプライン終了距離（メートル単位、0でスプライン全長）。")]
        [Min(0f)]
        [SerializeField]
        private float _endDistance = 0f;

        [Header("Timing (タイミング設定)")]
        [Tooltip("Preset原本の基準Duration（秒単位）。")]
        [Min(0.1f)]
        [SerializeField]
        private float _clipDuration = 5.0f;

        [Tooltip("正規化時間(0..1)に対する進行度(0..1)のProgress Curve。")]
        [SerializeField]
        private AnimationCurve _progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Rigスケール変更時のタイミング調整規則。")]
        [SerializeField]
        private ScaleTimingMode _scaleTimingMode = ScaleTimingMode.PreserveDuration;

        [Tooltip("Preset原本の基準スプライン長（メートル単位）。")]
        [SerializeField]
        private float _referenceSplineLength = 0f;

        [Tooltip("Scene上で解決された実スプライン長（メートル単位）。")]
        [SerializeField]
        private float _resolvedSplineLength = 0f;

        [Tooltip("スプライン長とスケール規則から決定された実効再生時間（秒単位）。")]
        [Min(0.1f)]
        [SerializeField]
        private float _effectiveDuration = 5.0f;

        [Tooltip("手動速度倍率の最小値。")]
        [SerializeField]
        private float _minSpeedMultiplier = 0.2f;

        [Tooltip("手動速度倍率の最大値。")]
        [SerializeField]
        private float _maxSpeedMultiplier = 3.0f;

        [Tooltip("Speed操作1ステップあたりの速度増減量。")]
        [SerializeField]
        private float _speedStep = 0.1f;

        [Header("Activation & Entry (開始・終了設定)")]
        [Tooltip("ショット開始時の速度状態（Static: 速度0から, Rolling: 非0速度から継続）。")]
        [SerializeField]
        private EntryMode _entryMode = EntryMode.Static;

        [Tooltip("クリップ内での実番組使用開始時刻（秒単位）。")]
        [SerializeField]
        private float _inTime = 0f;

        [Tooltip("クリップ内での実番組使用終了時刻（秒単位）。")]
        [SerializeField]
        private float _outTime = 5.0f;

        [Tooltip("終了時刻到達時の動作（Hold: 停止保持, PostRoll: 余白区間継続）。")]
        [SerializeField]
        private ExitBehavior _exitBehavior = ExitBehavior.Hold;

        [Header("Aim & Composition (注視・構図設定)")]
        [Tooltip("注視点の基準オフセット（メートル単位）。")]
        [SerializeField]
        private Vector3 _aimOffset = Vector3.zero;

        [Tooltip("Aim Offset X方向のアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _aimOffsetXCurve = new AnimationCurve();

        [Tooltip("Aim Offset Y方向のアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _aimOffsetYCurve = new AnimationCurve();

        [Tooltip("Aim Offset Z方向のアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _aimOffsetZCurve = new AnimationCurve();

        [Tooltip("画面内注視目標位置（-0.5〜0.5、0が中央）。")]
        [SerializeField]
        private Vector2 _screenPosition = Vector2.zero;

        [Tooltip("Screen Position Xのアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _screenPositionXCurve = new AnimationCurve();

        [Tooltip("Screen Position Yのアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _screenPositionYCurve = new AnimationCurve();

        [Tooltip("Dead Zoneを有効化するか。")]
        [SerializeField]
        private bool _deadZoneEnabled = false;

        [Tooltip("Dead Zoneの画面サイズ（0..1）。")]
        [SerializeField]
        private Vector2 _deadZoneSize = new Vector2(0.1f, 0.1f);

        [Tooltip("Hard Limitsを有効化するか。")]
        [SerializeField]
        private bool _hardLimitsEnabled = false;

        [Tooltip("Hard Limitsの画面サイズ（0..1）。")]
        [SerializeField]
        private Vector2 _hardLimitsSize = new Vector2(0.8f, 0.8f);

        [Tooltip("Hard Limitsの画面オフセット。")]
        [SerializeField]
        private Vector2 _hardLimitsOffset = Vector2.zero;

        [Tooltip("カメラ追従のダンピング（X: 水平, Y: 垂直）。")]
        [SerializeField]
        private Vector2 _damping = new Vector2(0.5f, 0.5f);

        [Tooltip("被写体移動の先読み（Lookahead）を有効化するか。")]
        [SerializeField]
        private bool _lookaheadEnabled = false;

        [Tooltip("先読み時間（秒単位）。")]
        [SerializeField]
        private float _lookaheadTime = 0f;

        [Tooltip("先読みスムージング（0〜30）。")]
        [SerializeField]
        private float _lookaheadSmoothing = 0f;

        [Tooltip("カメラ起動時に被写体を画面中央へ引き戻すか。")]
        [SerializeField]
        private bool _centerOnActivate = true;

        [Header("Lens Track (レンズ設定)")]
        [Tooltip("レンズの焦点設定モード（FieldOfViewまたはFocalLength）。")]
        [SerializeField]
        private LensMode _lensMode = LensMode.FieldOfView;

        [Tooltip("カメラの基準画角（度単位）。")]
        [SerializeField]
        private float _fieldOfView = 40f;

        [Tooltip("画角のアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _fieldOfViewCurve = new AnimationCurve();

        [Tooltip("カメラの基準焦点距離（ミリメートル単位）。")]
        [SerializeField]
        private float _focalLength = 50f;

        [Tooltip("焦点距離のアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _focalLengthCurve = new AnimationCurve();

        [Tooltip("センサーサイズ（ミリメートル単位）。")]
        [SerializeField]
        private Vector2 _sensorSize = new Vector2(36f, 24f);

        [Header("Roll Track (ロール設定)")]
        [Tooltip("水平維持またはロールカーブによる回転モード。")]
        [SerializeField]
        private RollMode _rollMode = RollMode.MaintainHorizon;

        [Tooltip("ロール角度（度単位）のアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _rollCurve = new AnimationCurve();

        [Header("Rig Response (機材応答設定)")]
        [Tooltip("速度変更追従時間（秒単位）。")]
        [SerializeField]
        private float _speedResponseTime = 0.5f;

        [Tooltip("Hold減速時間（秒単位）。")]
        [SerializeField]
        private float _holdDecelerationTime = 0.4f;

        [Tooltip("Resume加速時間（秒単位）。")]
        [SerializeField]
        private float _resumeAccelerationTime = 0.4f;

        [Tooltip("Reverse減速時間（秒単位）。")]
        [SerializeField]
        private float _reverseDecelerationTime = 0.5f;

        [Tooltip("Reverse逆方向加速時間（秒単位）。")]
        [SerializeField]
        private float _reverseAccelerationTime = 0.5f;


        // Properties

        public VLiveCameraShot.ShotType ShotType => _shotType;
        public MotionFamily Family => _motionFamily;
        public float StartDistance => _startDistance;
        public float EndDistance => _endDistance;
        public float ClipDuration => _clipDuration;
        public AnimationCurve ProgressCurve => _progressCurve;
        public ScaleTimingMode ScaleMode => _scaleTimingMode;
        public float ReferenceSplineLength => _referenceSplineLength;
        public float ResolvedSplineLength => _resolvedSplineLength;
        public float EffectiveDuration => _effectiveDuration;
        public float MinSpeedMultiplier => _minSpeedMultiplier;
        public float MaxSpeedMultiplier => _maxSpeedMultiplier;
        public float SpeedStep => _speedStep;

        public EntryMode EntryMode => _entryMode;
        public float InTime => _inTime;
        public float OutTime => _outTime;
        public ExitBehavior ExitBehavior => _exitBehavior;

        public Vector3 AimOffset => _aimOffset;
        public AnimationCurve AimOffsetXCurve => _aimOffsetXCurve;
        public AnimationCurve AimOffsetYCurve => _aimOffsetYCurve;
        public AnimationCurve AimOffsetZCurve => _aimOffsetZCurve;
        public Vector2 ScreenPosition => _screenPosition;
        public AnimationCurve ScreenPositionXCurve => _screenPositionXCurve;
        public AnimationCurve ScreenPositionYCurve => _screenPositionYCurve;
        public bool DeadZoneEnabled => _deadZoneEnabled;
        public Vector2 DeadZoneSize => _deadZoneSize;
        public bool HardLimitsEnabled => _hardLimitsEnabled;
        public Vector2 HardLimitsSize => _hardLimitsSize;
        public Vector2 HardLimitsOffset => _hardLimitsOffset;
        public Vector2 Damping => _damping;
        public bool LookaheadEnabled => _lookaheadEnabled;
        public float LookaheadTime => _lookaheadTime;
        public float LookaheadSmoothing => _lookaheadSmoothing;
        public bool CenterOnActivate => _centerOnActivate;

        public LensMode LensMode => _lensMode;
        public float FieldOfView => _fieldOfView;
        public AnimationCurve FieldOfViewCurve => _fieldOfViewCurve;
        public float FocalLength => _focalLength;
        public AnimationCurve FocalLengthCurve => _focalLengthCurve;
        public Vector2 SensorSize => _sensorSize;

        public RollMode RollMode => _rollMode;
        public AnimationCurve RollCurve => _rollCurve;

        public float SpeedResponseTime => _speedResponseTime;
        public float HoldDecelerationTime => _holdDecelerationTime;
        public float ResumeAccelerationTime => _resumeAccelerationTime;
        public float ReverseDecelerationTime => _reverseDecelerationTime;
        public float ReverseAccelerationTime => _reverseAccelerationTime;


        // Methods

        /// <summary>
        /// Preset原本および実スプライン長からShot固有の設定を適用・解決します。
        /// </summary>
        public void ApplyFromPreset(VLiveCameraMotionPreset preset, float resolvedSplineLength, VLiveCameraRigProfile defaultProfile = null)
        {
            if (preset == null)
            {
                return;
            }

            _shotType = preset.ShotType;
            _motionFamily = preset.Family;
            _startDistance = preset.StartDistance;
            _endDistance = preset.EndDistance;
            _clipDuration = preset.ClipDuration;
            _progressCurve = CloneCurve(preset.ProgressCurve);
            _scaleTimingMode = preset.ScaleMode;
            _referenceSplineLength = preset.ReferenceSplineLength;
            _resolvedSplineLength = Mathf.Max(0f, resolvedSplineLength);

            if (_scaleTimingMode == ScaleTimingMode.PreserveSpeed && _referenceSplineLength > 0.001f && _resolvedSplineLength > 0.001f)
            {
                float scaleFactor = _resolvedSplineLength / _referenceSplineLength;
                _effectiveDuration = Mathf.Max(0.1f, preset.ClipDuration * scaleFactor);
                _inTime = Mathf.Clamp(preset.InTime * scaleFactor, 0f, _effectiveDuration);
                _outTime = Mathf.Clamp(preset.OutTime * scaleFactor, _inTime, _effectiveDuration);
            }
            else
            {
                _effectiveDuration = Mathf.Max(0.1f, preset.ClipDuration);
                _inTime = Mathf.Clamp(preset.InTime, 0f, _effectiveDuration);
                _outTime = Mathf.Clamp(preset.OutTime, _inTime, _effectiveDuration);
            }

            _minSpeedMultiplier = preset.MinSpeedMultiplier;
            _maxSpeedMultiplier = preset.MaxSpeedMultiplier;
            _speedStep = preset.SpeedStep;

            _entryMode = preset.EntryMode;
            _exitBehavior = preset.ExitBehavior;

            _aimOffset = preset.AimOffset;
            _aimOffsetXCurve = CloneCurve(preset.AimOffsetXCurve);
            _aimOffsetYCurve = CloneCurve(preset.AimOffsetYCurve);
            _aimOffsetZCurve = CloneCurve(preset.AimOffsetZCurve);

            _screenPosition = preset.ScreenPosition;
            _screenPositionXCurve = CloneCurve(preset.ScreenPositionXCurve);
            _screenPositionYCurve = CloneCurve(preset.ScreenPositionYCurve);

            _deadZoneEnabled = preset.DeadZoneEnabled;
            _deadZoneSize = preset.DeadZoneSize;
            _hardLimitsEnabled = preset.HardLimitsEnabled;
            _hardLimitsSize = preset.HardLimitsSize;
            _hardLimitsOffset = preset.HardLimitsOffset;
            _damping = preset.Damping;
            _lookaheadEnabled = preset.LookaheadEnabled;
            _lookaheadTime = preset.LookaheadTime;
            _lookaheadSmoothing = preset.LookaheadSmoothing;
            _centerOnActivate = preset.CenterOnActivate;

            _lensMode = preset.LensMode;
            _fieldOfView = preset.FieldOfView;
            _fieldOfViewCurve = CloneCurve(preset.FieldOfViewCurve);
            _focalLength = preset.FocalLength;
            _focalLengthCurve = CloneCurve(preset.FocalLengthCurve);
            _sensorSize = preset.SensorSize;

            _rollMode = preset.RollMode;
            _rollCurve = CloneCurve(preset.RollCurve);

            VLiveCameraRigProfile profile = preset.RigProfile != null ? preset.RigProfile : defaultProfile;
            if (profile != null)
            {
                _speedResponseTime = profile.SpeedResponseTime;
                _holdDecelerationTime = profile.HoldDecelerationTime;
                _resumeAccelerationTime = profile.ResumeAccelerationTime;
                _reverseDecelerationTime = profile.ReverseDecelerationTime;
                _reverseAccelerationTime = profile.ReverseAccelerationTime;
            }
            else
            {
                _speedResponseTime = 0.5f;
                _holdDecelerationTime = 0.4f;
                _resumeAccelerationTime = 0.4f;
                _reverseDecelerationTime = 0.5f;
                _reverseAccelerationTime = 0.5f;
            }
        }

        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            if (source == null || source.length == 0)
            {
                return new AnimationCurve();
            }

            return new AnimationCurve(source.keys);
        }
    }
}
