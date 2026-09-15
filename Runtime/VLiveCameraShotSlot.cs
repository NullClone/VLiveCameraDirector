using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Rig上のPreset参照、任意の演者Override、生成済みShot参照を対応付けます。
    /// </summary>
    [Serializable]
    public class VLiveCameraShotSlot
    {
        // Fields

        private const int PerformerAssignmentVersion = 1;

        [Tooltip("このスロットで使用するMotion Preset。")]
        [SerializeField]
        private VLiveCameraMotionPreset _preset;

        [Tooltip("有効時はRig共通の演者一覧を使用せず、このShot固有の演者一覧を使用します。")]
        [SerializeField]
        private bool _usePerformerOverride;

        [Tooltip("Override有効時にこのShotへ写す演者一覧。1人または複数のVLivePerformerを指定します。")]
        [SerializeField]
        private List<VLivePerformer> _performers = new();

        [HideInInspector]
        [SerializeField]
        private int _performerAssignmentVersion;

        [Tooltip("このスロットに対応する生成済みShot。")]
        [SerializeField]
        private VLiveCameraShot _shot;


        // Properties

        public VLiveCameraMotionPreset Preset
        {
            get => _preset;
            set => _preset = value;
        }

        public VLiveCameraShot Shot => _shot;

        /// <summary>
        /// Rig共通の演者一覧を置き換えるかどうかを取得または設定します。
        /// </summary>
        public bool UsePerformerOverride
        {
            get => UsesPerformerOverride;
            set
            {
                _usePerformerOverride = value;
                _performerAssignmentVersion = PerformerAssignmentVersion;
            }
        }

        /// <summary>
        /// Shot固有の演者Override一覧を取得します。
        /// </summary>
        public IReadOnlyList<VLivePerformer> PerformerOverride => _performers;

        private bool UsesPerformerOverride => _performerAssignmentVersion == 0
            ? _performers != null && _performers.Count > 0
            : _usePerformerOverride;


        // Methods

        /// <summary>
        /// 空のShotスロットを作成します。
        /// </summary>
        public VLiveCameraShotSlot() { }

        /// <summary>
        /// 指定Presetを持つShotスロットを作成します。
        /// </summary>
        public VLiveCameraShotSlot(VLiveCameraMotionPreset preset)
        {
            _preset = preset;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor生成時にPresetとShotを対応付けます。
        /// </summary>
        public VLiveCameraShotSlot(VLiveCameraMotionPreset preset, VLiveCameraShot shot)
        {
            _preset = preset;
            _shot = shot;
        }

        /// <summary>
        /// 再構築後のShot参照を設定します。
        /// </summary>
        public void SetShot(VLiveCameraShot shot)
        {
            _shot = shot;
        }
#endif

        /// <summary>
        /// Rig共通一覧を考慮し、このSlotで使用する演者一覧を返します。
        /// </summary>
        internal IReadOnlyList<VLivePerformer> ResolvePerformers(IReadOnlyList<VLivePerformer> rigPerformers)
        {
            return UsesPerformerOverride ? _performers : rigPerformers;
        }
    }
}
