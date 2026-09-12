using Unity.Cinemachine;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// 単一のカメラショットの所有物（専用Camera、Spline、Aim Proxy、適用済みMotion設定）とOn-Air/Off-Air lifecycleを管理するコンポーネント。
    /// </summary>
    [DisallowMultipleComponent]
    public class VLiveCameraShot : MonoBehaviour
    {
        // Fields

        [Tooltip("ショットの識別名。")]
        [SerializeField]
        private string _shotName = "Shot";

        [Tooltip("ショットの動作種別（Fixed: 固定構図, Spline: スプライン移動）。")]
        [SerializeField]
        private VLiveCameraShotType _shotType = VLiveCameraShotType.Fixed;

        [Tooltip("このShotを所有するVLiveCameraRig。")]
        [SerializeField]
        private VLiveCameraRig _rig;

        [Header("Owned Components & References")]
        [Tooltip("このショット専用のCinemachineCamera。")]
        [SerializeField]
        private CinemachineCamera _cinemachineCamera;

        [Tooltip("スプライン移動を行う場合のCinemachineSplineDolly。")]
        [SerializeField]
        private CinemachineSplineDolly _splineDolly;

        [Tooltip("構図・注視制御を行うCinemachineRotationComposer。")]
        [SerializeField]
        private CinemachineRotationComposer _rotationComposer;

        [Tooltip("このショット専用のAim Proxy Transform。")]
        [SerializeField]
        private Transform _aimProxy;

        [Tooltip("このショット専用のMotion Player。")]
        [SerializeField]
        private VLiveCameraMotionPlayer _motionPlayer;

        [Tooltip("注視・追従基準となる演者Target Transform。")]
        [SerializeField]
        private Transform _performerTarget;

        [Header("Applied Motion Settings")]
        [Tooltip("このショットに適用されている再利用元Preset参照。")]
        [SerializeField]
        private VLiveCameraMotionPreset _appliedPreset;

        [Tooltip("このショットに適用済みのMotion設定実体。")]
        [SerializeField]
        private VLiveCameraAppliedMotion _appliedMotion = new VLiveCameraAppliedMotion();

        [Tooltip("Rebuild時に適用された注視基準高さ（メートル単位）。")]
        [SerializeField]
        private float _appliedTargetHeight = 1.3f;

        [Tooltip("Rebuild時に適用されたリグ正面向きクォータニオン。")]
        [SerializeField]
        private Quaternion _appliedRigOrientation = Quaternion.identity;

        [Tooltip("Rebuild時に適用された水平距離スケール。")]
        [SerializeField]
        private float _appliedDistanceScale = 1.0f;

        [Tooltip("Rebuild時に適用された移動幅スケール。")]
        [SerializeField]
        private float _appliedMotionScale = 1.0f;

        [Tooltip("Rebuild時に適用された垂直移動幅スケール。")]
        [SerializeField]
        private float _appliedVerticalMotionScale = 1.0f;


        // Properties

        public string ShotName => _shotName;
        public VLiveCameraShotType Type => _shotType;
        public VLiveCameraRig Rig => _rig;

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

        public CinemachineRotationComposer RotationComposer
        {
            get
            {
                if (_rotationComposer == null && CinemachineCamera != null)
                {
                    _rotationComposer = CinemachineCamera.GetComponent<CinemachineRotationComposer>();
                }

                return _rotationComposer;
            }
        }

        public Transform AimProxy => _aimProxy;

        public VLiveCameraMotionPlayer MotionPlayer
        {
            get
            {
                if (_motionPlayer == null)
                {
                    _motionPlayer = GetComponent<VLiveCameraMotionPlayer>();
                }

                return _motionPlayer;
            }
        }

        public Transform PerformerTarget
        {
            get => _performerTarget;
            set => _performerTarget = value;
        }

        public VLiveCameraMotionPreset AppliedPreset => _appliedPreset;
        public VLiveCameraAppliedMotion AppliedMotion => _appliedMotion;
        public float AppliedTargetHeight => _appliedTargetHeight;
        public Quaternion AppliedRigOrientation => _appliedRigOrientation;
        public float AppliedDistanceScale => _appliedDistanceScale > 0.001f ? _appliedDistanceScale : 1.0f;
        public float AppliedMotionScale => _appliedMotionScale > 0.001f ? _appliedMotionScale : 1.0f;
        public float AppliedVerticalMotionScale => Mathf.Max(0f, _appliedVerticalMotionScale);
        public float MasterPlaybackSpeed
        {
            get
            {
                if (_rig == null)
                {
                    _rig = GetComponentInParent<VLiveCameraRig>();
                }

                return _rig != null ? _rig.MasterPlaybackSpeed : 1f;
            }
        }

        /// <summary>
        /// このショットが正常にProgramとして動作可能であるかを取得します。
        /// 不正値や欠落参照がある場合はfalseを返し、暗黙の代替演出を行いません。
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (CinemachineCamera == null || MotionPlayer == null)
                {
                    return false;
                }

                if (_performerTarget == null || _aimProxy == null || RotationComposer == null)
                {
                    return false;
                }

                if (_appliedMotion == null)
                {
                    return false;
                }

                if (_appliedMotion.ClipDuration <= 0.001f || _appliedMotion.EffectiveDuration <= 0.001f)
                {
                    return false;
                }

                if (_appliedMotion.ProgressCurve == null || _appliedMotion.ProgressCurve.length < 2)
                {
                    return false;
                }

                if (_shotType == VLiveCameraShotType.Spline)
                {
                    if (SplineDolly == null || SplineDolly.Spline == null)
                    {
                        return false;
                    }

                    var spline = SplineDolly.Spline.Spline;
                    if (spline == null || spline.Count < 2 || spline.GetLength() <= 0.001f)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsLive => MotionPlayer != null && MotionPlayer.IsLive;
        public bool IsPlaying => MotionPlayer != null && MotionPlayer.IsPlaying;
        public bool IsHolding => MotionPlayer != null && MotionPlayer.IsHolding;
        public bool IsPrepared => MotionPlayer != null && MotionPlayer.IsPrepared;
        public float CurrentSpeedMultiplier => MotionPlayer != null ? MotionPlayer.CurrentSpeedMultiplier : 0f;
        public int CurrentDirection => MotionPlayer != null ? MotionPlayer.CurrentDirection : 1;
        public float CurrentTime => MotionPlayer != null ? MotionPlayer.CurrentTime : 0f;
        public float CurrentSplineDistance => MotionPlayer != null ? MotionPlayer.CurrentSplineDistance : 0f;


        // Methods

        private void Awake()
        {
            EnsureComponentReferences();
        }

        /// <summary>
        /// Off-Air中に次回再生用の始点へカメラを復帰させます。
        /// </summary>
        public void PrepareStart()
        {
            if (MotionPlayer != null)
            {
                MotionPlayer.PrepareStart();
            }
        }

        /// <summary>
        /// Programに選択された際に呼び出され、移動ショットの自動再生を開始します。
        /// </summary>
        public void OnEnterProgram()
        {
            if (MotionPlayer != null)
            {
                MotionPlayer.OnEnterProgram();
            }
        }

        /// <summary>
        /// Programから外れた（Off-Airになった）際に呼び出され、次回用の準備を行います。
        /// </summary>
        public void OnExitProgram()
        {
            if (MotionPlayer != null)
            {
                MotionPlayer.OnExitProgram();
            }
        }

        /// <summary>
        /// 進行速度を1段階上昇させます。
        /// </summary>
        public void SpeedUp()
        {
            if (MotionPlayer != null)
            {
                MotionPlayer.SpeedUp();
            }
        }

        /// <summary>
        /// 進行速度を1段階下降させます。
        /// </summary>
        public void SpeedDown()
        {
            if (MotionPlayer != null)
            {
                MotionPlayer.SpeedDown();
            }
        }

        /// <summary>
        /// 進行方向を安全に反転します。
        /// </summary>
        public void Reverse()
        {
            if (MotionPlayer != null)
            {
                MotionPlayer.Reverse();
            }
        }

        /// <summary>
        /// 移動を一時停止します。
        /// </summary>
        public void Hold()
        {
            if (MotionPlayer != null)
            {
                MotionPlayer.Hold();
            }
        }

        /// <summary>
        /// 一時停止中の移動を再開します。
        /// </summary>
        public void Resume()
        {
            if (MotionPlayer != null)
            {
                MotionPlayer.Resume();
            }
        }

        /// <summary>
        /// 緊急時に即時停止します。
        /// </summary>
        public void Freeze()
        {
            if (MotionPlayer != null)
            {
                MotionPlayer.Freeze();
            }
        }

        /// <summary>
        /// Aim Proxy Transformを設定します（参照修復・初期化用）。
        /// </summary>
        public void SetAimProxy(Transform aimProxy)
        {
            _aimProxy = aimProxy;
        }

        /// <summary>
        /// ショットの構成要素を一括設定します（Builder用）。
        /// </summary>
        public void Configure(
            string shotName,
            VLiveCameraRig rig,
            CinemachineCamera cmCam,
            VLiveCameraShotType shotType,
            CinemachineSplineDolly splineDolly,
            CinemachineRotationComposer composer,
            Transform aimProxy,
            VLiveCameraMotionPlayer motionPlayer,
            Transform performerTarget,
            VLiveCameraMotionPreset preset,
            float targetHeight,
            Quaternion rigOrientation,
            float distanceScale = 1.0f,
            float motionScale = 1.0f,
            float verticalMotionScale = 1.0f)
        {
            _shotName = shotName;
            _rig = rig;
            _cinemachineCamera = cmCam;
            _shotType = shotType;
            _splineDolly = splineDolly;
            _rotationComposer = composer;
            _aimProxy = aimProxy;
            _motionPlayer = motionPlayer;
            _performerTarget = performerTarget;
            _appliedPreset = preset;
            _appliedTargetHeight = targetHeight;
            _appliedRigOrientation = rigOrientation;
            _appliedDistanceScale = Mathf.Max(0.01f, distanceScale);
            _appliedMotionScale = Mathf.Max(0.01f, motionScale);
            _appliedVerticalMotionScale = Mathf.Max(0f, verticalMotionScale);

            if (_appliedMotion == null)
            {
                _appliedMotion = new VLiveCameraAppliedMotion();
            }

            float splineLen = 0f;
            if (_splineDolly != null && _splineDolly.Spline != null && _splineDolly.Spline.Spline != null)
            {
                splineLen = _splineDolly.Spline.Spline.GetLength();
            }

            if (preset != null)
            {
                _appliedMotion.ApplyFromPreset(preset, splineLen, preset.RigProfile);
            }

            if (_motionPlayer != null)
            {
                _motionPlayer.Configure(this, _cinemachineCamera, _splineDolly, _rotationComposer, _aimProxy);
            }
        }

        /// <summary>
        /// 適用済みMotion設定の内部値をPresetから再同期します（Rebuild用）。
        /// </summary>
        public void SyncAppliedMotion(
            VLiveCameraMotionPreset preset,
            float splineLength,
            float targetHeight,
            Quaternion rigOrientation,
            float distanceScale = 1.0f,
            float motionScale = 1.0f,
            float verticalMotionScale = 1.0f)
        {
            _appliedPreset = preset;
            _appliedTargetHeight = targetHeight;
            _appliedRigOrientation = rigOrientation;
            _appliedDistanceScale = Mathf.Max(0.01f, distanceScale);
            _appliedMotionScale = Mathf.Max(0.01f, motionScale);
            _appliedVerticalMotionScale = Mathf.Max(0f, verticalMotionScale);

            if (_appliedMotion == null)
            {
                _appliedMotion = new VLiveCameraAppliedMotion();
            }

            if (preset != null)
            {
                _appliedMotion.ApplyFromPreset(preset, splineLength, preset.RigProfile);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_appliedDistanceScale < 0.01f)
            {
                _appliedDistanceScale = 1.0f;
            }

            if (_appliedMotionScale < 0.01f)
            {
                _appliedMotionScale = 1.0f;
            }

            if (_appliedVerticalMotionScale < 0f)
            {
                _appliedVerticalMotionScale = 0f;
            }
        }
#endif

        private void EnsureComponentReferences()
        {
            if (_rig == null)
            {
                _rig = GetComponentInParent<VLiveCameraRig>();
            }

            if (_cinemachineCamera == null)
            {
                _cinemachineCamera = GetComponent<CinemachineCamera>();
            }

            if (_motionPlayer == null)
            {
                _motionPlayer = GetComponent<VLiveCameraMotionPlayer>();
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
        }
    }
}
