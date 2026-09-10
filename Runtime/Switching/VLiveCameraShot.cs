using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

namespace toshi.VLiveKit.Camera
{
    /// <summary>
    /// 単一のカメラショットの構図と再生状態を管理するコンポーネント。
    /// 各ショットは専用のCinemachineCameraを所有します。
    /// </summary>
    [DisallowMultipleComponent]
    public class VLiveCameraShot : MonoBehaviour
    {
        // Fields

        /// <summary>
        /// ショットの動作種別。
        /// </summary>
        public enum ShotType
        {
            Fixed,
            Spline
        }

        [Header("Shot Identification (ショット識別)")]
        [Tooltip("ショットの識別名。")]
        [SerializeField]
        private string _shotName = "Shot";

        [Tooltip("生成元となったMotion Presetへの参照（任意）。")]
        [SerializeField]
        private VLiveCameraMotionPreset _preset = null;

        [Header("Camera References (カメラ参照)")]
        [Tooltip("このショット専用のCinemachineCamera。")]
        [SerializeField]
        private CinemachineCamera _cinemachineCamera;

        [Tooltip("ショットの動作種別（Fixed: 固定構図, Spline: スプライン移動）。")]
        [SerializeField]
        private ShotType _shotType = ShotType.Fixed;

        [Tooltip("スプライン移動を行う場合のCinemachineSplineDolly。")]
        [SerializeField]
        private CinemachineSplineDolly _splineDolly;

        [Header("Motion Settings (移動設定)")]
        [Tooltip("スプライン上の初期進行速度（正規化位置/秒）。")]
        [Min(0f)]
        [SerializeField]
        private float _initialSpeed = 0.2f;

        [Tooltip("スプライン進行速度の最小値。")]
        [Min(0f)]
        [SerializeField]
        private float _minSpeed = 0.05f;

        [Tooltip("スプライン進行速度の最大値。")]
        [Min(0f)]
        [SerializeField]
        private float _maxSpeed = 1.0f;

        [Tooltip("Speed Up / Down操作による速度変化量。")]
        [Min(0f)]
        [SerializeField]
        private float _speedStep = 0.05f;

        [Tooltip("初期の進行方向（1: 正方向, -1: 逆方向）。")]
        [Range(-1, 1)]
        [SerializeField]
        private int _initialDirection = 1;

