using UnityEditor;
using UnityEngine;

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

        private static readonly GUIContent s_animatorContent = new GUIContent("Performer Animator");
        private static readonly GUIContent s_nameContent = new GUIContent("Performer Name");
        private static readonly GUIContent s_headRadiusContent = new GUIContent("Head Radius");
        private static readonly GUIContent s_bustRadiusContent = new GUIContent("Bust Radius");
        private static readonly GUIContent s_bodyRadiusContent = new GUIContent("Body Radius");

        private SerializedProperty _performerAnimator;
        private SerializedProperty _performerName;
        private SerializedProperty _headRadius;
        private SerializedProperty _bustRadius;
        private SerializedProperty _bodyRadius;


        // Methods

        private void OnEnable()
        {
            _performerAnimator = serializedObject.FindProperty(nameof(_performerAnimator));
            _performerName = serializedObject.FindProperty(nameof(_performerName));
            _headRadius = serializedObject.FindProperty(nameof(_headRadius));
            _bustRadius = serializedObject.FindProperty(nameof(_bustRadius));
            _bodyRadius = serializedObject.FindProperty(nameof(_bodyRadius));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_performerAnimator, s_animatorContent);
            EditorGUILayout.PropertyField(_performerName, s_nameContent);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Framing Bounds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_headRadius, s_headRadiusContent);
            EditorGUILayout.PropertyField(_bustRadius, s_bustRadiusContent);
            EditorGUILayout.PropertyField(_bodyRadius, s_bodyRadiusContent);

            if (!_performerAnimator.hasMultipleDifferentValues && _performerAnimator.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Performer Animator is unassigned.", MessageType.Warning);
            }
            else if (!_performerAnimator.hasMultipleDifferentValues)
            {
                var animator = (Animator)_performerAnimator.objectReferenceValue;
                if (animator != null && !animator.isHuman)
                {
                    EditorGUILayout.HelpBox("Performer Animator must use a valid Humanoid Avatar.", MessageType.Error);
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Resolve Animator From Children"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    var performer = (VLivePerformer)targets[i];
                    Undo.RecordObject(performer, "Resolve Performer Animator");
                    performer.ResolveAnimator();
                    EditorUtility.SetDirty(performer);
                }

                serializedObject.Update();
            }
        }
    }
}
