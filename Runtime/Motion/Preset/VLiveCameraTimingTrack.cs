using System;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Motion Presetの再生時間、速度プロファイル、手動速度範囲を保持します。
    /// </summary>
    [Serializable]
    public class VLiveCameraTimingTrack
    {
        // Fields

        [Tooltip("基準再生時間。単位は秒です。")]
        [Min(0.1f)]
        [SerializeField]
        private float _clipDuration = 4f;

        [Tooltip("正規化時間に対する軌道進行度。0から1へ単調増加させます。")]
        [SerializeField]
        private AnimationCurve _progressCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Rigスケール変更時に再生時間と移動速度のどちらを維持するか。")]
        [SerializeField]
        private ScaleTimingMode _scaleTimingMode = ScaleTimingMode.PreserveSpeed;

        [Tooltip("オペレーターが選択できる最小速度倍率。")]
        [Min(0f)]
        [SerializeField]
        private float _minSpeedMultiplier = 0.25f;

        [Tooltip("オペレーターが選択できる最大速度倍率。")]
        [Min(0.1f)]
        [SerializeField]
        private float _maxSpeedMultiplier = 3f;

        [Tooltip("速度変更入力一回あたりの倍率増減量。")]
        [Min(0.01f)]
        [SerializeField]
        private float _speedStep = 0.25f;


        // Properties

        public float ClipDuration
        {
            get => _clipDuration;
            set => _clipDuration = Mathf.Max(0.1f, value);
        }

        public AnimationCurve ProgressCurve
        {
            get => _progressCurve;
            set => _progressCurve = VLiveCameraCurveUtility.CloneOrDefault(value, AnimationCurve.Linear(0f, 0f, 1f, 1f));
        }

        public ScaleTimingMode ScaleMode
        {
            get => _scaleTimingMode;
            set => _scaleTimingMode = value;
        }

        public float MinSpeedMultiplier
        {
            get => _minSpeedMultiplier;
            set => _minSpeedMultiplier = Mathf.Max(0f, value);
        }

        public float MaxSpeedMultiplier
        {
            get => _maxSpeedMultiplier;
            set => _maxSpeedMultiplier = Mathf.Max(0.1f, value);
        }

        public float SpeedStep
        {
            get => _speedStep;
            set => _speedStep = Mathf.Max(0.01f, value);
        }


        // Methods

        /// <summary>
        /// AnimationCurveを含む独立した複製を作成します。
        /// </summary>
        public VLiveCameraTimingTrack Clone()
        {
            return new VLiveCameraTimingTrack
            {
                ClipDuration = ClipDuration,
                ProgressCurve = ProgressCurve,
                ScaleMode = ScaleMode,
                MinSpeedMultiplier = MinSpeedMultiplier,
                MaxSpeedMultiplier = MaxSpeedMultiplier,
                SpeedStep = SpeedStep
            };
        }

        /// <summary>
        /// 速度範囲と時間を有効値へ補正します。
        /// </summary>
        public void Sanitize()
        {
            _clipDuration = Mathf.Max(0.1f, _clipDuration);
            _minSpeedMultiplier = Mathf.Max(0f, _minSpeedMultiplier);
            _maxSpeedMultiplier = Mathf.Max(0.1f, _maxSpeedMultiplier);
            _maxSpeedMultiplier = Mathf.Max(_minSpeedMultiplier, _maxSpeedMultiplier);
            _speedStep = Mathf.Max(0.01f, _speedStep);
            _progressCurve = VLiveCameraCurveUtility.CloneOrDefault(_progressCurve, AnimationCurve.Linear(0f, 0f, 1f, 1f));
        }
    }
}
