using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

namespace VLiveKit.Camera
{
    /// <summary>
    /// 単一ShotのCamera Performanceを再生・制御し、CinemachineCameraおよび各Trackへ反映するRuntime専用プレイヤー。
    /// 現在時刻、進行速度、方向、Hold/Reverseなどの再生状態の唯一の正本です。
    /// </summary>
    [DisallowMultipleComponent]
    public class VLiveCameraMotionPlayer : MonoBehaviour
    {
        // Fields

        [Header("Target Shot & Components")]
        [Tooltip("このプレイヤーが属するVLiveCameraShot。")]
        [SerializeField]
        private VLiveCameraShot _shot;

        [Tooltip("制御対象の専用CinemachineCamera。")]
        [SerializeField]
        private CinemachineCamera _cinemachineCamera;

        [Tooltip("スプライン制御用のCinemachineSplineDolly。")]
        [SerializeField]
        private CinemachineSplineDolly _splineDolly;

        [Tooltip("構図・注視制御用のCinemachineRotationComposer。")]
        [SerializeField]
        private CinemachineRotationComposer _rotationComposer;

        [Tooltip("注視点制御用のAim Proxy Transform。")]
        [SerializeField]
        private Transform _aimProxy;

        // Runtime Playback States (再生状態の正本)
        private float _currentTime;
        private float _currentSpeedMultiplier = 1.0f;
        private float _targetSpeedMultiplier = 1.0f;
        private int _currentDirection = 1;
        private int _targetDirection = 1;
        private bool _isLive;
        private bool _isPlaying;
        private bool _isHolding;
        private bool _isReversing;
        private bool _isResuming;
        private bool _isPrepared;


        // Properties

        public VLiveCameraShot Shot => _shot;
        public CinemachineCamera CinemachineCamera => _cinemachineCamera;
        public CinemachineSplineDolly SplineDolly => _splineDolly;
        public CinemachineRotationComposer RotationComposer => _rotationComposer;
        public Transform AimProxy => _aimProxy;

        public float CurrentTime => _currentTime;
        public float CurrentSpeedMultiplier => _currentSpeedMultiplier;
        public float TargetSpeedMultiplier => _targetSpeedMultiplier;
        public int CurrentDirection => _currentDirection;
        public bool IsLive => _isLive;
        public bool IsPlaying => _isPlaying;
        public bool IsHolding => _isHolding;
        public bool IsReversing => _isReversing;
        public bool IsResuming => _isResuming;
        public bool IsPrepared => _isPrepared;

        public float CurrentSplineDistance
        {
            get
            {
                if (_splineDolly != null)
                {
                    return _splineDolly.CameraPosition;
                }

                return 0f;
            }
        }


        // Methods

        private void Awake()
        {
            InitializeReferences();
        }

        private void Start()
        {
            PrepareStart();
        }

        private void Update()
        {
            if (!_isLive)
            {
                return;
            }

            VLiveCameraAppliedMotion motion = GetAppliedMotion();
            if (motion == null)
            {
                return;
            }

            if (_isPlaying && motion.ShotType == VLiveCameraShotType.Spline)
            {
                UpdateSpeedTransitions(motion, Time.deltaTime);

                float rigSpeed = _shot != null ? _shot.MasterPlaybackSpeed : 1f;
                float effectiveSpeed = _currentSpeedMultiplier * _currentDirection * rigSpeed;
                _currentTime += effectiveSpeed * Time.deltaTime;

                HandleBoundaries(motion);
            }

            EvaluateAndApply(motion);
        }

