using System;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// カメラ本体の三次元移動軌道を保持します。
    /// </summary>
    [Serializable]
    public class VLiveCameraBodyTrack
    {
        // Fields

        [Tooltip("ターゲット基準空間で定義した完全なスプライン結節点配列。")]
        [SerializeField]
        private MotionKnot[] _knots = Array.Empty<MotionKnot>();

        [Tooltip("スプラインを閉ループとして生成するか。")]
        [SerializeField]
        private bool _isClosed;

        [Tooltip("基準スケールでの軌道長。Preserve Speedの時間解決に使用します。")]
        [Min(0f)]
        [SerializeField]
        private float _referenceSplineLength;

        [Tooltip("再生開始距離。0で軌道の始点を使用します。")]
        [Min(0f)]
        [SerializeField]
        private float _startDistance;

        [Tooltip("再生終了距離。0で軌道の終点を使用します。")]
        [Min(0f)]
        [SerializeField]
        private float _endDistance;


        // Properties

        public MotionKnot[] Knots
        {
            get => _knots;
            set => _knots = value ?? Array.Empty<MotionKnot>();
        }

        public bool IsClosed
        {
            get => _isClosed;
            set => _isClosed = value;
        }

        public float ReferenceSplineLength
        {
            get => _referenceSplineLength;
            set => _referenceSplineLength = Mathf.Max(0f, value);
        }

        public float StartDistance
        {
            get => _startDistance;
            set => _startDistance = Mathf.Max(0f, value);
        }

        public float EndDistance
        {
            get => _endDistance;
            set => _endDistance = Mathf.Max(0f, value);
        }


        // Methods

        /// <summary>
        /// 結節点配列を含む独立した複製を作成します。
        /// </summary>
        public VLiveCameraBodyTrack Clone()
        {
            return new VLiveCameraBodyTrack
            {
                Knots = (MotionKnot[])Knots.Clone(),
                IsClosed = IsClosed,
                ReferenceSplineLength = ReferenceSplineLength,
                StartDistance = StartDistance,
                EndDistance = EndDistance
            };
        }
    }
}
