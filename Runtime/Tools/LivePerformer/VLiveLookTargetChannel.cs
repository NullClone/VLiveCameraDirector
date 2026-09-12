using System;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Humanoidボーンと生成済み注視ターゲットを対応付けます。
    /// </summary>
    [Serializable]
    public struct VLiveLookTargetChannel
    {
        [Tooltip("追跡するHumanoidボーン。")]
        public HumanBodyBones TargetBone;

        [Tooltip("演者側で解決されたボーンTransform。")]
        public Transform PerformerBone;

        [Tooltip("カメラが参照する生成済み注視ターゲット。")]
        public GameObject LookTargetObject;
    }
}
