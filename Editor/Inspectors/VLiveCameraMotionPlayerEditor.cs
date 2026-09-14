using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    [CustomEditor(typeof(VLiveCameraMotionPlayer))]
    public class VLiveCameraMotionPlayerEditor : UnityEditor.Editor
    {
        // Fields

        private SerializedProperty _shotProp;
        private SerializedProperty _cinemachineCameraProp;
        private SerializedProperty _splineDollyProp;
        private SerializedProperty _rotationComposerProp;
        private SerializedProperty _groupFramingProp;


        // Methods

        private void OnEnable()
        {
            _shotProp = serializedObject.FindProperty("_shot");
            _cinemachineCameraProp = serializedObject.FindProperty("_cinemachineCamera");
            _splineDollyProp = serializedObject.FindProperty("_splineDolly");
            _rotationComposerProp = serializedObject.FindProperty("_rotationComposer");
            _groupFramingProp = serializedObject.FindProperty("_groupFraming");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Owned References", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_shotProp);
                EditorGUILayout.PropertyField(_cinemachineCameraProp);
                EditorGUILayout.PropertyField(_splineDollyProp);
                EditorGUILayout.PropertyField(_rotationComposerProp);
                EditorGUILayout.PropertyField(_groupFramingProp);
            }

            VLiveCameraMotionPlayer player = (VLiveCameraMotionPlayer)target;
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("State", GetState(player));
                EditorGUILayout.LabelField("Time", $"{player.CurrentTime:F2} s");
                EditorGUILayout.LabelField("Spline Distance", $"{player.CurrentSplineDistance:F2} m");
                EditorGUILayout.LabelField("Speed", $"{player.CurrentSpeedMultiplier:F2}x");
                EditorGUILayout.LabelField("Direction", player.CurrentDirection > 0 ? "Forward" : "Reverse");
                Repaint();
            }
        }

        private static string GetState(VLiveCameraMotionPlayer player)
        {
            if (!player.IsLive)
            {
                return player.IsPrepared ? "Prepared" : "Off Air";
            }

            if (player.IsHolding)
            {
                return "Hold";
            }

            return player.IsPlaying ? "Playing" : "Stopped";
        }
    }
}
