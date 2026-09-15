using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace VLiveKit.Camera
{
    /// <summary>
    /// ライブ用の MasterTimeline と、そこに紐づく各セクション Timeline を管理するタイムテーブル。
    /// どのコンポーネントからでも Get で取得できる。
    /// </summary>
    [DisallowMultipleComponent]
    public class VLiveTimeTable : MonoBehaviour
    {
        // Fields

        [Tooltip("ライブ進行の基準となるMaster PlayableDirector。")]
        [FormerlySerializedAs("masterTimeline")]
        [SerializeField]
        private PlayableDirector _masterTimeline;

        [Tooltip("名前付きセクションとPlayableDirectorの対応一覧。")]
        [FormerlySerializedAs("sectionTimelines")]
        [SerializeField]
        private List<VLiveTimelineSlot> _sectionTimelines = new();

        [Tooltip("Awake時にMaster Timeline配下のPlayableDirectorを自動収集するか。")]
        [FormerlySerializedAs("autoFindOnAwake")]
        [SerializeField]
        private bool _autoFindOnAwake = true;

        [Tooltip("自動収集時に非アクティブなGameObjectも含めるか。")]
        [FormerlySerializedAs("includeInactive")]
        [SerializeField]
        private bool _includeInactive = true;

        private static VLiveTimeTable _cachedInstance;

        private readonly Dictionary<string, PlayableDirector> _sectionMap =
            new(StringComparer.OrdinalIgnoreCase);


        // Properties

        public PlayableDirector MasterTimeline => _masterTimeline;
        public IReadOnlyList<VLiveTimelineSlot> SectionTimelines => _sectionTimelines;


        // Methods

        private void Awake()
        {
            _cachedInstance = this;

            if (_autoFindOnAwake)
            {
                AutoCollectChildDirectors();
            }

            RebuildMap();
        }

        private void OnEnable()
        {
            if (_cachedInstance == null)
            {
                _cachedInstance = this;
            }

            RebuildMap();
        }

        private void OnValidate()
        {
            RebuildMap();
        }

        /// <summary>
        /// シーン上の TimeTable を取得する。
        /// caller が属する階層親から優先的に探し、無ければシーン全体から探す。
        /// </summary>
        public static VLiveTimeTable Get(Component caller = null)
        {
            if (caller != null)
            {
                VLiveTimeTable parentTimeTable = caller.GetComponentInParent<VLiveTimeTable>(true);
                if (parentTimeTable != null)
                {
                    _cachedInstance = parentTimeTable;
                    return _cachedInstance;
                }
            }

            if (_cachedInstance != null)
            {
                return _cachedInstance;
            }

#if UNITY_2023_1_OR_NEWER
            _cachedInstance = FindFirstObjectByType<VLiveTimeTable>(FindObjectsInactive.Include);
#else
            _cachedInstance = FindObjectOfType<VLiveTimeTable>(true);
#endif
            return _cachedInstance;
        }

        /// <summary>
        /// MasterTimeline を取得。
        /// </summary>
        public PlayableDirector GetMasterTimeline()
        {
            return _masterTimeline;
        }

        /// <summary>
        /// 名前付きセクションを取得し、見つからない場合はMaster Timelineを返します。
        /// </summary>
        public PlayableDirector GetTimelineOrMaster(string sectionName)
        {
            if (!string.IsNullOrWhiteSpace(sectionName) &&
                TryGetSectionTimeline(sectionName, out PlayableDirector director))
            {
                return director;
            }

            return _masterTimeline;
        }

        /// <summary>
        /// セクション名から PlayableDirector を取得。
        /// </summary>
        public PlayableDirector GetSectionTimeline(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                return null;
            }

            if (_sectionMap.Count == 0)
            {
                RebuildMap();
            }

            _sectionMap.TryGetValue(sectionName, out var director);
            return director;
        }

        /// <summary>
        /// セクション名から PlayableDirector を TryGet する。
        /// </summary>
        public bool TryGetSectionTimeline(string sectionName, out PlayableDirector director)
        {
            director = null;

            if (string.IsNullOrWhiteSpace(sectionName))
            {
                return false;
            }

            if (_sectionMap.Count == 0)
            {
                RebuildMap();
            }

            return _sectionMap.TryGetValue(sectionName, out director) && director != null;
        }

        /// <summary>
        /// 指定 Director がどのセクションに属するかを取得。
        /// </summary>
        public bool TryGetSectionName(PlayableDirector targetDirector, out string sectionName)
        {
            sectionName = null;

            if (targetDirector == null)
            {
                return false;
            }

            for (int i = 0; i < _sectionTimelines.Count; i++)
            {
                var slot = _sectionTimelines[i];
                if (slot != null && slot.Director == targetDirector)
                {
                    sectionName = slot.SectionName;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// セクションを追加または上書き登録。
        /// </summary>
        public void SetSectionTimeline(string sectionName, PlayableDirector director, string note = "")
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                Debug.LogWarning("[VLiveTimeTable] sectionName is null or empty.", this);
                return;
            }

            for (int i = 0; i < _sectionTimelines.Count; i++)
            {
                var slot = _sectionTimelines[i];
                if (slot == null)
                {
                    continue;
                }

                if (string.Equals(slot.SectionName, sectionName, StringComparison.OrdinalIgnoreCase))
                {
                    slot.Director = director;
                    slot.Note = note;
                    RebuildMap();
                    return;
                }
            }

            _sectionTimelines.Add(new VLiveTimelineSlot
            {
                SectionName = sectionName,
                Director = director,
                Note = note
            });

            RebuildMap();
        }

        /// <summary>
        /// セクションを削除。
        /// </summary>
        public bool RemoveSectionTimeline(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                return false;
            }

            for (int i = _sectionTimelines.Count - 1; i >= 0; i--)
            {
                var slot = _sectionTimelines[i];
                if (slot == null)
                {
                    continue;
                }

                if (string.Equals(slot.SectionName, sectionName, StringComparison.OrdinalIgnoreCase))
                {
                    _sectionTimelines.RemoveAt(i);
                    RebuildMap();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 子階層の PlayableDirector を自動収集して section に追加する。
        /// 既存の sectionName と重複する場合は上書きしない。
        /// sectionName は GameObject 名を使う。
        /// </summary>
        [ContextMenu("Auto Collect Child Directors")]
        public void AutoCollectChildDirectors()
        {
            if (_masterTimeline == null)
            {
                Debug.LogWarning("[VLiveTimeTable] MasterTimeline is not assigned.", this);
                return;
            }

            var root = _masterTimeline.transform;
            var foundDirectors = root.GetComponentsInChildren<PlayableDirector>(_includeInactive);

            for (int i = 0; i < foundDirectors.Length; i++)
            {
                var director = foundDirectors[i];
                if (director == null || director == _masterTimeline)
                {
                    continue;
                }

                var sectionName = director.gameObject.name;

                bool alreadyExists = false;
                for (int j = 0; j < _sectionTimelines.Count; j++)
                {
                    var slot = _sectionTimelines[j];
                    if (slot == null)
                    {
                        continue;
                    }

                    if (slot.Director == director ||
                        string.Equals(slot.SectionName, sectionName, StringComparison.OrdinalIgnoreCase))
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    _sectionTimelines.Add(new VLiveTimelineSlot
                    {
                        SectionName = sectionName,
                        Director = director,
                        Note = "Auto Collected"
                    });
                }
            }

            RebuildMap();
        }

        /// <summary>
        /// 現在のセクション一覧から名前検索用Mapを再構築します。
        /// </summary>
        [ContextMenu("Rebuild Section Map")]
        public void RebuildMap()
        {
            _sectionMap.Clear();

            for (int i = 0; i < _sectionTimelines.Count; i++)
            {
                var slot = _sectionTimelines[i];
                if (slot == null || string.IsNullOrWhiteSpace(slot.SectionName) || slot.Director == null)
                {
                    continue;
                }

                _sectionMap[slot.SectionName] = slot.Director;
            }
        }
    }
}
