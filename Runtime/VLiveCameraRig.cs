using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// VLiveCameraRigで管理される1つのショットスロット。
    /// Preset参照とそれに対応して生成されたShot参照を保持します。
    /// </summary>
    [Serializable]
    public class VLiveCameraShotSlot
    {
        // Fields

        [Tooltip("このスロットで使用するMotion Preset。")]
        [SerializeField]
        private VLiveCameraMotionPreset _preset;

        [Tooltip("このスロットに対応して生成されたShot。")]
        [SerializeField]
        private VLiveCameraShot _shot;


        // Properties

        /// <summary>
        /// 使用するMotion Presetを取得または設定します。
        /// </summary>
        public VLiveCameraMotionPreset Preset
        {
            get => _preset;
            set => _preset = value;
        }

        /// <summary>
        /// 生成されたShotコンポーネントを取得します。
        /// </summary>
        public VLiveCameraShot Shot => _shot;


        // Methods

        /// <summary>
        /// 空のショットスロットを作成します。
        /// </summary>
        public VLiveCameraShotSlot() { }

        /// <summary>
        /// 指定されたPresetを持つショットスロットを作成します。
        /// </summary>
        public VLiveCameraShotSlot(VLiveCameraMotionPreset preset)
        {
            _preset = preset;
            _shot = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor専用: 指定されたPresetとShotを持つショットスロットを作成します。
        /// </summary>
        public VLiveCameraShotSlot(VLiveCameraMotionPreset preset, VLiveCameraShot shot)
        {
            _preset = preset;
            _shot = shot;
        }

        /// <summary>
        /// Editor専用: 生成・再構築されたShot参照を設定します。
        /// </summary>
        public void SetShot(VLiveCameraShot shot)
        {
            _shot = shot;
        }
#endif
    }

    /// <summary>
    /// カメラ配置の正面方向を決定する基準モード。
    /// </summary>
    public enum ForwardReferenceMode
    {
        TargetForward,
        WorldPlusZ,
        WorldMinusZ,
        CustomReference
    }

    /// <summary>
    /// カメラリグ全体の構成、注視対象、正面基準、スケール、順序付きShotスロットを保持する正本コンポーネント。
    /// </summary>
    [DisallowMultipleComponent]
    public class VLiveCameraRig : MonoBehaviour
    {
        // Fields

        [Header("Target & Output (対象と出力設定)")]
        [Tooltip("カメラワークの注視・追従基準となる演者Transform。")]
        [SerializeField]
        private Transform _performerTarget;

        [Tooltip("Program映像を出力するUnity Camera。CinemachineBrainはこのカメラから取得されます。")]
        [SerializeField]
        private UnityEngine.Camera _programCamera;

        [Header("Orientation & Framing (正面基準と構図)")]
        [Tooltip("正面方向の基準モード（TargetForward: Targetの正面XZ, WorldPlusZ: World +Z, WorldMinusZ: World -Z, CustomReference: 指定Transformの正面XZ）。")]
        [SerializeField]
        private ForwardReferenceMode _forwardReferenceMode = ForwardReferenceMode.TargetForward;

        [Tooltip("正面基準がCustomReferenceの場合に使用するTransform。向き（forwardのXZ射影）のみ使用します。")]
        [SerializeField]
        private Transform _customReference;

        [Tooltip("Target原点から注視点までの高さオフセット（メートル単位）。")]
        [Min(0f)]
        [SerializeField]
        private float _targetHeight = 1.3f;

        [Tooltip("カメラ開始位置の水平距離スケール。1.0が基準距離。0より大きい値。")]
        [Min(0.01f)]
        [SerializeField]
        private float _distanceScale = 1.0f;

        [Tooltip("スプライン制御点の移動幅スケール。1.0が基準移動幅。0より大きい値。")]
        [Min(0.01f)]
        [SerializeField]
        private float _motionScale = 1.0f;

        [Header("Shot Slots (ショットスロット一覧)")]
        [Tooltip("番号順に管理されるShotスロット一覧。これがShot順の唯一の正本です。")]
        [SerializeField]
        private List<VLiveCameraShotSlot> _slots = new List<VLiveCameraShotSlot>();


        // Properties

        /// <summary>
        /// 演者Target Transformを取得または設定します。
        /// </summary>
        public Transform PerformerTarget
        {
            get => _performerTarget;
            set => _performerTarget = value;
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
        /// Target原点からの注視基準高さを取得または設定します。
        /// </summary>
        public float TargetHeight
        {
            get => _targetHeight;
            set => _targetHeight = Mathf.Max(0f, value);
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
            if (_slots == null)
            {
                _slots = new List<VLiveCameraShotSlot>();
            }

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
                case ForwardReferenceMode.TargetForward:
                    if (_performerTarget != null)
                    {
                        Vector3 fwd = Vector3.ProjectOnPlane(_performerTarget.forward, Vector3.up);
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_targetHeight < 0f)
            {
                _targetHeight = 0f;
            }

            if (_distanceScale < 0.01f)
            {
                _distanceScale = 0.01f;
            }

            if (_motionScale < 0.01f)
            {
                _motionScale = 0.01f;
            }

            if (_slots == null)
            {
                _slots = new List<VLiveCameraShotSlot>();
            }
        }
#endif
    }
}
