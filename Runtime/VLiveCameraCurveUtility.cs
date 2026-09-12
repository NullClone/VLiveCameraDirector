using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Motion Trackで使用するAnimationCurveの複製と検証を行います。
    /// </summary>
    public static class VLiveCameraCurveUtility
    {
        // Methods

        /// <summary>
        /// AnimationCurveをWrap Modeを含めて複製します。
        /// </summary>
        public static AnimationCurve Clone(AnimationCurve source)
        {
            if (source == null)
            {
                return new AnimationCurve();
            }

            var clone = new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };

            return clone;
        }

        /// <summary>
        /// 有効なCurveを複製し、無効な場合は指定した既定Curveを返します。
        /// </summary>
        public static AnimationCurve CloneOrDefault(AnimationCurve source, AnimationCurve fallback)
        {
            return source != null && source.length > 0 ? Clone(source) : Clone(fallback);
        }
    }
}
