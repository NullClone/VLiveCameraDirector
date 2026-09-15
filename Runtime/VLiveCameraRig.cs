using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace VLiveKit.Camera
{
    /// <summary>
    /// カメラリグ全体の基準座標、共通演者、Physical Camera設定、スケール、順序付きShotスロットを保持します。
    /// </summary>
    [DisallowMultipleComponent]
    public class VLiveCameraRig : MonoBehaviour
    {
        // Fields

        [Tooltip("カメラ軌道の原点となるTransform。未設定時はCamera Director自身を使用します。被写体参照とは独立しています。")]
        [FormerlySerializedAs("_performerTarget")]
        [SerializeField]
        private Transform _referenceTransform;

        [Tooltip("Program映像を出力するUnity Camera。CinemachineBrainはこのカメラから取得されます。")]
        [SerializeField]
        private UnityEngine.Camera _programCamera;

        [Tooltip("通常の全Shotに写す共通演者一覧。Shot SlotでOverrideを有効にした場合だけ、その一覧へ置き換わります。")]
        [SerializeField]
        private List<VLivePerformer> _performers = new();

        [Tooltip("正面方向の基準モード（ReferenceForward: Referenceの正面XZ, WorldPlusZ: World +Z, WorldMinusZ: World -Z, CustomReference: 指定Transformの正面XZ）。")]
        [SerializeField]
        private ForwardReferenceMode _forwardReferenceMode = ForwardReferenceMode.ReferenceForward;

        [Tooltip("正面基準がCustomReferenceの場合に使用するTransform。向き（forwardのXZ射影）のみ使用します。")]
        [SerializeField]
        private Transform _customReference;

        [Tooltip("全ShotとProgram Cameraへ適用するセンサーサイズ。単位はミリメートルです。")]
        [SerializeField]
        private Vector2 _sensorSize = new Vector2(36f, 24f);

        [Tooltip("センサーと出力アスペクトが異なる場合のGate Fit方式。")]
        [SerializeField]
        private UnityEngine.Camera.GateFitMode _gateFit = UnityEngine.Camera.GateFitMode.Horizontal;

        [Tooltip("光学中心をセンサー中央からずらす量。通常はゼロを使用します。")]
        [SerializeField]
        private Vector2 _lensShift = Vector2.zero;

        [Tooltip("全ShotのNear Clip Plane。メートル単位で、接写を欠かない範囲で大きくします。")]
        [Min(0.001f)]
        [SerializeField]
        private float _nearClipPlane = 0.1f;

        [Tooltip("全ShotのFar Clip Plane。メートル単位で、必要な背景を含む最小値にします。")]
        [Min(0.01f)]
        [SerializeField]
        private float _farClipPlane = 1000f;

        [Tooltip("カメラ開始位置の水平距離スケール。1.0が基準距離。0より大きい値。")]
        [Min(0.01f)]
        [SerializeField]
        private float _distanceScale = 1.0f;

        [Tooltip("スプライン制御点の水平移動幅スケール。1.0が基準移動幅です。")]
        [Min(0.01f)]
        [SerializeField]
        private float _motionScale = 1.0f;

        [Tooltip("スプライン制御点の垂直移動幅スケール。1.0が基準移動幅です。")]
        [Min(0f)]
        [SerializeField]
        private float _verticalMotionScale = 1.0f;

        [Tooltip("Rig内の全Shotへ掛ける再生速度倍率。Presetの形状と個別速度は変更しません。")]
        [Range(0.1f, 4f)]
        [SerializeField]
        private float _masterPlaybackSpeed = 1.25f;

        [Tooltip("番号順に管理されるShotスロット一覧。これがShot順の唯一の正本です。")]
        [SerializeField]
        private List<VLiveCameraShotSlot> _slots = new();


        // Properties

        /// <summary>
        /// カメラ運動の基準Transformを取得または設定します。
        /// </summary>
        public Transform ReferenceTransform
        {
            get => _referenceTransform;
            set => _referenceTransform = value;
        }

        /// <summary>
        /// Program出力Cameraを取得または設定します。
        /// </summary>
        public UnityEngine.Camera ProgramCamera
        {
            get => _programCamera;
            set => _programCamera = value;
        }

        /// <summary>
        /// Program出力Cameraに付属するCinemachineBrainを取得します。
        /// </summary>
        public CinemachineBrain CinemachineBrain
        {
            get
            {
                if (_programCamera != null)
                {
                    return _programCamera.GetComponent<CinemachineBrain>();
                }

                return null;
            }
        }

        /// <summary>
        /// 通常の全Shotに使用する共通演者一覧を取得します。
        /// </summary>
        public IReadOnlyList<VLivePerformer> Performers => _performers;

        /// <summary>
        /// 正面方向の基準モードを取得または設定します。
        /// </summary>
        public ForwardReferenceMode ForwardMode
        {
            get => _forwardReferenceMode;
            set => _forwardReferenceMode = value;
        }

        /// <summary>
        /// 正面基準のCustom Reference Transformを取得または設定します。
        /// </summary>
        public Transform CustomReference
        {
            get => _customReference;
            set => _customReference = value;
        }

        /// <summary>
        /// 共通センサーサイズを取得または設定します。
        /// </summary>
        public Vector2 SensorSize
        {
            get => _sensorSize;
            set => _sensorSize = new Vector2(Mathf.Max(0.1f, value.x), Mathf.Max(0.1f, value.y));
        }

        /// <summary>
        /// 共通Gate Fit方式を取得または設定します。
        /// </summary>
        public UnityEngine.Camera.GateFitMode GateFit
        {
            get => _gateFit;
            set => _gateFit = value;
        }

        /// <summary>
        /// 共通Lens Shiftを取得または設定します。
        /// </summary>
        public Vector2 LensShift
        {
            get => _lensShift;
            set => _lensShift = value;
        }

        /// <summary>
        /// 共通Near Clip Planeを取得または設定します。
        /// </summary>
        public float NearClipPlane
        {
            get => _nearClipPlane;
            set => _nearClipPlane = Mathf.Max(0.001f, value);
        }

        /// <summary>
        /// 共通Far Clip Planeを取得または設定します。
        /// </summary>
        public float FarClipPlane
        {
            get => _farClipPlane;
            set => _farClipPlane = Mathf.Max(_nearClipPlane + 0.01f, value);
        }

        /// <summary>
        /// 水平距離スケールを取得または設定します。
        /// </summary>
        public float DistanceScale
        {
            get => _distanceScale;
            set => _distanceScale = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// 移動幅スケールを取得または設定します。
        /// </summary>
        public float MotionScale
        {
            get => _motionScale;
            set => _motionScale = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// 垂直移動幅スケールを取得または設定します。
        /// </summary>
        public float VerticalMotionScale
        {
            get => _verticalMotionScale;
            set => _verticalMotionScale = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Rig内の全Shotへ適用する再生速度倍率を取得または設定します。
        /// </summary>
        public float MasterPlaybackSpeed
        {
            get => _masterPlaybackSpeed;
            set => _masterPlaybackSpeed = Mathf.Clamp(value, 0.1f, 4f);
        }

        /// <summary>
        /// 管理されている順序付きShotスロット一覧を取得します。
        /// </summary>
        public IReadOnlyList<VLiveCameraShotSlot> Slots => _slots;

        /// <summary>
        /// 管理されているShotスロット数を取得します。
        /// </summary>
        public int SlotCount => _slots != null ? _slots.Count : 0;


        // Methods

#if UNITY_EDITOR
        /// <summary>
        /// Editor専用: 初期セットアップ時にスロットを追加します。
        /// </summary>
        public void AddSlot(VLiveCameraShotSlot slot)
        {
            _slots ??= new List<VLiveCameraShotSlot>();

            _slots.Add(slot);
        }

        /// <summary>
        /// Editor専用: スロットリストをクリアします。
        /// </summary>
        public void ClearSlots()
        {
            _slots?.Clear();
        }
#endif

        /// <summary>
        /// 現在の設定に基づく正面方向の正規化ベクトルを取得します。
        /// </summary>
        public Vector3 GetReferenceForward()
        {
            switch (_forwardReferenceMode)
            {
                case ForwardReferenceMode.ReferenceForward:
                    Transform reference = _referenceTransform != null ? _referenceTransform : transform;
                    if (reference != null)
                    {
                        Vector3 fwd = Vector3.ProjectOnPlane(reference.forward, Vector3.up);
                        if (fwd.sqrMagnitude > 0.0001f)
                        {
                            return fwd.normalized;
                        }
                    }

                    return Vector3.forward;

                case ForwardReferenceMode.WorldPlusZ:
                    return Vector3.forward;

                case ForwardReferenceMode.WorldMinusZ:
                    return Vector3.back;

                case ForwardReferenceMode.CustomReference:
                    if (_customReference != null)
                    {
                        Vector3 fwd = Vector3.ProjectOnPlane(_customReference.forward, Vector3.up);
                        if (fwd.sqrMagnitude > 0.0001f)
                        {
                            return fwd.normalized;
                        }

                        Debug.LogWarning("[VLiveCameraRig] Custom Reference の forward が垂直方向のため World +Z にフォールバックしました。", this);
                    }
                    else
                    {
                        Debug.LogWarning("[VLiveCameraRig] Custom Reference が未設定のため World +Z にフォールバックしました。", this);
                    }

                    return Vector3.forward;

                default:
                    return Vector3.forward;
            }
        }

        /// <summary>
        /// 現在の正面基準設定が有効であるかを検証します。CustomReference モードで Transform が未指定または垂直方向の場合は false を返します。
        /// </summary>
        public bool IsForwardReferenceValid()
        {
            if (_forwardReferenceMode == ForwardReferenceMode.CustomReference)
            {
                if (_customReference == null)
                {
                    return false;
                }

                Vector3 fwd = Vector3.ProjectOnPlane(_customReference.forward, Vector3.up);
                return fwd.sqrMagnitude >= 0.0001f;
            }

            return true;
        }

        /// <summary>
        /// カメラ運動の基準位置を取得します。
        /// </summary>
        public Vector3 GetReferencePosition()
        {
            return _referenceTransform != null ? _referenceTransform.position : transform.position;
        }

        /// <summary>
        /// 正面方向を向く回転クォータニオンを取得します。
        /// </summary>
        public Quaternion GetReferenceOrientation()
        {
            return Quaternion.LookRotation(GetReferenceForward(), Vector3.up);
        }

        /// <summary>
        /// 指定されたスロット番号（0始まり）のShotを取得します。範囲外または未設定の場合はnullを返します。
        /// </summary>
        public VLiveCameraShot GetShot(int index)
        {
            if (_slots == null || index < 0 || index >= _slots.Count)
            {
                return null;
            }

            return _slots[index]?.Shot;
        }

        /// <summary>
        /// 指定Slotに対して、Shot固有OverrideまたはRig共通の演者一覧を解決します。
        /// </summary>
        public IReadOnlyList<VLivePerformer> ResolvePerformers(VLiveCameraShotSlot slot)
        {
            return slot != null ? slot.ResolvePerformers(_performers) : _performers;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _sensorSize.x = Mathf.Max(0.1f, _sensorSize.x);
            _sensorSize.y = Mathf.Max(0.1f, _sensorSize.y);
            _nearClipPlane = Mathf.Max(0.001f, _nearClipPlane);
            _farClipPlane = Mathf.Max(_nearClipPlane + 0.01f, _farClipPlane);

            if (_distanceScale < 0.01f)
            {
                _distanceScale = 0.01f;
            }

            if (_motionScale < 0.01f)
            {
                _motionScale = 0.01f;
            }

            if (_verticalMotionScale < 0f)
            {
                _verticalMotionScale = 0f;
            }

            _masterPlaybackSpeed = Mathf.Clamp(_masterPlaybackSpeed, 0.1f, 4f);

            if (_slots == null)
            {
                _slots = new List<VLiveCameraShotSlot>();
            }

            if (_performers == null)
            {
                _performers = new List<VLivePerformer>();
            }
        }
#endif
    }

    /// <summary>
    /// カメラ配置の正面方向を決める基準。
    /// </summary>
    public enum ForwardReferenceMode
    {
        ReferenceForward,
        WorldPlusZ,
        WorldMinusZ,
        CustomReference
    }
}