        /// <summary>
        /// Off-Air中にIn Pointの位置、Aim、Lensへショットを準備します。Live中は無視されます。
        /// </summary>
        public void PrepareStart()
        {
            if (_isLive)
            {
                return;
            }

            InitializeReferences();

            VLiveCameraAppliedMotion motion = GetAppliedMotion();
            _isPlaying = false;
            _isHolding = false;
            _isReversing = false;
            _isResuming = false;
            _currentDirection = 1;
            _targetDirection = 1;
            _targetSpeedMultiplier = 1.0f;
            _currentSpeedMultiplier = 1.0f;

            if (motion != null)
            {
                _currentTime = Mathf.Clamp(motion.InTime, 0f, motion.EffectiveDuration);
                EvaluateAndApply(motion);
            }
            else
            {
                _currentTime = 0f;
            }

            _isPrepared = true;
        }

        /// <summary>
        /// Programに選択された際に呼び出され、自動再生を開始します。
        /// </summary>
        public void OnEnterProgram()
        {
            _isLive = true;
            _isPrepared = false;

            VLiveCameraAppliedMotion motion = GetAppliedMotion();
            if (motion != null)
            {
                if (motion.ShotType == VLiveCameraShotType.Spline)
                {
                    _isPlaying = true;
                    _isHolding = false;
                    _isReversing = false;
                    _targetSpeedMultiplier = 1.0f;

                    if (motion.EntryMode == EntryMode.Static)
                    {
                        _currentSpeedMultiplier = 0f;
                        _isResuming = true;
                    }
                    else
                    {
                        _currentSpeedMultiplier = 1.0f;
                        _isResuming = false;
                    }
                }
                else
                {
                    _isPlaying = false;
                    _isHolding = false;
                    _isReversing = false;
                    _isResuming = false;
                }

                EvaluateAndApply(motion);
            }
        }

        /// <summary>
        /// Programから外れた（Off-Airになった）際に呼び出され、次回用のIn Pointへ準備します。
        /// </summary>
        public void OnExitProgram()
        {
            _isLive = false;
            _isPlaying = false;
            PrepareStart();
        }

        /// <summary>
        /// 目標速度倍率を1段階上昇させます。
        /// </summary>
        public void SpeedUp()
        {
            if (!_isLive)
            {
                return;
            }

            VLiveCameraAppliedMotion motion = GetAppliedMotion();
            if (motion == null || motion.ShotType != VLiveCameraShotType.Spline)
            {
                return;
            }

            _targetSpeedMultiplier = Mathf.Clamp(
                _targetSpeedMultiplier + motion.SpeedStep,
                motion.MinSpeedMultiplier,
                motion.MaxSpeedMultiplier
            );
        }

        /// <summary>
        /// 目標速度倍率を1段階下降させます。
        /// </summary>
        public void SpeedDown()
        {
            if (!_isLive)
            {
                return;
            }

            VLiveCameraAppliedMotion motion = GetAppliedMotion();
            if (motion == null || motion.ShotType != VLiveCameraShotType.Spline)
            {
                return;
            }

            _targetSpeedMultiplier = Mathf.Clamp(
                _targetSpeedMultiplier - motion.SpeedStep,
                motion.MinSpeedMultiplier,
                motion.MaxSpeedMultiplier
            );
        }

        /// <summary>
        /// 減速・停止を経て進行方向を安全に反転します。
        /// </summary>
        public void Reverse()
        {
            if (!_isLive)
            {
                return;
            }

            VLiveCameraAppliedMotion motion = GetAppliedMotion();
            if (motion == null || motion.ShotType != VLiveCameraShotType.Spline)
            {
                return;
            }

            if (_isHolding)
            {
                _isHolding = false;
                _isPlaying = true;
            }

            if (_isReversing)
            {
                _targetDirection = -_targetDirection;
            }
            else
            {
                _isReversing = true;
                _targetDirection = -_currentDirection;
            }

            _isResuming = false;
            _isPlaying = true;
        }

        /// <summary>
        /// 機材応答に従って減速し、現在位置で一時停止します。
        /// </summary>
        public void Hold()
        {
            if (!_isLive)
            {
                return;
            }

            VLiveCameraAppliedMotion motion = GetAppliedMotion();
            if (motion == null || motion.ShotType != VLiveCameraShotType.Spline)
            {
                return;
            }

            _isHolding = true;
            _isReversing = false;
            _isResuming = false;
        }

