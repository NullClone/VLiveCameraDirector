using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

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
        /// 初期のMotion Preset Assetを新形式で更新または生成します。
        /// </summary>
        [MenuItem("Tools/VLive Camera/Create Default Presets", priority = 20)]
        public static void CreateOrUpdateDefaultPresets()
        {
            CreateDefaultPresetsInternal(true);
        }

        /// <summary>
        /// 不足している初期Motion Preset Assetのみを生成します（既存Presetは上書きしません）。
        /// </summary>
        public static void CreateMissingDefaultPresets()
        {
            CreateDefaultPresetsInternal(false);
        }

        private static void CreateDefaultPresetsInternal(bool overwriteExisting)
        {
            if (!Directory.Exists(PresetFolderPath))
            {
                Directory.CreateDirectory(PresetFolderPath);
                AssetDatabase.Refresh();
            }

            // 0. Default Rig Profile
            string profilePath = $"{PresetFolderPath}/DefaultRigProfile.asset";
            VLiveCameraRigProfile defaultProfile = AssetDatabase.LoadAssetAtPath<VLiveCameraRigProfile>(profilePath);
            if (defaultProfile == null)
            {
                defaultProfile = ScriptableObject.CreateInstance<VLiveCameraRigProfile>();
                AssetDatabase.CreateAsset(defaultProfile, profilePath);
                EditorUtility.SetDirty(defaultProfile);
            }

            // 1. Fixed Medium (+Z 正面ミドル)
            CreateOrUpdatePreset(
                "FixedMedium.asset",
                "Fixed Medium",
                VLiveCameraShot.ShotType.Fixed,
                MotionFamily.Fixed,
                ShotSize.Medium,
                ShotEnergy.Calm,
                "被写体正面の安定した固定ミドルショット。",
                defaultProfile,
                new MotionKnot[]
                {
                    new MotionKnot(new Vector3(0f, 1.3f, 3.5f))
                },
                false,
                0f,
                5.0f,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                ScaleTimingMode.PreserveDuration,
                Vector3.zero,
                Vector2.zero,
                40f,
                EntryMode.Static,
                0f,
                5.0f,
                ExitBehavior.Hold,
                overwriteExisting
            );

            // 2. Push In (+Z 正面からTargetへ直線接近)
            CreateOrUpdatePreset(
                "PushIn.asset",
                "Push In",
                VLiveCameraShot.ShotType.Spline,
                MotionFamily.Push,
                ShotSize.Medium,
                ShotEnergy.Normal,
                "被写体正面から寄っていくドリープッシュ。",
                defaultProfile,
                new MotionKnot[]
                {
                    new MotionKnot(new Vector3(0f, 1.3f, 4.5f), Vector3.zero, new Vector3(0f, 0f, -0.9f), TangentMode.Continuous, Quaternion.identity),
                    new MotionKnot(new Vector3(0f, 1.3f, 1.8f), new Vector3(0f, 0f, 0.9f), Vector3.zero, TangentMode.Continuous, Quaternion.identity)
                },
                false,
                2.7f,
                5.0f,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                ScaleTimingMode.PreserveSpeed,
                Vector3.zero,
                Vector2.zero,
                45f,
                EntryMode.Static,
                0f,
                5.0f,
                ExitBehavior.Hold,
                overwriteExisting
            );

            // 3. Pull Out (Targetから後方+Zへ後退)
            CreateOrUpdatePreset(
                "PullOut.asset",
                "Pull Out",
                VLiveCameraShot.ShotType.Spline,
                MotionFamily.Pull,
                ShotSize.Close,
                ShotEnergy.Normal,
                "被写体のクローズから周囲を見せるドリーバック。",
                defaultProfile,
                new MotionKnot[]
                {
                    new MotionKnot(new Vector3(0f, 1.3f, 1.8f), Vector3.zero, new Vector3(0f, 0f, 0.9f), TangentMode.Continuous, Quaternion.identity),
                    new MotionKnot(new Vector3(0f, 1.3f, 4.5f), new Vector3(0f, 0f, -0.9f), Vector3.zero, TangentMode.Continuous, Quaternion.identity)
                },
                false,
                2.7f,
                5.0f,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                ScaleTimingMode.PreserveSpeed,
                Vector3.zero,
                Vector2.zero,
                45f,
                EntryMode.Static,
                0f,
                5.0f,
                ExitBehavior.Hold,
                overwriteExisting
            );

            // 4. Truck Left (Targetを捉えたまま右から左へ移動: +X -> -X)
            CreateOrUpdatePreset(
                "TruckLeft.asset",
                "Truck Left",
                VLiveCameraShot.ShotType.Spline,
                MotionFamily.Truck,
                ShotSize.Medium,
                ShotEnergy.Normal,
                "被写体を捉えたまま右から左へ平行移動するトラック。",
                defaultProfile,
                new MotionKnot[]
                {
                    new MotionKnot(new Vector3(2.5f, 1.3f, 3.0f), Vector3.zero, new Vector3(-0.8f, 0f, 0f), TangentMode.Continuous, Quaternion.identity),
                    new MotionKnot(new Vector3(0f, 1.3f, 3.0f), new Vector3(0.8f, 0f, 0f), new Vector3(-0.8f, 0f, 0f), TangentMode.Continuous, Quaternion.identity),
                    new MotionKnot(new Vector3(-2.5f, 1.3f, 3.0f), new Vector3(0.8f, 0f, 0f), Vector3.zero, TangentMode.Continuous, Quaternion.identity)
                },
                false,
                5.0f,
                5.0f,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                ScaleTimingMode.PreserveSpeed,
                Vector3.zero,
                Vector2.zero,
                45f,
                EntryMode.Static,
                0f,
                5.0f,
                ExitBehavior.Hold,
                overwriteExisting
            );

            // 5. Truck Right (Targetを捉えたまま左から右へ移動: -X -> +X)
            CreateOrUpdatePreset(
                "TruckRight.asset",
                "Truck Right",
                VLiveCameraShot.ShotType.Spline,
                MotionFamily.Truck,
                ShotSize.Medium,
                ShotEnergy.Normal,
                "被写体を捉えたまま左から右へ平行移動するトラック。",
                defaultProfile,
                new MotionKnot[]
                {
                    new MotionKnot(new Vector3(-2.5f, 1.3f, 3.0f), Vector3.zero, new Vector3(0.8f, 0f, 0f), TangentMode.Continuous, Quaternion.identity),
                    new MotionKnot(new Vector3(0f, 1.3f, 3.0f), new Vector3(-0.8f, 0f, 0f), new Vector3(0.8f, 0f, 0f), TangentMode.Continuous, Quaternion.identity),
                    new MotionKnot(new Vector3(2.5f, 1.3f, 3.0f), new Vector3(-0.8f, 0f, 0f), Vector3.zero, TangentMode.Continuous, Quaternion.identity)
                },
                false,
                5.0f,
                5.0f,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                ScaleTimingMode.PreserveSpeed,
                Vector3.zero,
                Vector2.zero,
                45f,
                EntryMode.Static,
                0f,
                5.0f,
                ExitBehavior.Hold,
                overwriteExisting
            );

            // 6. Arc Around (Target周囲を弧状に回り込む: 右側から正面を経て左側へ)
            CreateOrUpdatePreset(
                "ArcAround.asset",
                "Arc Around",
                VLiveCameraShot.ShotType.Spline,
                MotionFamily.Arc,
                ShotSize.Medium,
                ShotEnergy.Dynamic,
                "被写体の前面を弧を描いて回り込むダイナミックショット。",
                defaultProfile,
                new MotionKnot[]
                {
                    new MotionKnot(new Vector3(2.5f, 1.3f, 2.5f), Vector3.zero, new Vector3(-0.4f, 0f, 0.35f), TangentMode.Continuous, Quaternion.identity),
                    new MotionKnot(new Vector3(1.5f, 1.3f, 3.2f), new Vector3(0.45f, 0f, -0.2f), new Vector3(-0.45f, 0f, 0.2f), TangentMode.Continuous, Quaternion.identity),
                    new MotionKnot(new Vector3(-1.5f, 1.3f, 3.2f), new Vector3(0.45f, 0f, 0.2f), new Vector3(-0.45f, 0f, -0.2f), TangentMode.Continuous, Quaternion.identity),
                    new MotionKnot(new Vector3(-2.5f, 1.3f, 2.5f), new Vector3(0.4f, 0f, 0.35f), Vector3.zero, TangentMode.Continuous, Quaternion.identity)
                },
                false,
                5.6f,
                6.0f,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                ScaleTimingMode.PreserveSpeed,
                Vector3.zero,
                Vector2.zero,
                45f,
                EntryMode.Static,
                0f,
                6.0f,
                ExitBehavior.Hold,
                overwriteExisting
            );

            // 7. Push In Rolling (Rolling Entry確認用プリセット: In Time で移動中の状態から開始)
            CreateOrUpdatePreset(
                "PushInRolling.asset",
                "Push In (Rolling)",
                VLiveCameraShot.ShotType.Spline,
                MotionFamily.Push,
                ShotSize.Medium,
                ShotEnergy.Dynamic,
                "非ゼロ速度で助走区間から進行中の状態でCutインするローリングプッシュ。",
                defaultProfile,
                new MotionKnot[]
                {
                    new MotionKnot(new Vector3(0f, 1.3f, 5.0f), Vector3.zero, new Vector3(0f, 0f, -1.0f), TangentMode.Continuous, Quaternion.identity),
                    new MotionKnot(new Vector3(0f, 1.3f, 1.6f), new Vector3(0f, 0f, 1.0f), Vector3.zero, TangentMode.Continuous, Quaternion.identity)
                },
                false,
                3.4f,
                6.0f,
                AnimationCurve.Linear(0f, 0f, 1f, 1f),
                ScaleTimingMode.PreserveSpeed,
                Vector3.zero,
                Vector2.zero,
                45f,
                EntryMode.Rolling,
                1.0f,
                5.5f,
                ExitBehavior.Hold,
                overwriteExisting
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VLiveCameraPresetAssetCreator] Default motion presets updated successfully.");
        }

        private static void CreateOrUpdatePreset(
            string fileName,
            string displayName,
            VLiveCameraShot.ShotType shotType,
            MotionFamily motionFamily,
            ShotSize shotSize,
            ShotEnergy energy,
            string description,
            VLiveCameraRigProfile defaultProfile,
            MotionKnot[] knots,
            bool isClosed,
            float referenceSplineLength,
            float clipDuration,
            AnimationCurve progressCurve,
            ScaleTimingMode scaleTimingMode,
            Vector3 aimOffset,
            Vector2 screenPosition,
            float fieldOfView,
            EntryMode entryMode,
            float inTime,
            float outTime,
            ExitBehavior exitBehavior,
            bool overwriteExisting = true,
            float startDistance = 0f,
            float endDistance = 0f)
        {
            string assetPath = $"{PresetFolderPath}/{fileName}";
            VLiveCameraMotionPreset preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(assetPath);

            if (preset != null && !overwriteExisting)
            {
                return;
            }

            VLiveCameraRigProfile rigProfile = (preset != null && preset.RigProfile != null) ? preset.RigProfile : defaultProfile;

            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<VLiveCameraMotionPreset>();
                preset.Initialize(
                    displayName, shotType, motionFamily, shotSize, energy, description,
                    rigProfile, knots, isClosed, referenceSplineLength, clipDuration, progressCurve,
                    scaleTimingMode, aimOffset, screenPosition, fieldOfView, entryMode,
                    inTime, outTime, exitBehavior, startDistance, endDistance
                );
                AssetDatabase.CreateAsset(preset, assetPath);
                EditorUtility.SetDirty(preset);
            }
            else
            {
                preset.Initialize(
                    displayName, shotType, motionFamily, shotSize, energy, description,
                    rigProfile, knots, isClosed, referenceSplineLength, clipDuration, progressCurve,
                    scaleTimingMode, aimOffset, screenPosition, fieldOfView, entryMode,
                    inTime, outTime, exitBehavior, startDistance, endDistance
                );
                EditorUtility.SetDirty(preset);
            }
        }
    }
}
