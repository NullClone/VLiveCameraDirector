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

        private SerializedProperty _performerAnimatorProp;
        private SerializedProperty _performerNameProp;
        private SerializedProperty _headRadiusProp;
        private SerializedProperty _bustRadiusProp;
        private SerializedProperty _bodyRadiusProp;


        // Methods

        private void OnEnable()
        {
            _performerAnimatorProp = serializedObject.FindProperty("_performerAnimator");
            _performerNameProp = serializedObject.FindProperty("_performerName");
            _headRadiusProp = serializedObject.FindProperty("_headRadius");
            _bustRadiusProp = serializedObject.FindProperty("_bustRadius");
            _bodyRadiusProp = serializedObject.FindProperty("_bodyRadius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_performerAnimatorProp, s_animatorContent);
            EditorGUILayout.PropertyField(_performerNameProp, s_nameContent);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Framing Bounds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_headRadiusProp, s_headRadiusContent);
            EditorGUILayout.PropertyField(_bustRadiusProp, s_bustRadiusContent);
            EditorGUILayout.PropertyField(_bodyRadiusProp, s_bodyRadiusContent);

            if (!_performerAnimatorProp.hasMultipleDifferentValues && _performerAnimatorProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Performer Animator is unassigned.", MessageType.Warning);
            }
            else if (!_performerAnimatorProp.hasMultipleDifferentValues)
            {
                var animator = (Animator)_performerAnimatorProp.objectReferenceValue;
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
