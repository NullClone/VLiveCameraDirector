using System.IO;
using UnityEditor;
using UnityEngine;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// 初期Motion Palette用Preset AssetをUnity Editor経由で生成・管理するエディタユーティリティ。
    /// </summary>
    public static class VLiveCameraPresetAssetCreator
    {
        // Fields

        public const string PresetFolderPath = "Assets/toshi.VLiveKit/VLiveCameraUnit/Presets";


        // Methods

        /// <summary>
        /// 初期の6つのMotion Preset AssetをUnity Editor API経由で生成します。
        /// </summary>
        [MenuItem("Tools/VLive Camera/Create Default Presets", priority = 20)]
        public static void CreateOrUpdateDefaultPresets()
        {
            if (!Directory.Exists(PresetFolderPath))
            {
                Directory.CreateDirectory(PresetFolderPath);
                AssetDatabase.Refresh();
            }

            // 1. Fixed Medium
            CreatePresetAsset(
                "FixedMedium.asset",
                "Fixed Medium",
                VLiveCameraShot.ShotType.Fixed,
                40f,
                new Vector3(0f, 1.2f, 0f),
                new Vector3[]
                {
                    new Vector3(0f, 1.3f, -3.5f)
                },
                0.2f,
                0.25f
            );

            // 2. Push In
            CreatePresetAsset(
                "PushIn.asset",
                "Push In",
                VLiveCameraShot.ShotType.Spline,
                45f,
                new Vector3(0f, 1.2f, 0f),
                new Vector3[]
                {
                    new Vector3(0f, 1.3f, -4.5f),
                    new Vector3(0f, 1.3f, -1.8f)
                },
                0.25f,
                0.3f
            );

            // 3. Pull Out
            CreatePresetAsset(
                "PullOut.asset",
                "Pull Out",
                VLiveCameraShot.ShotType.Spline,
                45f,
                new Vector3(0f, 1.2f, 0f),
                new Vector3[]
                {
                    new Vector3(0f, 1.3f, -1.8f),
                    new Vector3(0f, 1.3f, -4.5f)
                },
                0.25f,
                0.3f
            );

            // 4. Truck Left (右から左へ移動)
            CreatePresetAsset(
                "TruckLeft.asset",
                "Truck Left",
                VLiveCameraShot.ShotType.Spline,
                45f,
                new Vector3(0f, 1.2f, 0f),
                new Vector3[]
                {
                    new Vector3(2.5f, 1.3f, -3.0f),
                    new Vector3(0f, 1.3f, -3.0f),
                    new Vector3(-2.5f, 1.3f, -3.0f)
                },
                0.2f,
                0.25f
            );

            // 5. Truck Right (左から右へ移動)
            CreatePresetAsset(
                "TruckRight.asset",
                "Truck Right",
                VLiveCameraShot.ShotType.Spline,
                45f,
                new Vector3(0f, 1.2f, 0f),
                new Vector3[]
                {
                    new Vector3(-2.5f, 1.3f, -3.0f),
                    new Vector3(0f, 1.3f, -3.0f),
                    new Vector3(2.5f, 1.3f, -3.0f)
                },
                0.2f,
                0.25f
            );

            // 6. Arc Around (Target周囲を弧状に回り込む)
            CreatePresetAsset(
                "ArcAround.asset",
                "Arc Around",
                VLiveCameraShot.ShotType.Spline,
                45f,
                new Vector3(0f, 1.2f, 0f),
                new Vector3[]
                {
                    new Vector3(2.5f, 1.3f, -2.5f),
                    new Vector3(1.5f, 1.3f, -3.2f),
                    new Vector3(-1.5f, 1.3f, -3.2f),
                    new Vector3(-2.5f, 1.3f, -2.5f)
                },
                0.2f,
                0.25f
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VLiveCameraPresetAssetCreator] Default 6 motion presets verified/created successfully.");
        }

        private static void CreatePresetAsset(
            string fileName,
            string displayName,
            VLiveCameraShot.ShotType shotType,
            float fov,
            Vector3 targetOffset,
            Vector3[] controlPoints,
            float initialSpeed,
            float decelerationDistance)
        {
            string assetPath = $"{PresetFolderPath}/{fileName}";
            VLiveCameraMotionPreset preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(assetPath);

            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<VLiveCameraMotionPreset>();
                preset.Initialize(displayName, shotType, fov, targetOffset, controlPoints, initialSpeed, decelerationDistance);
                AssetDatabase.CreateAsset(preset, assetPath);
                EditorUtility.SetDirty(preset);
            }
        }
    }
}
