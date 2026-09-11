using UnityEngine;
using UnityEngine.Splines;

namespace VLiveKit.Camera
{
    /// <summary>
    /// カメラワークの動作系統分類。
    /// </summary>
    public enum MotionFamily
    {
        Fixed,
        Push,
        Pull,
        Truck,
        Arc,
        Pedestal,
        Crane,
        Gimbal,
        Fluid
    }

    /// <summary>
    /// 被写体に対するフレーミングの大きさ。
    /// </summary>
    public enum ShotSize
    {
        Wide,
        Full,
        Medium,
        Close,
        ExtremeClose
    }

    /// <summary>
    /// ショットの演出エネルギー・勢い。
    /// </summary>
    public enum ShotEnergy
    {
        Calm,
        Normal,
        Dynamic
    }

    /// <summary>
    /// スケール変更時のタイミング調整モード。
    /// </summary>
    public enum ScaleTimingMode
    {
        PreserveDuration,
        PreserveSpeed
    }

    /// <summary>
    /// レンズ焦点特性の指定モード。
    /// </summary>
    public enum LensMode
    {
        FieldOfView,
        FocalLength
    }

    /// <summary>
    /// カメラロール（傾き）の制御モード。
    /// </summary>
    public enum RollMode
    {
        MaintainHorizon,
        RollCurve,
        SplineUp
    }

    /// <summary>
    /// ショット開始時の運動モード。
    /// </summary>
    public enum EntryMode
    {
        Static,
        Rolling,
        Continuous
    }

    /// <summary>
    /// Clip終端到達時の動作。
    /// </summary>
    public enum ExitBehavior
    {
        Hold,
        PostRoll
    }

    /// <summary>
    /// スプラインの単一結節点（制御点、接線、モード、回転）を保持するシリアライズ可能構造体。
    /// </summary>
    [System.Serializable]
    public struct MotionKnot
    {
        [Tooltip("ターゲットローカル空間における制御点位置。")]
        public Vector3 Position;

        [Tooltip("結節点へのイン方向の接線ベクトル。")]
        public Vector3 TangentIn;

        [Tooltip("結節点からのアウト方向の接線ベクトル。")]
        public Vector3 TangentOut;

        [Tooltip("結節点の接線モード（AutoSmooth, Linear, Mirrored, Continuous, Broken）。")]
        public TangentMode TangentMode;

        [Tooltip("結節点のローカル回転。")]
        public Quaternion Rotation;

        public MotionKnot(Vector3 position)
        {
            Position = position;
            TangentIn = Vector3.zero;
            TangentOut = Vector3.zero;
            TangentMode = TangentMode.Continuous;
            Rotation = Quaternion.identity;
        }

        public MotionKnot(Vector3 position, Vector3 tangentIn, Vector3 tangentOut, TangentMode tangentMode, Quaternion rotation)
        {
            Position = position;
            TangentIn = tangentIn;
            TangentOut = tangentOut;
            TangentMode = tangentMode;
            Rotation = rotation;
        }
    }

    /// <summary>
    /// Body、Timing、Aim、Composition、Lens、Roll、Activationを同期評価する再利用可能なCamera Performance設定。
    /// </summary>
    [CreateAssetMenu(fileName = "MotionPreset", menuName = "VLiveKit/Camera/Motion Preset", order = 100)]
    public class VLiveCameraMotionPreset : ScriptableObject
    {
        // Fields

        [Header("Identity & Intent (識別と意図)")]
        [Tooltip("InspectorおよびSetup Windowで表示されるプリセット名。")]
        [SerializeField]
        private string _displayName = "New Motion Preset";

        [Tooltip("ショットの種別（Fixed: 固定構図, Spline: スプライン移動）。")]
        [SerializeField]
        private VLiveCameraShot.ShotType _shotType = VLiveCameraShot.ShotType.Fixed;

