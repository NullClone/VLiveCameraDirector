using UnityEngine;
using UnityEngine.Serialization;

namespace VLiveKit.Camera
{
    /// <summary>
    /// カメラ補助機能が参照する演者のAnimatorと表示名を保持します。
    /// </summary>
    public class VLivePerformer : MonoBehaviour
    {
        // Fields

        [Header("Performer Core")]
        [Tooltip("演者のHumanoid Animator。")]
        [FormerlySerializedAs("performerAnimator")]
        [SerializeField]
        private Animator _performerAnimator;

        [Header("Display")]
        [Tooltip("Editorと生成オブジェクト名に使用する演者名。空の場合はGameObject名を使用します。")]
        [FormerlySerializedAs("performerName")]
        [SerializeField]
        private string _performerName = "Performer";


        // Properties

        public Animator PerformerAnimator => _performerAnimator;

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

        private void Reset()
        {
            if (_performerAnimator == null)
            {
                _performerAnimator = GetComponentInChildren<Animator>();
            }

            if (string.IsNullOrWhiteSpace(_performerName))
            {
                _performerName = gameObject.name;
            }
        }
    }
}
