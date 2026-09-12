using System;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// 水平維持と演出的なロール変化を保持します。
    /// </summary>
    [Serializable]
    public class VLiveCameraRollTrack
    {
        // Fields

        [Tooltip("水平維持、角度カーブ、Spline Upのいずれでロールを制御するか。")]
        [SerializeField]
        private RollMode _rollMode = RollMode.MaintainHorizon;

        [Tooltip("進行度に応じたロール角度。単位は度です。")]
        [SerializeField]
        private AnimationCurve _rollCurve = new AnimationCurve();


        // Properties

        public RollMode Mode
        {
            get => _rollMode;
            set => _rollMode = value;
        }

        public AnimationCurve Curve
        {
            get => _rollCurve;
            set => _rollCurve = VLiveCameraCurveUtility.Clone(value);
        }


        // Methods

        /// <summary>
        /// AnimationCurveを含む独立した複製を作成します。
        /// </summary>
        public VLiveCameraRollTrack Clone()
        {
            return new VLiveCameraRollTrack
            {
                Mode = Mode,
                Curve = Curve
            };
        }
    }
}
