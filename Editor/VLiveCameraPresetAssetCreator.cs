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
        /// 初期の6つのMotion Preset Assetを+Z正面基準で更新または生成します。
        /// </summary>
        [MenuItem("Tools/VLive Camera/Create Default Presets", priority = 20)]
        public static void CreateOrUpdateDefaultPresets()
        {
            if (!Directory.Exists(PresetFolderPath))
            {
                Directory.CreateDirectory(PresetFolderPath);
                AssetDatabase.Refresh();
            }

            // 1. Fixed Medium (+Z 正面ミドル)
            CreateOrUpdatePresetAsset(
                "FixedMedium.asset",
                "Fixed Medium",
                VLiveCameraShot.ShotType.Fixed,
                40f,
                Vector3.zero,
                new Vector3[]
                {
                    new Vector3(0f, 1.3f, 3.5f)
                },
                0.2f,
                0.25f
            );

            // 2. Push In (+Z 正面からTargetへ接近)
            CreateOrUpdatePresetAsset(
                "PushIn.asset",
                "Push In",
                VLiveCameraShot.ShotType.Spline,
                45f,
                Vector3.zero,
                new Vector3[]
                {
                    new Vector3(0f, 1.3f, 4.5f),
                    new Vector3(0f, 1.3f, 1.8f)
                },
                0.25f,
                0.3f
            );

            // 3. Pull Out (Targetから後方+Zへ後退)
            CreateOrUpdatePresetAsset(
                "PullOut.asset",
                "Pull Out",
                VLiveCameraShot.ShotType.Spline,
                45f,
                Vector3.zero,
                new Vector3[]
                {
                    new Vector3(0f, 1.3f, 1.8f),
                    new Vector3(0f, 1.3f, 4.5f)
                },
                0.25f,
                0.3f
            );

            // 4. Truck Left (Targetを捉えたまま右から左へ移動: +X -> -X)
            CreateOrUpdatePresetAsset(
                "TruckLeft.asset",
                "Truck Left",
                VLiveCameraShot.ShotType.Spline,
                45f,
                Vector3.zero,
                new Vector3[]
                {
                    new Vector3(2.5f, 1.3f, 3.0f),
                    new Vector3(0f, 1.3f, 3.0f),
                    new Vector3(-2.5f, 1.3f, 3.0f)
                },
                0.2f,
                0.25f
            );

            // 5. Truck Right (Targetを捉えたまま左から右へ移動: -X -> +X)
            CreateOrUpdatePresetAsset(
                "TruckRight.asset",
                "Truck Right",
                VLiveCameraShot.ShotType.Spline,
                45f,
                Vector3.zero,
                new Vector3[]
                {
                    new Vector3(-2.5f, 1.3f, 3.0f),
                    new Vector3(0f, 1.3f, 3.0f),
                    new Vector3(2.5f, 1.3f, 3.0f)
                },
                0.2f,
                0.25f
            );

            // 6. Arc Around (Target周囲を弧状に回り込む: 右側から正面を経て左側へ)
            CreateOrUpdatePresetAsset(
                "ArcAround.asset",
                "Arc Around",
                VLiveCameraShot.ShotType.Spline,
                45f,
                Vector3.zero,
                new Vector3[]
                {
                    new Vector3(2.5f, 1.3f, 2.5f),
                    new Vector3(1.5f, 1.3f, 3.2f),
                    new Vector3(-1.5f, 1.3f, 3.2f),
                    new Vector3(-2.5f, 1.3f, 2.5f)
                },
                0.2f,
                0.25f
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VLiveCameraPresetAssetCreator] Default 6 motion presets updated to +Z front successfully.");
        }

        /// <summary>
        /// 不足している初期Motion Preset Assetのみを生成します。既存のPreset Assetは変更しません。
        /// </summary>
        public static void CreateMissingDefaultPresets()
        {
            if (!Directory.Exists(PresetFolderPath))
            {
                Directory.CreateDirectory(PresetFolderPath);
                AssetDatabase.Refresh();
            }

            bool anyCreated = false;

            anyCreated |= CreatePresetAssetIfMissing(
                "FixedMedium.asset",
                "Fixed Medium",
                VLiveCameraShot.ShotType.Fixed,
                40f,
                Vector3.zero,
                new Vector3[]
                {
                    new Vector3(0f, 1.3f, 3.5f)
                },
                0.2f,
                0.25f
            );

            anyCreated |= CreatePresetAssetIfMissing(
                "PushIn.asset",
                "Push In",
                VLiveCameraShot.ShotType.Spline,
                45f,
                Vector3.zero,
                new Vector3[]
                {
                    new Vector3(0f, 1.3f, 4.5f),
                    new Vector3(0f, 1.3f, 1.8f)
                },
                0.25f,
                0.3f
            );

            anyCreated |= CreatePresetAssetIfMissing(
                "PullOut.asset",
                "Pull Out",
                VLiveCameraShot.ShotType.Spline,
                45f,
                Vector3.zero,
                new Vector3[]
                {
                    new Vector3(0f, 1.3f, 1.8f),
                    new Vector3(0f, 1.3f, 4.5f)
                },
                0.25f,
                0.3f
            );

            anyCreated |= CreatePresetAssetIfMissing(
                "TruckLeft.asset",
                "Truck Left",
                VLiveCameraShot.ShotType.Spline,
                45f,
                Vector3.zero,
                new Vector3[]
                {
                    new Vector3(2.5f, 1.3f, 3.0f),
                    new Vector3(0f, 1.3f, 3.0f),
                    new Vector3(-2.5f, 1.3f, 3.0f)
                },
                0.2f,
                0.25f
            );

            anyCreated |= CreatePresetAssetIfMissing(
                "TruckRight.asset",
                "Truck Right",
                VLiveCameraShot.ShotType.Spline,
                45f,
                Vector3.zero,
                new Vector3[]
                {
                    new Vector3(-2.5f, 1.3f, 3.0f),
                    new Vector3(0f, 1.3f, 3.0f),
                    new Vector3(2.5f, 1.3f, 3.0f)
                },
                0.2f,
                0.25f
            );

            anyCreated |= CreatePresetAssetIfMissing(
                "ArcAround.asset",
                "Arc Around",
                VLiveCameraShot.ShotType.Spline,
                45f,
                Vector3.zero,
                new Vector3[]
                {
                    new Vector3(2.5f, 1.3f, 2.5f),
                    new Vector3(1.5f, 1.3f, 3.2f),
                    new Vector3(-1.5f, 1.3f, 3.2f),
                    new Vector3(-2.5f, 1.3f, 2.5f)
                },
                0.2f,
                0.25f
            );

            if (anyCreated)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[VLiveCameraPresetAssetCreator] Missing motion presets created.");
            }
        }

        private static bool CreatePresetAssetIfMissing(
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
                return true;
            }

            return false;
        }

        private static void CreateOrUpdatePresetAsset(
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
            else
            {
                preset.Initialize(displayName, shotType, fov, targetOffset, controlPoints, initialSpeed, decelerationDistance);
                EditorUtility.SetDirty(preset);
            }
        }
    }
}