        /// <summary>
        /// 一時停止中のスプライン移動を加速再開します。
        /// </summary>
        public void Resume()
        {
            if (!_isLive)
            {
                return;
            }

            VLiveCameraAppliedMotion motion = GetAppliedMotion();
            if (motion == null || motion.ShotType != VLiveCameraShotType.Spline)
            {
                return;
            }

            if (_targetSpeedMultiplier <= 0f)
            {
                _targetSpeedMultiplier = 1.0f;
            }

            _isHolding = false;
            _isPlaying = true;
            _isResuming = true;
            _isReversing = false;
        }

        /// <summary>
        /// 緊急時に即時停止します（速度平滑化を行いません）。
        /// </summary>
        public void Freeze()
        {
            _currentSpeedMultiplier = 0f;
            _targetSpeedMultiplier = 0f;
            _isHolding = true;
            _isReversing = false;
            _isResuming = false;
        }

        /// <summary>
        /// 外部コンポーネント参照を明示的に割り当てます（Builder用）。
        /// </summary>
        public void Configure(
            VLiveCameraShot shot,
            CinemachineCamera cmCam,
            CinemachineSplineDolly splineDolly,
            CinemachineRotationComposer composer,
            Transform aimProxy)
        {
            _shot = shot;
            _cinemachineCamera = cmCam;
            _splineDolly = splineDolly;
            _rotationComposer = composer;
            _aimProxy = aimProxy;

            if (_splineDolly != null && _splineDolly.PositionUnits != PathIndexUnit.Distance)
            {
                _splineDolly.PositionUnits = PathIndexUnit.Distance;
            }
        }

        private void InitializeReferences()
        {
            if (_shot == null)
            {
                _shot = GetComponent<VLiveCameraShot>();
            }

            if (_cinemachineCamera == null && _shot != null)
            {
                _cinemachineCamera = _shot.CinemachineCamera;
            }

            if (_cinemachineCamera != null)
            {
                if (_splineDolly == null)
                {
                    _splineDolly = _cinemachineCamera.GetComponent<CinemachineSplineDolly>();
                }

                if (_rotationComposer == null)
                {
                    _rotationComposer = _cinemachineCamera.GetComponent<CinemachineRotationComposer>();
                }
            }

            if (_aimProxy == null && _shot != null)
            {
                _aimProxy = _shot.AimProxy;
            }

            if (_splineDolly != null && _splineDolly.PositionUnits != PathIndexUnit.Distance)
            {
                _splineDolly.PositionUnits = PathIndexUnit.Distance;
            }
        }

        private VLiveCameraAppliedMotion GetAppliedMotion()
        {
            return _shot != null ? _shot.AppliedMotion : null;
        }

        private float GetSplineLength()
        {
            if (_splineDolly != null && _splineDolly.Spline != null)
            {
                var spline = _splineDolly.Spline.Spline;
                if (spline != null)
                {
                    return spline.GetLength();
                }
            }

            return 0f;
        }

