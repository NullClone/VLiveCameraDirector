using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// Master Timelineと名前付きセクションを標準IMGUIで編集します。
    /// </summary>
    [CustomEditor(typeof(VLiveTimeTable))]
    public class VLiveTimeTableEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _masterTimelineProp;
        private SerializedProperty _sectionTimelinesProp;
        private SerializedProperty _autoFindOnAwakeProp;
        private SerializedProperty _includeInactiveProp;


        // Methods

        private void OnEnable()
        {
            _masterTimelineProp = serializedObject.FindProperty("_masterTimeline");
            _sectionTimelinesProp = serializedObject.FindProperty("_sectionTimelines");
            _autoFindOnAwakeProp = serializedObject.FindProperty("_autoFindOnAwake");
            _includeInactiveProp = serializedObject.FindProperty("_includeInactive");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_masterTimelineProp, new UnityEngine.GUIContent("Master Timeline"));
            EditorGUILayout.PropertyField(_sectionTimelinesProp, new UnityEngine.GUIContent("Section Timelines"), true);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Auto Collection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_autoFindOnAwakeProp, new UnityEngine.GUIContent("Auto Find On Awake"));
            EditorGUILayout.PropertyField(_includeInactiveProp, new UnityEngine.GUIContent("Include Inactive"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(4f);
            VLiveTimeTable table = (VLiveTimeTable)target;
            if (GUILayout.Button("Collect Child Directors"))
            {
                Undo.RecordObject(table, "Collect Child Directors");
                table.AutoCollectChildDirectors();
                EditorUtility.SetDirty(table);
            }

            if (GUILayout.Button("Rebuild Section Map"))
            {
                table.RebuildMap();
            }
        }
    }
}
