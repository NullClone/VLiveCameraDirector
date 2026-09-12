using System;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// レンズの基準値と時間変化を保持します。
    /// </summary>
    [Serializable]
    public class VLiveCameraLensTrack
    {
        // Fields

        [Tooltip("画角または焦点距離のどちらでレンズを制御するか。")]
        [SerializeField]
        private LensMode _lensMode = LensMode.FieldOfView;

        [Tooltip("基準垂直画角。単位は度です。")]
        [Range(1f, 179f)]
        [SerializeField]
        private float _fieldOfView = 40f;

        [Tooltip("進行度に応じた垂直画角。空の場合は基準値を使用します。")]
        [SerializeField]
        private AnimationCurve _fieldOfViewCurve = new AnimationCurve();

        [Tooltip("基準焦点距離。単位はミリメートルです。")]
        [Min(1f)]
        [SerializeField]
        private float _focalLength = 50f;

        [Tooltip("進行度に応じた焦点距離。空の場合は基準値を使用します。")]
        [SerializeField]
        private AnimationCurve _focalLengthCurve = new AnimationCurve();

        [Tooltip("物理カメラで使用するセンサーサイズ。単位はミリメートルです。")]
        [SerializeField]
        private Vector2 _sensorSize = new Vector2(36f, 24f);


        // Properties

        public LensMode Mode { get => _lensMode; set => _lensMode = value; }
        public float FieldOfView { get => _fieldOfView; set => _fieldOfView = Mathf.Clamp(value, 1f, 179f); }
        public AnimationCurve FieldOfViewCurve { get => _fieldOfViewCurve; set => _fieldOfViewCurve = VLiveCameraCurveUtility.Clone(value); }
        public float FocalLength { get => _focalLength; set => _focalLength = Mathf.Max(1f, value); }
        public AnimationCurve FocalLengthCurve { get => _focalLengthCurve; set => _focalLengthCurve = VLiveCameraCurveUtility.Clone(value); }
        public Vector2 SensorSize { get => _sensorSize; set => _sensorSize = new Vector2(Mathf.Max(0.1f, value.x), Mathf.Max(0.1f, value.y)); }


        // Methods

        /// <summary>
        /// AnimationCurveを含む独立した複製を作成します。
        /// </summary>
        public VLiveCameraLensTrack Clone()
        {
            return new VLiveCameraLensTrack
            {
                Mode = Mode,
                FieldOfView = FieldOfView,
                FieldOfViewCurve = FieldOfViewCurve,
                FocalLength = FocalLength,
                FocalLengthCurve = FocalLengthCurve,
                SensorSize = SensorSize
            };
        }
    }
}