        private void UpdateSpeedTransitions(VLiveCameraAppliedMotion motion, float deltaTime)
        {
            float maxSpeed = Mathf.Max(1.0f, motion.MaxSpeedMultiplier);

            if (_isHolding)
            {
                float decelRate = maxSpeed / Mathf.Max(0.01f, motion.HoldDecelerationTime);
                _currentSpeedMultiplier = Mathf.MoveTowards(_currentSpeedMultiplier, 0f, decelRate * deltaTime);
                return;
            }

            if (_isReversing)
            {
                if (_currentDirection != _targetDirection)
                {
                    float decelRate = maxSpeed / Mathf.Max(0.01f, motion.ReverseDecelerationTime);
                    _currentSpeedMultiplier = Mathf.MoveTowards(_currentSpeedMultiplier, 0f, decelRate * deltaTime);

                    if (_currentSpeedMultiplier <= 0.0001f)
                    {
                        _currentDirection = _targetDirection;
                        _currentSpeedMultiplier = 0f;
                    }
                }
                else
                {
                    float accelRate = maxSpeed / Mathf.Max(0.01f, motion.ReverseAccelerationTime);
                    _currentSpeedMultiplier = Mathf.MoveTowards(_currentSpeedMultiplier, _targetSpeedMultiplier, accelRate * deltaTime);

                    if (Mathf.Approximately(_currentSpeedMultiplier, _targetSpeedMultiplier))
                    {
                        _currentSpeedMultiplier = _targetSpeedMultiplier;
                        _isReversing = false;
                    }
                }

                return;
            }

            if (_isResuming)
            {
                float accelRate = maxSpeed / Mathf.Max(0.01f, motion.ResumeAccelerationTime);
                _currentSpeedMultiplier = Mathf.MoveTowards(_currentSpeedMultiplier, _targetSpeedMultiplier, accelRate * deltaTime);

                if (Mathf.Approximately(_currentSpeedMultiplier, _targetSpeedMultiplier))
                {
                    _currentSpeedMultiplier = _targetSpeedMultiplier;
                    _isResuming = false;
                }

                return;
            }

            float speedRate = maxSpeed / Mathf.Max(0.01f, motion.SpeedResponseTime);
            _currentSpeedMultiplier = Mathf.MoveTowards(_currentSpeedMultiplier, _targetSpeedMultiplier, speedRate * deltaTime);
        }

        private void HandleBoundaries(VLiveCameraAppliedMotion motion)
        {
            if (_currentDirection > 0)
            {
                float limit = motion.ExitBehavior == ExitBehavior.Hold ? motion.OutTime : motion.EffectiveDuration;
                if (_currentTime >= limit)
                {
                    _currentTime = limit;
                    _isPlaying = false;
                    _currentSpeedMultiplier = 0f;
                    _isReversing = false;
                    _isResuming = false;
                }
            }
            else if (_currentDirection < 0)
            {
                if (_currentTime <= motion.InTime)
                {
                    _currentTime = motion.InTime;
                    _isPlaying = false;
                    _currentSpeedMultiplier = 0f;
                    _isReversing = false;
                    _isResuming = false;
                }
                else if (_currentTime <= 0f)
                {
                    _currentTime = 0f;
                    _isPlaying = false;
                    _currentSpeedMultiplier = 0f;
                    _isReversing = false;
                    _isResuming = false;
                }
            }
        }

        private void EvaluateAndApply(VLiveCameraAppliedMotion motion)
        {
            float splineLength = GetSplineLength();
            VLiveCameraMotionSample sample = VLiveCameraMotionEvaluator.Evaluate(motion, _currentTime, splineLength);

            if (!sample.IsValid)
            {
                return;
            }

            if (_splineDolly != null && motion.ShotType == VLiveCameraShotType.Spline)
            {
                if (_splineDolly.PositionUnits != PathIndexUnit.Distance)
                {
                    _splineDolly.PositionUnits = PathIndexUnit.Distance;
                }

                _splineDolly.CameraPosition = sample.SplineDistance;
            }

            if (_aimProxy != null && _shot != null && _shot.PerformerTarget != null)
            {
                Vector3 targetPos = _shot.PerformerTarget.position;
                Vector3 aimPos = targetPos + Vector3.up * _shot.AppliedTargetHeight + _shot.AppliedRigOrientation * sample.AimOffset;
                _aimProxy.position = aimPos;
            }

            if (_rotationComposer != null)
            {
                _rotationComposer.Composition.ScreenPosition = sample.ScreenPosition;
            }

            if (_cinemachineCamera != null)
            {
                _cinemachineCamera.Lens.FieldOfView = sample.FieldOfView;

                _cinemachineCamera.Lens.Dutch = motion.RollMode == RollMode.RollCurve ? sample.Roll : 0f;
            }
        }
    }
}
