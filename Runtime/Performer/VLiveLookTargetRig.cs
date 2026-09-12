using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace VLiveKit.Camera
{
    /// <summary>
    /// 演者のHumanoidボーンへ追従するカメラ注視ターゲット群を管理します。
    /// </summary>
    [DefaultExecutionOrder(110)]
    [MovedFrom(false, sourceNamespace: null, sourceAssembly: null, sourceClassName: "BoneCollector")]
    public class VLiveLookTargetRig : MonoBehaviour
    {
        // Fields

        [Header("Performer Reference")]
        [Tooltip("演者情報の優先参照。未設定時は親階層とSceneから解決します。")]
        [FormerlySerializedAs("vLivePerformer")]
        [SerializeField]
        private VLivePerformer _vLivePerformer;

        [Header("Animator Fallback")]
        [Tooltip("Humanoidボーンを取得するAnimator。VLive Performer設定時は自動同期されます。")]
        [FormerlySerializedAs("targetAnimator")]
        [FormerlySerializedAs("performerAnimator")]
        [SerializeField]
        private Animator _performerAnimator;

        [Header("Humanoid Support")]
        [Tooltip("Animatorに有効なHumanoid Avatarがない場合だけ一時的に使用するAvatar。")]
        [FormerlySerializedAs("primaryHumanoidAvatar")]
        [FormerlySerializedAs("fallbackHumanoidAvatar")]
        [SerializeField]
        private Avatar _fallbackHumanoidAvatar;

        [Header("Look Target Root")]
        [Tooltip("生成した注視ターゲットを格納する親Transform。未設定時は自動生成します。")]
        [FormerlySerializedAs("parentTransform")]
        [FormerlySerializedAs("lookTargetRoot")]
        [SerializeField]
        private Transform _lookTargetRoot;

        [Header("Performer Naming")]
        [Tooltip("生成する注視ターゲット名に使用する演者名。")]
        [FormerlySerializedAs("characterName")]
        [FormerlySerializedAs("performerName")]
        [SerializeField]
        private string _performerName = "Performer";

        [Header("Live Update")]
        [Tooltip("演者のactive状態を毎フレーム追従制約へ反映するか。")]
        [FormerlySerializedAs("updateWeightsEveryFrame")]
        [FormerlySerializedAs("syncActiveStateEveryFrame")]
        [SerializeField]
        private bool _syncActiveStateEveryFrame = true;

        [Header("Debug View")]
        [Tooltip("解決済みボーンと生成した注視ターゲットの対応一覧。")]
        [FormerlySerializedAs("boneDataList")]
        [FormerlySerializedAs("lookTargetChannels")]
        [SerializeField]
        private List<VLiveLookTargetChannel> _lookTargetChannels = new();

        private readonly Dictionary<HumanBodyBones, Transform> _boneMap = new();
        private readonly Dictionary<HumanBodyBones, GameObject> _targetMap = new();
        private readonly Dictionary<HumanBodyBones, PositionConstraint> _constraintMap = new();


        // Properties

        public Transform LookTargetRoot => _lookTargetRoot;
        public IReadOnlyList<VLiveLookTargetChannel> LookTargetChannels => _lookTargetChannels;

        public bool IsPerformerLive { get; private set; }


        // Methods

        private void Start()
        {
            ResolvePerformer();
            BuildTargets();
            RefreshLiveState();
        }

        private void Update()
        {
            if (_syncActiveStateEveryFrame)
            {
                RefreshLiveState();
            }
        }

        /// <summary>
        /// 親階層またはSceneから演者参照を解決します。
        /// </summary>
        public void ResolvePerformer()
        {
            if (_vLivePerformer == null)
            {
                _vLivePerformer = GetComponentInParent<VLivePerformer>();

                if (_vLivePerformer == null)
                {
                    _vLivePerformer = FindFirstObjectByType<VLivePerformer>();
                }
            }

            if (_vLivePerformer != null)
            {
                _performerAnimator = _vLivePerformer.PerformerAnimator;

                if (string.IsNullOrWhiteSpace(_performerName))
                {
                    _performerName = _vLivePerformer.PerformerName;
                }
            }
        }

        /// <summary>
        /// 現在の演者ボーンから専用の注視ターゲット群を再生成します。
        /// </summary>
        [ContextMenu("Build Targets")]
        public void BuildTargets()
        {
            ClearTargets();

            _boneMap.Clear();
            _targetMap.Clear();
            _constraintMap.Clear();
            _lookTargetChannels.Clear();

            CollectBones(_performerAnimator, _fallbackHumanoidAvatar, _boneMap);

            if (_boneMap.Count == 0)
            {
                Debug.LogWarning("[VLiveLookTargetRig] Bone collect failed");
                return;
            }

            EnsureRoot();

            foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone)
                {
                    continue;
                }

                if (!_boneMap.TryGetValue(bone, out Transform performerBone))
                {
                    continue;
                }

                var go = new GameObject($"VLiveTG_{_performerName}_{bone}");
                go.transform.SetParent(_lookTargetRoot, false);
                go.transform.position = performerBone.position;
                go.transform.rotation = performerBone.rotation;

                var c = go.AddComponent<PositionConstraint>();
                c.translationAtRest = Vector3.zero;
                c.locked = true;
                c.AddSource(new ConstraintSource { sourceTransform = performerBone, weight = 1f });
                c.constraintActive = true;

                _targetMap[bone] = go;
                _constraintMap[bone] = c;

                _lookTargetChannels.Add(new VLiveLookTargetChannel
                {
                    TargetBone = bone,
                    PerformerBone = performerBone,
                    LookTargetObject = go
                });
            }
        }

        /// <summary>
        /// 演者のactive状態を生成済み追従制約へ反映します。
        /// </summary>
        public void RefreshLiveState()
        {
            bool active = _performerAnimator != null && _performerAnimator.gameObject.activeInHierarchy;
            IsPerformerLive = active;

            foreach (var c in _constraintMap.Values)
            {
                for (int i = 0; i < c.sourceCount; i++)
                {
                    var s = c.GetSource(i);
                    s.weight = active ? 1f : 0f;
                    c.SetSource(i, s);
                }
            }
        }

        private void CollectBones(
            Animator anim,
            Avatar fallback,
            Dictionary<HumanBodyBones, Transform> dict)
        {
            if (!anim)
            {
                return;
            }

            var original = anim.avatar;
            bool swapped = false;

            if ((original == null || !original.isHuman) && fallback && fallback.isHuman)
            {
                anim.avatar = fallback;
                swapped = true;
            }

            foreach (HumanBodyBones b in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (b == HumanBodyBones.LastBone)
                {
                    continue;
                }

                Transform boneTransform = anim.GetBoneTransform(b);
                if (boneTransform)
                {
                    dict[b] = boneTransform;
                }
            }

            if (swapped)
            {
                anim.avatar = original;
            }
        }

        private void EnsureRoot()
        {
            if (_lookTargetRoot != null)
            {
                return;
            }

            var go = new GameObject($"VLiveTargets_{_performerName}");
            _lookTargetRoot = go.transform;

            if (_performerAnimator)
            {
                _lookTargetRoot.SetParent(_performerAnimator.transform, false);
            }
        }

        private void ClearTargets()
        {
            if (!_lookTargetRoot)
            {
                return;
            }

            for (int i = _lookTargetRoot.childCount - 1; i >= 0; i--)
            {
                var c = _lookTargetRoot.GetChild(i).gameObject;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(c);
                }
                else
#endif
                {
                    Destroy(c);
                }
            }
        }

        /// <summary>
        /// 指定Humanoidボーン用の生成済み注視ターゲットを取得します。
        /// </summary>
        public GameObject GetBoneTarget(HumanBodyBones bone)
        {
            if (_targetMap.TryGetValue(bone, out GameObject target))
            {
                return target;
            }

            return null;
        }
    }
}
