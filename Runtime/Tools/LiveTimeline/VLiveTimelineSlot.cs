using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VLiveKit.Camera
{
    /// <summary>
    /// ライブ進行上のセクション名とPlayableDirectorを対応付けます。
    /// </summary>
    [Serializable]
    public class VLiveTimelineSlot
    {
        // Fields

        [Tooltip("Camera、Light、FXなどの一意なセクション名。")]
        [SerializeField]
        private string _sectionName;

        [Tooltip("このセクションで再生するPlayableDirector。")]
        [SerializeField]
        private PlayableDirector _director;

        [Tooltip("運用上の短いメモ。")]
        [TextArea]
        [SerializeField]
        private string _note;


        // Properties

        public string SectionName
        {
            get => _sectionName;
            set => _sectionName = value;
        }

        public PlayableDirector Director
        {
            get => _director;
            set => _director = value;
        }

        public string Note
        {
            get => _note;
            set => _note = value;
        }
    }
}
