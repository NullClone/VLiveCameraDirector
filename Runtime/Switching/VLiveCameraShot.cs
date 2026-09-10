using Unity.Cinemachine;
using UnityEngine;

namespace toshi.VLiveKit.Camera
{
    public class VLiveCameraShot : MonoBehaviour
    {
        public enum ShotType
        {
            Fixed,
            Spline
        }

        [SerializeField]
        private string _shotName = "Shot";

        [SerializeField]
        private CinemachineCamera _cinemachineCamera;

        [SerializeField]
        private ShotType _shotType = ShotType.Fixed;

        [SerializeField]
        private CinemachineSplineDolly _splineDolly;

        [SerializeField]
        private float _initialSpeed = 0.2f;

        [SerializeField]
        private float _minSpeed = 0.05f;

        [SerializeField]
        private float _maxSpeed = 1.0f;

        [SerializeField]
        private float _speedStep = 0.05f;

        [SerializeField]
        private int _initialDirection = 1;

        [SerializeField]
        private float _startPosition = 0f;

        [SerializeField]
        private float _endPosition = 1f;

        [SerializeField]
        private float _decelerationDistance = 0.25f;

        private float _currentSpeed;
        private int _currentDirection = 1;
        private bool _isLive;
        private bool _isPlaying;
        private bool _isHolding;
        private bool _isPrepared;

        public string ShotName => _shotName;
        public CinemachineCamera CinemachineCamera => _cinemachineCamera;
        public ShotType Type => _shotType;
        public CinemachineSplineDolly SplineDolly => _splineDolly;
        public bool IsLive => _isLive;
        public bool IsPlaying => _isPlaying;
        public bool IsHolding => _isHolding;
        public bool IsPrepared => _isPrepared;
        public float CurrentSpeed => _currentSpeed;
        public int CurrentDirection => _currentDirection;

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

            _currentSpeed = Mathf.Clamp(_initialSpeed, _minSpeed, _maxSpeed);
            _currentDirection = (_initialDirection < 0) ? -1 : 1;
            _isPrepared = (_shotType == ShotType.Spline);
        }

        private void Update()
        {
            if (!_isLive || !_isPlaying || _isHolding)
            {
                return;
            }

            if (_shotType != ShotType.Spline || _splineDolly == null)
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

        public void OnExitProgram()
        {
            _isLive = false;
            _isPlaying = false;

            if (_shotType == ShotType.Spline)
            {
                PrepareStart();
            }
        }

        public void SpeedUp()
        {
            if (!_isLive || _shotType != ShotType.Spline)
            {
                return;
            }

            _currentSpeed = Mathf.Clamp(_currentSpeed + _speedStep, _minSpeed, _maxSpeed);
        }

        public void SpeedDown()
        {
            if (!_isLive || _shotType != ShotType.Spline)
            {
                return;
            }

            _currentSpeed = Mathf.Clamp(_currentSpeed - _speedStep, _minSpeed, _maxSpeed);
        }

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

        public void Hold()
        {
            if (!_isLive || _shotType != ShotType.Spline)
            {
                return;
            }

            _isHolding = true;
        }

        public void Resume()
        {
            if (!_isLive || _shotType != ShotType.Spline)
            {
                return;
            }

            _isHolding = false;
            _isPlaying = true;
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

            _initialSpeed = Mathf.Clamp(_initialSpeed, _minSpeed, _maxSpeed);
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
