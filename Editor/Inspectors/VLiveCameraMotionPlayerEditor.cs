using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    [CustomEditor(typeof(VLiveCameraMotionPlayer))]
    public class VLiveCameraMotionPlayerEditor : UnityEditor.Editor
    {
        // Methods

        public override void OnInspectorGUI()
        {
            VLiveCameraMotionPlayer player = (VLiveCameraMotionPlayer)target;
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Live Playback", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("State", GetState(player));
                EditorGUILayout.LabelField("Time", $"{player.CurrentTime:F2} s");
                EditorGUILayout.LabelField("Spline Distance", $"{player.CurrentSplineDistance:F2} m");
                EditorGUILayout.LabelField("Speed", $"{player.CurrentSpeedMultiplier:F2}x");
                EditorGUILayout.LabelField("Direction", player.CurrentDirection > 0 ? "Forward" : "Reverse");
                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("Playback status is monitored during Play Mode.", MessageType.Info);
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
