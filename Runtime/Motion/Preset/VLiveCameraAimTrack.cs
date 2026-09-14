using System;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// 注視点と画面内構図の時間変化を保持します。
    /// </summary>
    [Serializable]
    public class VLiveCameraAimTrack
    {
        // Fields

        [Tooltip("被写体Target Group中心からの基準注視オフセット。単位はメートルです。")]
        [SerializeField]
        private Vector3 _aimOffset;

        [Tooltip("進行度に応じて加算する注視オフセットX。")]
        [SerializeField]
        private AnimationCurve _aimOffsetXCurve = new AnimationCurve();

        [Tooltip("進行度に応じて加算する注視オフセットY。")]
        [SerializeField]
        private AnimationCurve _aimOffsetYCurve = new AnimationCurve();

        [Tooltip("進行度に応じて加算する注視オフセットZ。")]
        [SerializeField]
        private AnimationCurve _aimOffsetZCurve = new AnimationCurve();

        [Tooltip("画面中心を0とする基準注視位置。範囲は-0.5から0.5です。")]
        [SerializeField]
        private Vector2 _screenPosition;

        [Tooltip("進行度に応じて加算する画面位置X。")]
        [SerializeField]
        private AnimationCurve _screenPositionXCurve = new AnimationCurve();

        [Tooltip("進行度に応じて加算する画面位置Y。")]
        [SerializeField]
        private AnimationCurve _screenPositionYCurve = new AnimationCurve();

        [Tooltip("Rotation ComposerのDead Zoneを使用するか。")]
        [SerializeField]
        private bool _deadZoneEnabled;

        [Tooltip("Dead Zoneの画面比率。")]
        [SerializeField]
        private Vector2 _deadZoneSize = new Vector2(0.1f, 0.1f);

        [Tooltip("Rotation ComposerのHard Limitsを使用するか。")]
        [SerializeField]
        private bool _hardLimitsEnabled;

        [Tooltip("Hard Limitsの画面比率。")]
        [SerializeField]
        private Vector2 _hardLimitsSize = new Vector2(0.8f, 0.8f);

        [Tooltip("Hard Limitsの画面オフセット。")]
        [SerializeField]
        private Vector2 _hardLimitsOffset;

        [Tooltip("Rotation Composerの水平・垂直ダンピング。")]
        [SerializeField]
        private Vector2 _damping = new Vector2(0.35f, 0.35f);

        [Tooltip("被写体移動の先読みを使用するか。")]
        [SerializeField]
        private bool _lookaheadEnabled;

        [Tooltip("先読み時間。単位は秒です。")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _lookaheadTime;

        [Tooltip("先読み結果の平滑化強度。範囲は0から30です。")]
        [Range(0f, 30f)]
        [SerializeField]
        private float _lookaheadSmoothing;

        [Tooltip("カメラ有効化時に被写体を基準構図へ戻すか。")]
        [SerializeField]
        private bool _centerOnActivate = true;


        // Properties

        public Vector3 AimOffset
        {
            get => _aimOffset;
            set => _aimOffset = value;
        }

        public AnimationCurve AimOffsetXCurve
        {
            get => _aimOffsetXCurve;
            set => _aimOffsetXCurve = VLiveCameraCurveUtility.Clone(value);
        }

        public AnimationCurve AimOffsetYCurve
        {
            get => _aimOffsetYCurve;
            set => _aimOffsetYCurve = VLiveCameraCurveUtility.Clone(value);
        }

        public AnimationCurve AimOffsetZCurve
        {
            get => _aimOffsetZCurve;
            set => _aimOffsetZCurve = VLiveCameraCurveUtility.Clone(value);
        }

        public Vector2 ScreenPosition
        {
            get => _screenPosition;
            set => _screenPosition = value;
        }

        public AnimationCurve ScreenPositionXCurve
        {
            get => _screenPositionXCurve;
            set => _screenPositionXCurve = VLiveCameraCurveUtility.Clone(value);
        }

        public AnimationCurve ScreenPositionYCurve
        {
            get => _screenPositionYCurve;
            set => _screenPositionYCurve = VLiveCameraCurveUtility.Clone(value);
        }

        public bool DeadZoneEnabled
        {
            get => _deadZoneEnabled;
            set => _deadZoneEnabled = value;
        }

        public Vector2 DeadZoneSize
        {
            get => _deadZoneSize;
            set => _deadZoneSize = value;
        }

        public bool HardLimitsEnabled
        {
            get => _hardLimitsEnabled;
            set => _hardLimitsEnabled = value;
        }

        public Vector2 HardLimitsSize
        {
            get => _hardLimitsSize;
            set => _hardLimitsSize = value;
        }

        public Vector2 HardLimitsOffset
        {
            get => _hardLimitsOffset;
            set => _hardLimitsOffset = value;
        }

        public Vector2 Damping
        {
            get => _damping;
            set => _damping = value;
        }

        public bool LookaheadEnabled
        {
            get => _lookaheadEnabled;
            set => _lookaheadEnabled = value;
        }

        public float LookaheadTime
        {
            get => _lookaheadTime;
            set => _lookaheadTime = Mathf.Clamp01(value);
        }

        public float LookaheadSmoothing
        {
            get => _lookaheadSmoothing;
            set => _lookaheadSmoothing = Mathf.Clamp(value, 0f, 30f);
        }

        public bool CenterOnActivate
        {
            get => _centerOnActivate;
            set => _centerOnActivate = value;
        }


        // Methods

        /// <summary>
        /// AnimationCurveを含む独立した複製を作成します。
        /// </summary>
        public VLiveCameraAimTrack Clone()
        {
            return new VLiveCameraAimTrack
            {
                AimOffset = AimOffset,
                AimOffsetXCurve = AimOffsetXCurve,
                AimOffsetYCurve = AimOffsetYCurve,
                AimOffsetZCurve = AimOffsetZCurve,
                ScreenPosition = ScreenPosition,
                ScreenPositionXCurve = ScreenPositionXCurve,
                ScreenPositionYCurve = ScreenPositionYCurve,
                DeadZoneEnabled = DeadZoneEnabled,
                DeadZoneSize = DeadZoneSize,
                HardLimitsEnabled = HardLimitsEnabled,
                HardLimitsSize = HardLimitsSize,
                HardLimitsOffset = HardLimitsOffset,
                Damping = Damping,
                LookaheadEnabled = LookaheadEnabled,
                LookaheadTime = LookaheadTime,
                LookaheadSmoothing = LookaheadSmoothing,
                CenterOnActivate = CenterOnActivate
            };
        }
    }
}
