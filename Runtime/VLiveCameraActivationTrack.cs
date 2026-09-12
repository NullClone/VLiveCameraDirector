using System;
using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Program選択時の開始状態と有効再生区間を保持します。
    /// </summary>
    [Serializable]
    public class VLiveCameraActivationTrack
    {
        // Fields

        [Tooltip("Program選択時に静止開始または動作中開始のどちらを使用するか。")]
        [SerializeField]
        private EntryMode _entryMode = EntryMode.Rolling;

        [Tooltip("実番組で使用する開始時刻。単位は秒です。")]
        [Min(0f)]
        [SerializeField]
        private float _inTime;

        [Tooltip("実番組で使用する終了時刻。単位は秒です。")]
        [Min(0f)]
        [SerializeField]
        private float _outTime = 4f;

        [Tooltip("終了時刻で停止するか、クリップ終端まで余白動作を続けるか。")]
        [SerializeField]
        private ExitBehavior _exitBehavior = ExitBehavior.Hold;


        // Properties

        public EntryMode EntryMode { get => _entryMode; set => _entryMode = value; }
        public float InTime { get => _inTime; set => _inTime = Mathf.Max(0f, value); }
        public float OutTime { get => _outTime; set => _outTime = Mathf.Max(0f, value); }
        public ExitBehavior ExitBehavior { get => _exitBehavior; set => _exitBehavior = value; }


        // Methods

        /// <summary>
        /// 独立して編集可能な複製を作成します。
        /// </summary>
        public VLiveCameraActivationTrack Clone()
        {
            return new VLiveCameraActivationTrack
            {
                EntryMode = EntryMode,
                InTime = InTime,
                OutTime = OutTime,
                ExitBehavior = ExitBehavior
            };
        }

        /// <summary>
        /// 指定時間内へ有効区間を補正します。
        /// </summary>
        public void Sanitize(float duration)
        {
            float safeDuration = Mathf.Max(0.1f, duration);
            _inTime = Mathf.Clamp(_inTime, 0f, safeDuration);
            _outTime = Mathf.Clamp(_outTime, _inTime, safeDuration);
        }
    }
}
