using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// 機材固有の操作応答や推奨検証範囲を複数のMotion Presetで共有するためのプロファイルアセット。
    /// </summary>
    [CreateAssetMenu(fileName = "RigProfile", menuName = "VLiveKit/Camera/Rig Profile", order = 101)]
    public class VLiveCameraRigProfile : ScriptableObject
    {
        // Fields

        [Header("Operational Responses")]
        [Tooltip("手動速度倍率の目標値へ追従する応答時間（秒単位）。")]
        [Range(0.01f, 3f)]
        [SerializeField]
        private float _speedResponseTime = 0.5f;

        [Tooltip("Hold操作時に速度が0へ減速停止する時間（秒単位）。")]
        [Range(0.01f, 3f)]
        [SerializeField]
        private float _holdDecelerationTime = 0.4f;

        [Tooltip("Resume操作時に目標速度へ再加速する時間（秒単位）。")]
        [Range(0.01f, 3f)]
        [SerializeField]
        private float _resumeAccelerationTime = 0.4f;

        [Tooltip("Reverse操作時に停止するまでの減速時間（秒単位）。")]
        [Range(0.01f, 3f)]
        [SerializeField]
        private float _reverseDecelerationTime = 0.5f;

        [Tooltip("Reverse操作時に逆方向へ加速する時間（秒単位）。")]
        [Range(0.01f, 3f)]
        [SerializeField]
        private float _reverseAccelerationTime = 0.5f;

        [Header("Recommended Constraints")]
        [Tooltip("推奨最大移動速度（メートル/秒）。これを超えるとValidatorで警告されます。")]
        [Min(0.1f)]
        [SerializeField]
        private float _recommendedMaxSpeed = 5.0f;

        [Tooltip("推奨最大加速度（メートル/秒²）。")]
        [Min(0.1f)]
        [SerializeField]
        private float _recommendedMaxAcceleration = 10.0f;

        [Tooltip("推奨最大Jerk（メートル/秒³）。")]
        [Min(0.1f)]
        [SerializeField]
        private float _recommendedMaxJerk = 50.0f;

        [Tooltip("推奨最大角速度（度/秒）。")]
        [Min(1.0f)]
        [SerializeField]
        private float _recommendedMaxAngularSpeed = 90.0f;


        // Properties

        /// <summary>
        /// 速度変更時の応答時間を取得します。
        /// </summary>
        public float SpeedResponseTime => _speedResponseTime;

        /// <summary>
        /// Hold時の減速時間を取得します。
        /// </summary>
        public float HoldDecelerationTime => _holdDecelerationTime;

        /// <summary>
        /// Resume時の加速時間を取得します。
        /// </summary>
        public float ResumeAccelerationTime => _resumeAccelerationTime;

        /// <summary>
        /// Reverse時の減速時間を取得します。
        /// </summary>
        public float ReverseDecelerationTime => _reverseDecelerationTime;

        /// <summary>
        /// Reverse時の逆方向加速時間を取得します。
        /// </summary>
        public float ReverseAccelerationTime => _reverseAccelerationTime;

        /// <summary>
        /// 推奨最大移動速度を取得します。
        /// </summary>
        public float RecommendedMaxSpeed => _recommendedMaxSpeed;

        /// <summary>
        /// 推奨最大加速度を取得します。
        /// </summary>
        public float RecommendedMaxAcceleration => _recommendedMaxAcceleration;

        /// <summary>
        /// 推奨最大Jerkを取得します。
        /// </summary>
        public float RecommendedMaxJerk => _recommendedMaxJerk;

        /// <summary>
        /// 推奨最大角速度を取得します。
        /// </summary>
        public float RecommendedMaxAngularSpeed => _recommendedMaxAngularSpeed;


        // Methods

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_speedResponseTime < 0.01f)
            {
                _speedResponseTime = 0.01f;
            }

            if (_holdDecelerationTime < 0.01f)
            {
                _holdDecelerationTime = 0.01f;
            }

            if (_resumeAccelerationTime < 0.01f)
            {
                _resumeAccelerationTime = 0.01f;
            }

            if (_reverseDecelerationTime < 0.01f)
            {
                _reverseDecelerationTime = 0.01f;
            }

            if (_reverseAccelerationTime < 0.01f)
            {
                _reverseAccelerationTime = 0.01f;
            }

            if (_recommendedMaxSpeed < 0.1f)
            {
                _recommendedMaxSpeed = 0.1f;
            }

            if (_recommendedMaxAcceleration < 0.1f)
            {
                _recommendedMaxAcceleration = 0.1f;
            }

            if (_recommendedMaxJerk < 0.1f)
            {
                _recommendedMaxJerk = 0.1f;
            }

            if (_recommendedMaxAngularSpeed < 1.0f)
            {
                _recommendedMaxAngularSpeed = 1.0f;
            }
        }
#endif
    }
}
