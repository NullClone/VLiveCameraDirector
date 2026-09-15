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

        private SerializedProperty _masterTimeline;
        private SerializedProperty _sectionTimelines;
        private SerializedProperty _autoFindOnAwake;
        private SerializedProperty _includeInactive;


        // Methods

        private void OnEnable()
        {
            _masterTimeline = serializedObject.FindProperty(nameof(_masterTimeline));
            _sectionTimelines = serializedObject.FindProperty(nameof(_sectionTimelines));
            _autoFindOnAwake = serializedObject.FindProperty(nameof(_autoFindOnAwake));
            _includeInactive = serializedObject.FindProperty(nameof(_includeInactive));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_masterTimeline, new GUIContent("Master Timeline"));
            EditorGUILayout.PropertyField(_sectionTimelines, new GUIContent("Section Timelines"), true);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Auto Collection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_autoFindOnAwake, new GUIContent("Auto Find On Awake"));
            EditorGUILayout.PropertyField(_includeInactive, new GUIContent("Include Inactive"));

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
