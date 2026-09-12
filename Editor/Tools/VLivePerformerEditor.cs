using UnityEditor;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// 演者参照を標準IMGUIで編集します。
    /// </summary>
    [CustomEditor(typeof(VLivePerformer))]
    [CanEditMultipleObjects]
    public class VLivePerformerEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _performerAnimatorProp;
        private SerializedProperty _performerNameProp;


        // Methods

        private void OnEnable()
        {
            _performerAnimatorProp = serializedObject.FindProperty("_performerAnimator");
            _performerNameProp = serializedObject.FindProperty("_performerName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_performerAnimatorProp, new UnityEngine.GUIContent("Performer Animator"));
            EditorGUILayout.PropertyField(_performerNameProp, new UnityEngine.GUIContent("Performer Name"));

            if (!_performerAnimatorProp.hasMultipleDifferentValues && _performerAnimatorProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Performer Animator is unassigned.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
