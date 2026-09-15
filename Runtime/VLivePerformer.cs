using UnityEngine;

namespace VLiveKit.Camera
{
    /// <summary>
    /// Shot構図に使用する演者のHumanoid Animatorと被写体範囲を保持します。
    /// </summary>
    [DisallowMultipleComponent]
    public class VLivePerformer : MonoBehaviour
    {
        // Fields

        [Tooltip("Head、UpperChest、Chest、Hipsを取得するHumanoid Animator。")]
        [SerializeField]
        private Animator _performerAnimator;

        [Tooltip("Editorと生成オブジェクト名に使用する演者名。空の場合はGameObject名を使用します。")]
        [SerializeField]
        private string _performerName = "Performer";

        [Tooltip("FaceUpとCloseUpでHeadボーンの周囲に含める半径。メートル単位です。")]
        [Min(0.01f)]
        [SerializeField]
        private float _headRadius = 0.18f;

        [Tooltip("BustUpでUpperChestまたはChestボーンの周囲に含める半径。メートル単位です。")]
        [Min(0.01f)]
        [SerializeField]
        private float _bustRadius = 0.35f;

        [Tooltip("FullとWideでHipsボーンの周囲に含める半径。メートル単位です。")]
        [Min(0.01f)]
        [SerializeField]
        private float _bodyRadius = 0.65f;


        // Properties

        public Animator PerformerAnimator => _performerAnimator;

        public float HeadRadius => Mathf.Max(0.01f, _headRadius);

        public float BustRadius => Mathf.Max(0.01f, _bustRadius);

        public float BodyRadius => Mathf.Max(0.01f, _bodyRadius);

        public string PerformerName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_performerName))
                {
                    return _performerName;
                }

                return gameObject.name;
            }
        }


        // Methods

        /// <summary>
        /// Headボーンを取得します。Humanoid Avatarまたはボーンが無効な場合はfalseを返します。
        /// </summary>
        public bool TryGetHead(out Transform head)
        {
            return TryGetBone(HumanBodyBones.Head, out head);
        }

        /// <summary>
        /// UpperChestを優先し、存在しないAvatarではChestを取得します。
        /// </summary>
        public bool TryGetBust(out Transform bust)
        {
            if (TryGetBone(HumanBodyBones.UpperChest, out bust))
            {
                return true;
            }

            return TryGetBone(HumanBodyBones.Chest, out bust);
        }

        /// <summary>
        /// Hipsボーンを取得します。Humanoid Avatarまたはボーンが無効な場合はfalseを返します。
        /// </summary>
        public bool TryGetHips(out Transform hips)
        {
            return TryGetBone(HumanBodyBones.Hips, out hips);
        }

        /// <summary>
        /// Animatorを子階層から再取得します。
        /// </summary>
        public void ResolveAnimator()
        {
            _performerAnimator = GetComponentInChildren<Animator>();
        }

        private void Reset()
        {
            ResolveAnimator();

            if (string.IsNullOrWhiteSpace(_performerName))
            {
                _performerName = gameObject.name;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _headRadius = Mathf.Max(0.01f, _headRadius);
            _bustRadius = Mathf.Max(0.01f, _bustRadius);
            _bodyRadius = Mathf.Max(0.01f, _bodyRadius);
        }
#endif

        private bool TryGetBone(HumanBodyBones bone, out Transform result)
        {
            result = null;

            if (_performerAnimator == null || _performerAnimator.avatar == null || !_performerAnimator.isHuman)
            {
                return false;
            }

            result = _performerAnimator.GetBoneTransform(bone);
            return result != null;
        }
    }
}