        [Tooltip("カメラワークの動作系統分類。")]
        [SerializeField]
        private MotionFamily _motionFamily = MotionFamily.Fixed;

        [Tooltip("フレーミングの大きさ。")]
        [SerializeField]
        private ShotSize _shotSize = ShotSize.Medium;

        [Tooltip("ショットの演出エネルギー。")]
        [SerializeField]
        private ShotEnergy _energy = ShotEnergy.Normal;

        [Tooltip("想定用途の短い説明。")]
        [SerializeField]
        private string _description = "";

        [Header("Rig Profile (機材プロファイル)")]
        [Tooltip("機材固有の応答や制約を共有するRig Profile参照（未設定時は既定値を使用）。")]
        [SerializeField]
        private VLiveCameraRigProfile _rigProfile;

        [Header("Body Track (移動軌道)")]
        [Tooltip("ターゲットローカル空間における完全なスプライン結節点配列。")]
        [SerializeField]
        private MotionKnot[] _knots = new MotionKnot[0];

        [Tooltip("スプラインが閉ループであるか。")]
        [SerializeField]
        private bool _isClosed = false;

        [Tooltip("基準スケールにおけるスプライン長（メートル単位）。PreserveSpeedの計算に使用されます。")]
        [Min(0f)]
        [SerializeField]
        private float _referenceSplineLength = 0f;

        [Tooltip("スプライン開始距離（メートル単位、0でスプライン始点）。")]
        [Min(0f)]
        [SerializeField]
        private float _startDistance = 0f;

        [Tooltip("スプライン終了距離（メートル単位、0でスプライン全長）。")]
        [Min(0f)]
        [SerializeField]
        private float _endDistance = 0f;

        [Header("Timing Track (タイミング設定)")]
        [Tooltip("クリップの全体Duration（秒単位）。")]
        [Min(0.1f)]
        [SerializeField]
        private float _clipDuration = 5.0f;

        [Tooltip("正規化時間(0..1)に対する進行度(0..1)のProgress Curve。")]
        [SerializeField]
        private AnimationCurve _progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Rigスケール変更時のタイミング調整規則（PreserveDuration: 時間維持, PreserveSpeed: 速度維持）。")]
        [SerializeField]
        private ScaleTimingMode _scaleTimingMode = ScaleTimingMode.PreserveDuration;

        [Tooltip("手動速度倍率の最小値。")]
        [Min(0f)]
        [SerializeField]
        private float _minSpeedMultiplier = 0.2f;

        [Tooltip("手動速度倍率の最大値。")]
        [Min(0.1f)]
        [SerializeField]
        private float _maxSpeedMultiplier = 3.0f;

        [Tooltip("Speed操作1ステップあたりの速度増減量。")]
        [Min(0.01f)]
        [SerializeField]
        private float _speedStep = 0.1f;

        [Header("Aim & Composition (注視・構図設定)")]
        [Tooltip("Target Heightからの注視点基準オフセット（メートル単位）。")]
        [SerializeField]
        private Vector3 _aimOffset = Vector3.zero;

