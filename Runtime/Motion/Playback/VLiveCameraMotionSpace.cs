using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Presetのターゲット基準座標をRigスケールへ変換します。
    /// </summary>
    public static class VLiveCameraMotionSpace
    {
        /// <summary>
        /// Shot開始位置へ水平距離スケールを適用します。
        /// </summary>
        public static Vector3 ScaleStartPosition(Vector3 position, float distanceScale)
        {
            float safeScale = Mathf.Max(0.01f, distanceScale);
            return new Vector3(position.x * safeScale, position.y, position.z * safeScale);
        }

        /// <summary>
        /// 軌道差分へ水平・垂直の独立スケールを適用します。
        /// </summary>
        public static Vector3 ScaleMotion(Vector3 value, float horizontalScale, float verticalScale)
        {
            float safeHorizontal = Mathf.Max(0.01f, horizontalScale);
            float safeVertical = Mathf.Max(0f, verticalScale);
            return new Vector3(value.x * safeHorizontal, value.y * safeVertical, value.z * safeHorizontal);
        }

        /// <summary>
        /// 軸別スケールを適用した軌道差分をPreset基準値へ戻します。
        /// </summary>
        public static Vector3 UnscaleMotion(Vector3 value, float horizontalScale, float verticalScale)
        {
            float safeHorizontal = Mathf.Max(0.01f, horizontalScale);
            float y = verticalScale > 0.0001f ? value.y / verticalScale : 0f;
            return new Vector3(value.x / safeHorizontal, y, value.z / safeHorizontal);
        }
    }
}
