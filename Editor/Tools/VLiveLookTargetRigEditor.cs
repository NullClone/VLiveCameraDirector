using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// 演者参照と生成済み注視ターゲットを標準IMGUIで管理します。
    /// </summary>
    [CustomEditor(typeof(VLiveLookTargetRig))]
    public class VLiveLookTargetRigEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _vLivePerformerProp;
        private SerializedProperty _performerAnimatorProp;
        private SerializedProperty _fallbackHumanoidAvatarProp;
        private SerializedProperty _lookTargetRootProp;
        private SerializedProperty _performerNameProp;
        private SerializedProperty _syncActiveStateEveryFrameProp;
        private SerializedProperty _lookTargetChannelsProp;

        private bool _showChannelList = true;

        private static readonly HumanBodyBones[] QuickAccessBones =
        {
            HumanBodyBones.Head,
            HumanBodyBones.Neck,
            HumanBodyBones.Chest,
            HumanBodyBones.Spine,
            HumanBodyBones.Hips,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightHand
        };


        // Methods

        private void OnEnable()
        {
            _vLivePerformerProp = serializedObject.FindProperty("_vLivePerformer");
            _performerAnimatorProp = serializedObject.FindProperty("_performerAnimator");
            _fallbackHumanoidAvatarProp = serializedObject.FindProperty("_fallbackHumanoidAvatar");
            _lookTargetRootProp = serializedObject.FindProperty("_lookTargetRoot");
            _performerNameProp = serializedObject.FindProperty("_performerName");
            _syncActiveStateEveryFrameProp = serializedObject.FindProperty("_syncActiveStateEveryFrame");
            _lookTargetChannelsProp = serializedObject.FindProperty("_lookTargetChannels");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var rig = (VLiveLookTargetRig)target;

            DrawPerformerSection();
            EditorGUILayout.Space(8);

            DrawOperationSection(rig);
            EditorGUILayout.Space(8);

            DrawQuickAccessSection(rig);
            EditorGUILayout.Space(8);

            DrawDebugSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPerformerSection()
        {
            EditorGUILayout.LabelField("Performer", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_vLivePerformerProp, new GUIContent("VLive Performer"));

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Fallback / Override", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_performerAnimatorProp, new GUIContent("Performer Animator"));
            EditorGUILayout.PropertyField(_fallbackHumanoidAvatarProp, new GUIContent("Fallback Humanoid Avatar"));
            EditorGUILayout.PropertyField(_lookTargetRootProp, new GUIContent("Look Target Root"));
            EditorGUILayout.PropertyField(_performerNameProp, new GUIContent("Performer Name"));
            EditorGUILayout.PropertyField(_syncActiveStateEveryFrameProp, new GUIContent("Sync Active State Every Frame"));
        }

        private void DrawOperationSection(VLiveLookTargetRig rig)
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Resolve Performer", GUILayout.Height(28)))
                {
                    Undo.RecordObject(rig, "Resolve Performer");
                    rig.ResolvePerformer();
                    EditorUtility.SetDirty(rig);
                }

                if (GUILayout.Button("Build Targets", GUILayout.Height(28)))
                {
                    Undo.RecordObject(rig, "Build Targets");
                    rig.BuildTargets();
                    EditorUtility.SetDirty(rig);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh State", GUILayout.Height(24)))
                {
                    rig.RefreshLiveState();
                    EditorUtility.SetDirty(rig);
                }

                GUI.enabled = rig.LookTargetRoot != null;
                if (GUILayout.Button("Ping Root", GUILayout.Height(24)))
                {
                    EditorGUIUtility.PingObject(rig.LookTargetRoot);
                    Selection.activeObject = rig.LookTargetRoot.gameObject;
                }

                GUI.enabled = true;
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Performer Live", rig.IsPerformerLive ? "Yes" : "No");
            EditorGUILayout.LabelField("Targets Ready", rig.LookTargetChannels != null && rig.LookTargetChannels.Count > 0 ? "Yes" : "No");
        }

        private void DrawQuickAccessSection(VLiveLookTargetRig rig)
        {
            EditorGUILayout.LabelField("Quick Bone Access", EditorStyles.boldLabel);

            foreach (var bone in QuickAccessBones)
            {
                var target = rig.GetBoneTarget(bone);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(bone.ToString(), GUILayout.Width(100));

                    GUI.enabled = target != null;
                    EditorGUILayout.ObjectField(target, typeof(GameObject), true);

                    if (GUILayout.Button("Select", GUILayout.Width(60)) && target != null)
                    {
                        Selection.activeObject = target;
                        EditorGUIUtility.PingObject(target);
                    }

                    GUI.enabled = true;
                }
            }

        }

        private void DrawDebugSection()
        {
            _showChannelList = EditorGUILayout.Foldout(_showChannelList, "Target Channels", true);

            if (_showChannelList)
            {
                EditorGUI.indentLevel++;

                for (int i = 0; i < _lookTargetChannelsProp.arraySize; i++)
                {
                    var element = _lookTargetChannelsProp.GetArrayElementAtIndex(i);
                    var targetBoneProp = element.FindPropertyRelative("TargetBone");
                    var performerBoneProp = element.FindPropertyRelative("PerformerBone");
                    var lookTargetObjectProp = element.FindPropertyRelative("LookTargetObject");

                    EditorGUILayout.PropertyField(targetBoneProp, new GUIContent("Target Bone"));
                    EditorGUILayout.PropertyField(performerBoneProp, new GUIContent("Performer Bone"));
                    EditorGUILayout.PropertyField(lookTargetObjectProp, new GUIContent("Look Target"));
                    EditorGUILayout.Space(2f);
                }

                EditorGUI.indentLevel--;
            }

        }
    }
}
