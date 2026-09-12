using System;
using UnityEngine;
using UnityEngine.Splines;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Preset空間におけるSplineのKnot情報。
    /// </summary>
    [Serializable]
    public struct MotionKnot
    {
        [Tooltip("ターゲットローカル空間における制御点位置。")]
        public Vector3 Position;

        [Tooltip("Knotへ入る接線ベクトル。")]
        public Vector3 TangentIn;

        [Tooltip("Knotから出る接線ベクトル。")]
        public Vector3 TangentOut;

        [Tooltip("Knotの接線モード。")]
        public TangentMode TangentMode;

        [Tooltip("Auto Smooth時の接線張力。0から1の範囲です。")]
        [Range(0f, 1f)]
        public float AutoSmoothTension;

        [Tooltip("Knotのローカル回転。")]
        public Quaternion Rotation;


        /// <summary>
        /// 指定位置にContinuous Knotを作成します。
        /// </summary>
        public MotionKnot(Vector3 position)
        {
            Position = position;
            TangentIn = Vector3.zero;
            TangentOut = Vector3.zero;
            TangentMode = TangentMode.Continuous;
            AutoSmoothTension = 1f / 3f;
            Rotation = Quaternion.identity;
        }

        /// <summary>
        /// すべてのSpline情報を指定してKnotを作成します。
        /// </summary>
        public MotionKnot(
            Vector3 position,
            Vector3 tangentIn,
            Vector3 tangentOut,
            TangentMode tangentMode,
            Quaternion rotation,
            float autoSmoothTension = 1f / 3f)
        {
            Position = position;
            TangentIn = tangentIn;
            TangentOut = tangentOut;
            TangentMode = tangentMode;
            AutoSmoothTension = Mathf.Clamp01(autoSmoothTension);
            Rotation = rotation;
        }
    }
}
