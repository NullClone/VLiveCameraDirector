using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Motion Evaluatorによって計算された単一時刻におけるカメラ評価サンプル。
    /// </summary>
    public struct VLiveCameraMotionSample
    {
        // Fields

        public float PlaybackTime;
        public float Phase;
        public float Progress;
        public float SplineDistance;
        public Vector3 AimOffset;
        public Vector2 ScreenPosition;
        public float FieldOfView;
        public float FocalLength;
        public float Roll;
        public bool IsValid;
    }
}