        [Tooltip("進行度に伴うAim Offset X方向のアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _aimOffsetXCurve = new AnimationCurve();

        [Tooltip("進行度に伴うAim Offset Y方向のアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _aimOffsetYCurve = new AnimationCurve();

        [Tooltip("進行度に伴うAim Offset Z方向のアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _aimOffsetZCurve = new AnimationCurve();

        [Tooltip("画面内注視目標位置（-0.5〜0.5、0が中央）。")]
        [SerializeField]
        private Vector2 _screenPosition = Vector2.zero;

        [Tooltip("進行度に伴うScreen Position Xのアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _screenPositionXCurve = new AnimationCurve();

        [Tooltip("進行度に伴うScreen Position Yのアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _screenPositionYCurve = new AnimationCurve();

        [Tooltip("Rotation ComposerのDead Zoneを有効化するか。")]
        [SerializeField]
        private bool _deadZoneEnabled = false;

        [Tooltip("Dead Zoneの画面サイズ（0..1）。")]
        [SerializeField]
        private Vector2 _deadZoneSize = new Vector2(0.1f, 0.1f);

        [Tooltip("Rotation ComposerのHard Limitsを有効化するか。")]
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
        [Range(0f, 1f)]
        [SerializeField]
        private float _lookaheadTime = 0f;

        [Tooltip("先読みスムージング（0〜30）。")]
        [Range(0f, 30f)]
        [SerializeField]
        private float _lookaheadSmoothing = 0f;

        [Tooltip("カメラ起動時に被写体を画面中央へ引き戻すか。")]
        [SerializeField]
        private bool _centerOnActivate = true;

        [Header("Lens Track (レンズ設定)")]
        [Tooltip("レンズの焦点設定モード（FieldOfViewまたはFocalLength）。")]
        [SerializeField]
        private LensMode _lensMode = LensMode.FieldOfView;

        [Tooltip("カメラの基準画角（Field of View、度単位）。")]
        [Range(1f, 179f)]
        [SerializeField]
        private float _fieldOfView = 40f;

        [Tooltip("進行度に伴う画角（Field of View）のアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _fieldOfViewCurve = new AnimationCurve();

        [Tooltip("カメラの基準焦点距離（Focal Length、ミリメートル単位）。")]
        [Min(1f)]
        [SerializeField]
        private float _focalLength = 50f;

        [Tooltip("進行度に伴う焦点距離のアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _focalLengthCurve = new AnimationCurve();

        [Tooltip("物理カメラ使用時のセンサーサイズ（ミリメートル単位）。")]
        [SerializeField]
        private Vector2 _sensorSize = new Vector2(36f, 24f);

        [Header("Roll Track (ロール設定)")]
        [Tooltip("水平維持またはロールカーブによる回転モード。")]
        [SerializeField]
        private RollMode _rollMode = RollMode.MaintainHorizon;

        [Tooltip("進行度に伴うロール角度（度単位）のアニメーションカーブ。")]
        [SerializeField]
        private AnimationCurve _rollCurve = new AnimationCurve();

        [Header("Activation & Entry (開始・終了設定)")]
        [Tooltip("ショット開始時の速度状態（Static: 速度0から, Rolling: 非0速度から継続）。")]
        [SerializeField]
        private EntryMode _entryMode = EntryMode.Static;

        [Tooltip("クリップ内での実番組使用開始時刻（秒単位）。")]
        [Min(0f)]
        [SerializeField]
        private float _inTime = 0f;

        [Tooltip("クリップ内での実番組使用終了時刻（秒単位）。")]
        [Min(0f)]
        [SerializeField]
        private float _outTime = 5.0f;

        [Tooltip("終了時刻到達時の動作（Hold: 停止保持, PostRoll: 余白区間継続）。")]
        [SerializeField]
        private ExitBehavior _exitBehavior = ExitBehavior.Hold;


        // Properties

        public string DisplayName => _displayName;
        public VLiveCameraShot.ShotType ShotType => _shotType;
        public MotionFamily Family => _motionFamily;
        public ShotSize Size => _shotSize;
        public ShotEnergy Energy => _energy;
        public string Description => _description;
        public VLiveCameraRigProfile RigProfile => _rigProfile;

        public MotionKnot[] Knots => _knots;
        public bool IsClosed => _isClosed;
        public float ReferenceSplineLength => _referenceSplineLength;
        public float StartDistance => _startDistance;
        public float EndDistance => _endDistance;

        public float ClipDuration => _clipDuration;
        public AnimationCurve ProgressCurve => _progressCurve;
        public ScaleTimingMode ScaleMode => _scaleTimingMode;
        public float MinSpeedMultiplier => _minSpeedMultiplier;
        public float MaxSpeedMultiplier => _maxSpeedMultiplier;
        public float SpeedStep => _speedStep;

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

        public EntryMode EntryMode => _entryMode;
        public float InTime => _inTime;
        public float OutTime => _outTime;
        public ExitBehavior ExitBehavior => _exitBehavior;


        // Methods

        /// <summary>
        /// プリセットの主要パラメータを一括初期化します（CreatorおよびBaker共用）。
        /// </summary>
        public void Initialize(
            string displayName,
            VLiveCameraShot.ShotType shotType,
            MotionFamily motionFamily,
            ShotSize shotSize,
            ShotEnergy energy,
            string description,
            VLiveCameraRigProfile rigProfile,
            MotionKnot[] knots,
            bool isClosed,
            float referenceSplineLength,
            float clipDuration,
            AnimationCurve progressCurve,
            ScaleTimingMode scaleTimingMode,
            Vector3 aimOffset,
            Vector2 screenPosition,
            float fieldOfView,
            EntryMode entryMode,
            float inTime,
            float outTime,
            ExitBehavior exitBehavior,
            float startDistance = 0f,
            float endDistance = 0f)
        {
            _displayName = displayName;
            _shotType = shotType;
            _motionFamily = motionFamily;
            _shotSize = shotSize;
            _energy = energy;
            _description = description;
            _rigProfile = rigProfile;
            _knots = knots ?? new MotionKnot[0];
            _isClosed = isClosed;
            _referenceSplineLength = referenceSplineLength;
            _startDistance = Mathf.Max(0f, startDistance);
            _endDistance = Mathf.Max(0f, endDistance);
            _clipDuration = Mathf.Max(0.1f, clipDuration);
            _progressCurve = (progressCurve != null && progressCurve.length > 0)
                ? progressCurve
                : AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            _scaleTimingMode = scaleTimingMode;
            _aimOffset = aimOffset;
            _screenPosition = screenPosition;
            _fieldOfView = Mathf.Clamp(fieldOfView, 1f, 179f);
            _entryMode = entryMode;
            _inTime = Mathf.Clamp(inTime, 0f, _clipDuration);
            _outTime = Mathf.Clamp(outTime, _inTime, _clipDuration);
            _exitBehavior = exitBehavior;
        }

        /// <summary>
        /// 基準スプライン長を明示的に更新します。
        /// </summary>
        public void SetReferenceSplineLength(float length)
        {
            _referenceSplineLength = Mathf.Max(0f, length);
        }

        /// <summary>
        /// 結節点配列を設定します。
        /// </summary>
        public void SetKnots(MotionKnot[] knots)
        {
            _knots = knots ?? new MotionKnot[0];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_clipDuration < 0.1f)
            {
                _clipDuration = 0.1f;
            }

            if (_inTime < 0f)
            {
                _inTime = 0f;
            }
            else if (_inTime > _clipDuration)
            {
                _inTime = _clipDuration;
            }

            if (_outTime < _inTime)
            {
                _outTime = _inTime;
            }
            else if (_outTime > _clipDuration)
            {
                _outTime = _clipDuration;
            }

            if (_fieldOfView < 1f)
            {
                _fieldOfView = 1f;
            }
            else if (_fieldOfView > 179f)
            {
                _fieldOfView = 179f;
            }

            if (_focalLength < 1f)
            {
                _focalLength = 1f;
            }

            if (_minSpeedMultiplier < 0f)
            {
                _minSpeedMultiplier = 0f;
            }

            if (_maxSpeedMultiplier < _minSpeedMultiplier)
            {
                _maxSpeedMultiplier = _minSpeedMultiplier;
            }

            if (_speedStep < 0.01f)
            {
                _speedStep = 0.01f;
            }

            if (_progressCurve == null || _progressCurve.length == 0)
            {
                _progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }

            if (_knots == null)
            {
                _knots = new MotionKnot[0];
            }
        }
#endif
    }
}
