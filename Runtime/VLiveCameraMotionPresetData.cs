using System;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// すべてのCamera Performanceトラックを単一時間軸で束ねます。
    /// </summary>
    [Serializable]
    public class VLiveCameraMotionPresetData
    {
        // Fields

        [Tooltip("プリセットの識別情報と演出意図。")]
        [SerializeField]
        private VLiveCameraMotionIdentity _identity = new VLiveCameraMotionIdentity();

        [Tooltip("カメラ本体の三次元移動軌道。")]
        [SerializeField]
        private VLiveCameraBodyTrack _body = new VLiveCameraBodyTrack();

        [Tooltip("再生時間と速度プロファイル。")]
        [SerializeField]
        private VLiveCameraTimingTrack _timing = new VLiveCameraTimingTrack();

        [Tooltip("注視点と画面内構図の時間変化。")]
        [SerializeField]
        private VLiveCameraAimTrack _aim = new VLiveCameraAimTrack();

        [Tooltip("レンズの基準値と時間変化。")]
        [SerializeField]
        private VLiveCameraLensTrack _lens = new VLiveCameraLensTrack();

        [Tooltip("カメラロールの制御設定。")]
        [SerializeField]
        private VLiveCameraRollTrack _roll = new VLiveCameraRollTrack();

        [Tooltip("Program選択時の開始状態と有効再生区間。")]
        [SerializeField]
        private VLiveCameraActivationTrack _activation = new VLiveCameraActivationTrack();

        [Tooltip("機材固有の応答と診断基準。未設定時は既定値を使用します。")]
        [SerializeField]
        private VLiveCameraRigProfile _rigProfile;


        // Properties

        public VLiveCameraMotionIdentity Identity
        {
            get => _identity;
            set => _identity = value ?? new VLiveCameraMotionIdentity();
        }

        public VLiveCameraBodyTrack Body
        {
            get => _body;
            set => _body = value ?? new VLiveCameraBodyTrack();
        }

        public VLiveCameraTimingTrack Timing
        {
            get => _timing;
            set => _timing = value ?? new VLiveCameraTimingTrack();
        }

        public VLiveCameraAimTrack Aim
        {
            get => _aim;
            set => _aim = value ?? new VLiveCameraAimTrack();
        }

        public VLiveCameraLensTrack Lens
        {
            get => _lens;
            set => _lens = value ?? new VLiveCameraLensTrack();
        }

        public VLiveCameraRollTrack Roll
        {
            get => _roll;
            set => _roll = value ?? new VLiveCameraRollTrack();
        }

        public VLiveCameraActivationTrack Activation
        {
            get => _activation;
            set => _activation = value ?? new VLiveCameraActivationTrack();
        }

        public VLiveCameraRigProfile RigProfile
        {
            get => _rigProfile;
            set => _rigProfile = value;
        }


        // Methods

        /// <summary>
        /// 全トラックを複製した独立データを作成します。
        /// </summary>
        public VLiveCameraMotionPresetData Clone()
        {
            return new VLiveCameraMotionPresetData
            {
                Identity = Identity.Clone(),
                Body = Body.Clone(),
                Timing = Timing.Clone(),
                Aim = Aim.Clone(),
                Lens = Lens.Clone(),
                Roll = Roll.Clone(),
                Activation = Activation.Clone(),
                RigProfile = RigProfile
            };
        }

        /// <summary>
        /// 欠落トラックと無効な時間値を補正します。
        /// </summary>
        public void Sanitize()
        {
            Identity ??= new VLiveCameraMotionIdentity();
            Body ??= new VLiveCameraBodyTrack();
            Timing ??= new VLiveCameraTimingTrack();
            Aim ??= new VLiveCameraAimTrack();
            Lens ??= new VLiveCameraLensTrack();
            Roll ??= new VLiveCameraRollTrack();
            Activation ??= new VLiveCameraActivationTrack();

            Timing.Sanitize();
            Activation.Sanitize(Timing.ClipDuration);
        }
    }
}