        [Tooltip("スプラインの開始位置（0.0〜1.0）。")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _startPosition = 0f;

        [Tooltip("スプラインの終了位置（0.0〜1.0）。")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _endPosition = 1f;

        [Tooltip("終端に近づいた際に減速を開始する距離（正規化単位）。")]
        [Min(0f)]
        [SerializeField]
        private float _decelerationDistance = 0.25f;

        private float _currentSpeed;
        private int _currentDirection = 1;
        private bool _isLive;
        private bool _isPlaying;
        private bool _isHolding;
        private bool _isPrepared;


        // Properties

        /// <summary>
        /// ショットの識別名を取得します。
        /// </summary>
        public string ShotName => _shotName;

        /// <summary>
        /// 生成元となったMotion Presetを取得します。
        /// </summary>
        public VLiveCameraMotionPreset Preset => _preset;

        /// <summary>
        /// このショット専用のCinemachineCameraを取得します。
        /// </summary>
        public CinemachineCamera CinemachineCamera
        {
            get
            {
                if (_cinemachineCamera == null)
                {
                    _cinemachineCamera = GetComponent<CinemachineCamera>();
                }

                return _cinemachineCamera;
            }
        }

        /// <summary>
        /// ショットの動作種別を取得します。
        /// </summary>
        public ShotType Type => _shotType;

        /// <summary>
        /// スプライン移動コンポーネントを取得します。
        /// </summary>
        public CinemachineSplineDolly SplineDolly
        {
            get
            {
                if (_splineDolly == null && CinemachineCamera != null)
                {
                    _splineDolly = CinemachineCamera.GetComponent<CinemachineSplineDolly>();
                }

                return _splineDolly;
            }
        }

        /// <summary>
        /// このショットが正常にProgramとして動作可能であるか（必須コンポーネントおよび参照の有無）を取得します。
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (CinemachineCamera == null)
                {
                    return false;
                }

                if (_shotType == ShotType.Spline)
                {
                    if (SplineDolly == null || SplineDolly.Spline == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 現在ProgramとしてLive出力中であるかを取得します。
        /// </summary>
        public bool IsLive => _isLive;

        /// <summary>
        /// スプライン移動が再生中であるかを取得します。
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// スプライン移動が一時停止中であるかを取得します。
        /// </summary>
        public bool IsHolding => _isHolding;

        /// <summary>
        /// Off-Air中に始点での準備が完了しているかを取得します。
        /// </summary>
        public bool IsPrepared => _isPrepared;

        /// <summary>
        /// 現在の進行速度を取得します。
        /// </summary>
        public float CurrentSpeed => _currentSpeed;

        /// <summary>
        /// 現在の進行方向（1または-1）を取得します。
        /// </summary>
        public int CurrentDirection => _currentDirection;

        /// <summary>
        /// 現在のスプライン上のカメラ位置（正規化位置）を取得します。
        /// </summary>
        public float CurrentPosition
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
            if (_cinemachineCamera == null)
            {
                _cinemachineCamera = GetComponent<CinemachineCamera>();
            }

            if (_splineDolly == null && _cinemachineCamera != null)
            {
                _splineDolly = _cinemachineCamera.GetComponent<CinemachineSplineDolly>();
            }

            if (_splineDolly != null && _splineDolly.PositionUnits != PathIndexUnit.Normalized)
            {
                _splineDolly.PositionUnits = PathIndexUnit.Normalized;
            }

            _currentSpeed = Mathf.Clamp(_initialSpeed, _minSpeed, _maxSpeed);
            _currentDirection = (_initialDirection < 0) ? -1 : 1;
            _isPrepared = true;
        }

        private void Update()
        {
            if (!_isLive || !_isPlaying || _isHolding)
            {
                return;
            }

            if (_shotType != ShotType.Spline || _splineDolly == null || _splineDolly.Spline == null)
            {
                return;
            }

            float currentPos = _splineDolly.CameraPosition;
            if (_currentDirection > 0 && currentPos >= _endPosition)
            {
                _splineDolly.CameraPosition = _endPosition;
                _isPlaying = false;
                return;
            }

            if (_currentDirection < 0 && currentPos <= _startPosition)
            {
                _splineDolly.CameraPosition = _startPosition;
                _isPlaying = false;
                return;
            }

            float targetEnd = (_currentDirection > 0) ? _endPosition : _startPosition;
            float dist = Mathf.Abs(targetEnd - currentPos);

            if (dist <= 0.0001f)
            {
                _splineDolly.CameraPosition = targetEnd;
                _isPlaying = false;
                return;
            }

            float factor = 1.0f;
            if (_decelerationDistance > 0f && dist < _decelerationDistance)
            {
                factor = Mathf.SmoothStep(0.05f, 1.0f, dist / _decelerationDistance);
            }

            float delta = _currentSpeed * factor * _currentDirection * Time.deltaTime;
            float nextPos = currentPos + delta;

            if (_currentDirection > 0 && nextPos >= _endPosition)
            {
                nextPos = _endPosition;
                _isPlaying = false;
            }
            else if (_currentDirection < 0 && nextPos <= _startPosition)
            {
                nextPos = _startPosition;
                _isPlaying = false;
            }

            _splineDolly.CameraPosition = nextPos;
        }

        /// <summary>
        /// Off-Air中に次回再生用の始点へカメラを復帰させます。Live中は無視されます。
        /// </summary>
        public void PrepareStart()
        {
            if (_isLive)
            {
                return;
            }

            _isPlaying = false;
            _isHolding = false;
            _currentSpeed = Mathf.Clamp(_initialSpeed, _minSpeed, _maxSpeed);
            _currentDirection = (_initialDirection < 0) ? -1 : 1;

            if (_shotType == ShotType.Spline && _splineDolly != null)
            {
                _splineDolly.CameraPosition = _startPosition;
            }

            _isPrepared = true;
        }

        /// <summary>
        /// Programに選択された際に呼び出され、移動ショットの自動再生を開始します。
        /// </summary>
        public void OnEnterProgram()
        {
            _isLive = true;
            _isPrepared = false;

            if (_shotType == ShotType.Spline)
            {
                _isPlaying = true;
                _isHolding = false;
            }
        }

        /// <summary>
        /// Programから外れた（Off-Airになった）際に呼び出され、次回用の準備を行います。
        /// </summary>
        public void OnExitProgram()
        {
            _isLive = false;
            _isPlaying = false;

            if (_shotType == ShotType.Spline)
            {
                PrepareStart();
            }
        }

        /// <summary>
        /// スプライン進行速度を1段階上昇させます。
        /// </summary>
        public void SpeedUp()
        {
            if (!_isLive || _shotType != ShotType.Spline)
            {
                return;
            }

            _currentSpeed = Mathf.Clamp(_currentSpeed + _speedStep, _minSpeed, _maxSpeed);
        }

        /// <summary>
        /// スプライン進行速度を1段階下降させます。
        /// </summary>
        public void SpeedDown()
        {
            if (!_isLive || _shotType != ShotType.Spline)
            {
                return;
            }

            _currentSpeed = Mathf.Clamp(_currentSpeed - _speedStep, _minSpeed, _maxSpeed);
        }

        /// <summary>
        /// 現在位置を保持したまま進行方向を反転します。
        /// </summary>
        public void Reverse()
        {
            if (!_isLive || _shotType != ShotType.Spline)
            {
                return;
            }

            _currentDirection = -_currentDirection;
            if (!_isHolding)
            {
                _isPlaying = true;
            }
        }

        /// <summary>
        /// 現在位置でスプライン移動を一時停止します。
        /// </summary>
        public void Hold()
        {
            if (!_isLive || _shotType != ShotType.Spline)
            {
                return;
            }

            _isHolding = true;
        }

        /// <summary>
        /// 一時停止中のスプライン移動を再開します。
        /// </summary>
        public void Resume()
        {
            if (!_isLive || _shotType != ShotType.Spline)
            {
                return;
            }

            _isHolding = false;
            _isPlaying = true;
        }

        /// <summary>
        /// ショットの初期設定値を適用します。
        /// </summary>
        public void Configure(
            string shotName,
            VLiveCameraMotionPreset preset,
            CinemachineCamera cmCam,
            ShotType shotType,
            CinemachineSplineDolly splineDolly,
            float initialSpeed,
            float decelerationDistance)
        {
            _shotName = shotName;
            _preset = preset;
            _cinemachineCamera = cmCam;
            _shotType = shotType;
            _splineDolly = splineDolly;
            _initialSpeed = initialSpeed;
            _decelerationDistance = decelerationDistance;

            if (_splineDolly != null)
            {
                _splineDolly.PositionUnits = PathIndexUnit.Normalized;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_minSpeed < 0f)
            {
                _minSpeed = 0f;
            }

            if (_maxSpeed < _minSpeed)
            {
                _maxSpeed = _minSpeed;
            }

            if (_speedStep < 0f)
            {
                _speedStep = 0f;
            }

            _initialSpeed = Mathf.Clamp(_initialSpeed, _minSpeed, _maxSpeed);
            _initialDirection = (_initialDirection < 0) ? -1 : 1;

            if (_endPosition < _startPosition)
            {
                _endPosition = _startPosition;
            }

            if (_decelerationDistance < 0f)
            {
                _decelerationDistance = 0f;
            }
        }
#endif
    }
}
