using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// カメラの構図および移動軌道の初期設定を保持するScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "MotionPreset", menuName = "VLiveKit/Camera/Motion Preset", order = 100)]
    public class VLiveCameraMotionPreset : ScriptableObject
    {
        // Fields

        [Header("Preset Information (プリセット情報)")]
        [Tooltip("InspectorおよびSetup Windowで表示されるプリセット名。")]
        [SerializeField]
        private string _displayName = "New Motion Preset";

        [Tooltip("ショットの種別（Fixed: 固定構図, Spline: スプライン移動）。")]
        [SerializeField]
        private VLiveCameraShot.ShotType _shotType = VLiveCameraShot.ShotType.Fixed;

        [Header("Composition (構図設定)")]
        [Tooltip("カメラの画角（Field of View、度単位）。")]
        [Range(1f, 179f)]
        [SerializeField]
        private float _fieldOfView = 40f;

        [Tooltip("追従ターゲットの中心からのオフセット（メートル単位）。")]
        [SerializeField]
        private Vector3 _targetOffset = new Vector3(0f, 1.2f, 0f);

        [Header("Trajectory (軌道設定)")]
        [Tooltip("ターゲットローカル座標系における相対制御点。Fixedの場合は初期配置、Splineの場合はスプラインの制御点として使用します。")]
        [SerializeField]
        private Vector3[] _controlPoints = new Vector3[0];

        [Header("Motion Settings (移動設定)")]
        [Tooltip("スプライン移動の初期進行速度（正規化位置/秒）。")]
        [Min(0f)]
        [SerializeField]
        private float _initialSpeed = 0.2f;

        [Tooltip("終端へ近づいた際に減速を開始する正規化距離。")]
        [Min(0f)]
        [SerializeField]
        private float _decelerationDistance = 0.25f;


        // Properties

        /// <summary>
        /// プリセットの表示名を取得します。
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// ショット種別（FixedまたはSpline）を取得します。
        /// </summary>
        public VLiveCameraShot.ShotType ShotType => _shotType;

        /// <summary>
        /// カメラの画角（Field of View、度単位）を取得します。
        /// </summary>
        public float FieldOfView => _fieldOfView;

        /// <summary>
        /// 追従ターゲットからのオフセットを取得します。
        /// </summary>
        public Vector3 TargetOffset => _targetOffset;

        /// <summary>
        /// ターゲットローカル座標系における相対制御点の配列を取得します。
        /// </summary>
        public Vector3[] ControlPoints => _controlPoints;

        /// <summary>
        /// スプライン移動の初期進行速度を取得します。
        /// </summary>
        public float InitialSpeed => _initialSpeed;

        /// <summary>
        /// 終端への減速距離を取得します。
        /// </summary>
        public float DecelerationDistance => _decelerationDistance;


        // Methods

        /// <summary>
        /// プリセットの各初期値を設定します（Asset生成や初期化用）。
        /// </summary>
        /// <param name="displayName">表示名。</param>
        /// <param name="shotType">ショット種別。</param>
        /// <param name="fieldOfView">画角。</param>
        /// <param name="targetOffset">ターゲットオフセット。</param>
        /// <param name="controlPoints">相対制御点一覧。</param>
        /// <param name="initialSpeed">初期進行速度。</param>
        /// <param name="decelerationDistance">減速距離。</param>
        public void Initialize(
            string displayName,
            VLiveCameraShot.ShotType shotType,
            float fieldOfView,
            Vector3 targetOffset,
            Vector3[] controlPoints,
            float initialSpeed,
            float decelerationDistance)
        {
            _displayName = displayName;
            _shotType = shotType;
            _fieldOfView = fieldOfView;
            _targetOffset = targetOffset;
            _controlPoints = controlPoints;
            _initialSpeed = initialSpeed;
            _decelerationDistance = decelerationDistance;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_fieldOfView < 1f)
            {
                _fieldOfView = 1f;
            }
            else if (_fieldOfView > 179f)
            {
                _fieldOfView = 179f;
            }

            if (_initialSpeed < 0f)
            {
                _initialSpeed = 0f;
            }

            if (_decelerationDistance < 0f)
            {
                _decelerationDistance = 0f;
            }

            if (_controlPoints == null)
            {
                _controlPoints = new Vector3[0];
            }
        }
#endif
    }
}
