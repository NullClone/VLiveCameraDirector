using System;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Motion Presetの識別情報と演出意図を保持します。
    /// </summary>
    [Serializable]
    public class VLiveCameraMotionIdentity
    {
        // Fields

        [Tooltip("Inspectorに表示するプリセット名。")]
        [SerializeField]
        private string _displayName = "New Motion Preset";

        [Tooltip("固定またはスプライン移動のショット種別。")]
        [SerializeField]
        private VLiveCameraShotType _shotType = VLiveCameraShotType.Fixed;

        [Tooltip("カメラワークの動作系統分類。")]
        [SerializeField]
        private MotionFamily _motionFamily = MotionFamily.Fixed;

        [Tooltip("被写体に対するフレーミングの大きさ。")]
        [SerializeField]
        private ShotSize _shotSize = ShotSize.BustUp;

        [Tooltip("ショットの演出エネルギー。")]
        [SerializeField]
        private ShotEnergy _energy = ShotEnergy.Normal;

        [Tooltip("想定用途を示す短い説明。")]
        [SerializeField]
        private string _description = string.Empty;


        // Properties

        public string DisplayName
        {
            get => _displayName;
            set => _displayName = string.IsNullOrWhiteSpace(value) ? "Motion Preset" : value;
        }

        public VLiveCameraShotType ShotType
        {
            get => _shotType;
            set => _shotType = value;
        }

        public MotionFamily Family
        {
            get => _motionFamily;
            set => _motionFamily = value;
        }

        public ShotSize Size
        {
            get => _shotSize;
            set => _shotSize = value;
        }

        public ShotEnergy Energy
        {
            get => _energy;
            set => _energy = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value ?? string.Empty;
        }


        // Methods

        /// <summary>
        /// 独立して編集可能な複製を作成します。
        /// </summary>
        public VLiveCameraMotionIdentity Clone()
        {
            return new VLiveCameraMotionIdentity
            {
                DisplayName = DisplayName,
                ShotType = ShotType,
                Family = Family,
                Size = Size,
                Energy = Energy,
                Description = Description
            };
        }
    }

    /// <summary>
    /// カメラワークの動作系統分類。
    /// </summary>
    public enum MotionFamily
    {
        Fixed,
        Push,
        Pull,
        Truck,
        Arc,
        Pedestal,
        Crane,
        Gimbal,
        Fluid
    }

    /// <summary>
    /// 被写体に対するフレーミングの大きさ。
    /// </summary>
    public enum ShotSize
    {
        Wide,
        Full,
        BustUp,
        CloseUp,
        FaceUp
    }

    /// <summary>
    /// ショットの演出エネルギー。
    /// </summary>
    public enum ShotEnergy
    {
        Calm,
        Normal,
        Dynamic
    }
}
