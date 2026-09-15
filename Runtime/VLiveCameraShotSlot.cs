using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Rig上のPreset参照と生成済みShot参照を対応付けます。
    /// </summary>
    [Serializable]
    public class VLiveCameraShotSlot
    {
        // Fields

        [Tooltip("このスロットで使用するMotion Preset。")]
        [SerializeField]
        private VLiveCameraMotionPreset _preset;

        [Tooltip("このショットに写す演者一覧。1人または複数のVLivePerformerを指定します。")]
        [SerializeField]
        private List<VLivePerformer> _performers = new();

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

        public IReadOnlyList<VLivePerformer> Performers => _performers;


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
        /// 指定した演者がこのスロットに含まれるかを返します。
        /// </summary>
        public bool ContainsPerformer(VLivePerformer performer)
        {
            return performer != null && _performers != null && _performers.Contains(performer);
        }
    }
}
