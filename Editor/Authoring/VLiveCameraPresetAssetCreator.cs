using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// 初期Motion PaletteをCamera Performanceデータから生成します。
    /// </summary>
    public static class VLiveCameraPresetAssetCreator
    {
        // Fields

        public const string MotionPresetFolderPath = "Assets/toshi.VLiveKit/VLiveCameraUnit/Presets/Motion";

        public const string RigProfileFolderPath = "Assets/toshi.VLiveKit/VLiveCameraUnit/Presets/RigProfiles";


        // Methods

        /// <summary>
        /// 同梱Motion Presetを現在のデータ形式と既定演出へ更新します。
        /// </summary>
        [MenuItem("Tools/VLive Camera/Create Default Presets", priority = 20)]
        public static void CreateOrUpdateDefaultPresets()
        {
            CreateDefaultPresetsInternal(true);
        }

        /// <summary>
        /// 不足している同梱Motion Presetだけを生成します。
        /// </summary>
        public static void CreateMissingDefaultPresets()
        {
            CreateDefaultPresetsInternal(false);
        }

        private static void CreateDefaultPresetsInternal(bool overwriteExisting)
        {
            EnsurePresetFolders();
            VLiveCameraRigProfile profile = GetOrCreateDefaultProfile();

            CreateOrUpdatePreset("FixedMedium.asset", CreateFixedMedium(profile), overwriteExisting);
            CreateOrUpdatePreset("PushIn.asset", CreatePushIn(profile), overwriteExisting);
            CreateOrUpdatePreset("PullOut.asset", CreatePullReveal(profile), overwriteExisting);
            CreateOrUpdatePreset("TruckLeft.asset", CreateTruck(profile, false), overwriteExisting);
            CreateOrUpdatePreset("TruckRight.asset", CreateTruck(profile, true), overwriteExisting);
            CreateOrUpdatePreset("ArcAround.asset", CreateArcAround(profile), overwriteExisting);
            CreateOrUpdatePreset("PushInRolling.asset", CreateOrbitPush(profile), overwriteExisting);
            CreateOrUpdatePreset("CraneRise.asset", CreateCrane(profile, false), overwriteExisting);
            CreateOrUpdatePreset("CraneDrop.asset", CreateCrane(profile, true), overwriteExisting);
            CreateOrUpdatePreset("PedestalRise.asset", CreatePedestalRise(profile), overwriteExisting);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VLiveCameraPresetAssetCreator] Default Camera Performance presets updated.");
        }

        private static void EnsurePresetFolders()
        {
            EnsureFolder(MotionPresetFolderPath);
            EnsureFolder(RigProfileFolderPath);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                return;
            }

            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        private static VLiveCameraRigProfile GetOrCreateDefaultProfile()
        {
            string profilePath = $"{RigProfileFolderPath}/DefaultRigProfile.asset";
            VLiveCameraRigProfile profile = AssetDatabase.LoadAssetAtPath<VLiveCameraRigProfile>(profilePath);
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<VLiveCameraRigProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static VLiveCameraMotionPresetData CreateFixedMedium(VLiveCameraRigProfile profile)
        {
            return CreatePerformance(
                "Fixed Medium",
                VLiveCameraShotType.Fixed,
                MotionFamily.Fixed,
                ShotSize.Medium,
                ShotEnergy.Calm,
                "被写体正面を安定して捉える基準ミドルショット。",
                profile,
                new[] { Knot(new Vector3(0f, 1.35f, 3.6f)) },
                4f,
                CruiseCurve(),
                42f,
                new AnimationCurve(),
                EntryMode.Static,
                0f,
                4f,
                Vector2.up * 0.06f
            );
        }

        private static VLiveCameraMotionPresetData CreatePushIn(VLiveCameraRigProfile profile)
        {
            return CreatePerformance(
                "Push In Rise",
                VLiveCameraShotType.Spline,
                MotionFamily.Push,
                ShotSize.Medium,
                ShotEnergy.Dynamic,
                "低い正面ワイドからわずかに回り込みながら上昇して寄る。",
                profile,
                new[]
                {
                    Knot(new Vector3(0.35f, 0.95f, 5.4f), new Vector3(0f, 0f, -1.3f)),
                    Knot(new Vector3(0.18f, 1.35f, 3.5f), new Vector3(-0.22f, 0.28f, -1.15f)),
                    Knot(new Vector3(-0.12f, 1.85f, 1.75f), new Vector3(-0.18f, 0.24f, -0.75f))
                },
                3.4f,
                FastCruiseCurve(),
                48f,
                Curve(48f, 36f),
                EntryMode.Rolling,
                0.15f,
                3.2f,
                Vector2.up * 0.08f
            );
        }

        private static VLiveCameraMotionPresetData CreatePullReveal(VLiveCameraRigProfile profile)
        {
            return CreatePerformance(
                "Pull Out Reveal",
                VLiveCameraShotType.Spline,
                MotionFamily.Pull,
                ShotSize.Close,
                ShotEnergy.Dynamic,
                "低いクローズから上昇し、ステージ空間を見せるリビール。",
                profile,
                new[]
                {
                    Knot(new Vector3(-0.1f, 1.05f, 1.65f), new Vector3(0.2f, 0.22f, 0.75f)),
                    Knot(new Vector3(0.2f, 1.65f, 3.35f), new Vector3(0.15f, 0.35f, 1.2f)),
                    Knot(new Vector3(-0.35f, 2.45f, 5.9f), new Vector3(-0.25f, 0.35f, 1.25f))
                },
                3.8f,
                FastCruiseCurve(),
                34f,
                Curve(34f, 52f),
                EntryMode.Rolling,
                0.12f,
                3.6f,
                Vector2.up * 0.06f
            );
        }

        private static VLiveCameraMotionPresetData CreateTruck(VLiveCameraRigProfile profile, bool moveRight)
        {
            float sign = moveRight ? 1f : -1f;
            string direction = moveRight ? "Right" : "Left";

            return CreatePerformance(
                $"Truck {direction} Float",
                VLiveCameraShotType.Spline,
                MotionFamily.Truck,
                ShotSize.Medium,
                ShotEnergy.Normal,
                $"被写体との距離を変化させながら{direction.ToLowerInvariant()}へ横断する有人感のあるトラック。",
                profile,
                new[]
                {
                    Knot(new Vector3(-3.1f * sign, 1.0f, 3.65f), new Vector3(1.2f * sign, 0.18f, -0.2f)),
                    Knot(new Vector3(-0.45f * sign, 1.45f, 3.05f), new Vector3(1.5f * sign, 0.2f, 0.05f)),
                    Knot(new Vector3(3.2f * sign, 1.85f, 3.7f), new Vector3(1.45f * sign, 0.12f, 0.35f))
                },
                4.2f,
                CruiseCurve(),
                45f,
                Curve(46f, 42f),
                EntryMode.Rolling,
                0.18f,
                4f,
                new Vector2(0.04f * sign, 0.07f)
            );
        }

        private static VLiveCameraMotionPresetData CreateArcAround(VLiveCameraRigProfile profile)
        {
            VLiveCameraMotionPresetData data = CreatePerformance(
                "Arc Around Lift",
                VLiveCameraShotType.Spline,
                MotionFamily.Arc,
                ShotSize.Medium,
                ShotEnergy.Dynamic,
                "前面を大きく回り込みながら上昇し、画角も締めるヒーローショット。",
                profile,
                new[]
                {
                    Knot(new Vector3(3.2f, 0.85f, 2.8f), new Vector3(-0.55f, 0.2f, 0.8f)),
                    Knot(new Vector3(1.9f, 1.3f, 3.8f), new Vector3(-1.25f, 0.3f, 0.65f)),
                    Knot(new Vector3(-0.45f, 1.9f, 4.15f), new Vector3(-1.45f, 0.35f, -0.15f)),
                    Knot(new Vector3(-3.0f, 2.35f, 2.85f), new Vector3(-0.95f, 0.15f, -0.9f))
                },
                4.6f,
                CruiseCurve(),
                50f,
                Curve(50f, 39f),
                EntryMode.Rolling,
                0.2f,
                4.35f,
                new Vector2(-0.04f, 0.08f)
            );

            data.Roll.Mode = RollMode.RollCurve;
            data.Roll.Curve = new AnimationCurve(
                new Keyframe(0f, -1.2f),
                new Keyframe(0.5f, 0.6f),
                new Keyframe(1f, 0f)
            );
            return data;
        }

        private static VLiveCameraMotionPresetData CreateOrbitPush(VLiveCameraRigProfile profile)
        {
            return CreatePerformance(
                "Orbit Push",
                VLiveCameraShotType.Spline,
                MotionFamily.Gimbal,
                ShotSize.Medium,
                ShotEnergy.Dynamic,
                "斜め外側から旋回しながら被写体へ素早く寄る。",
                profile,
                new[]
                {
                    Knot(new Vector3(4.0f, 1.15f, 4.2f), new Vector3(-1.1f, 0.2f, -0.45f)),
                    Knot(new Vector3(2.35f, 1.45f, 3.2f), new Vector3(-1.25f, 0.25f, -0.8f)),
                    Knot(new Vector3(0.65f, 1.8f, 1.9f), new Vector3(-0.75f, 0.15f, -0.55f))
                },
                3.5f,
                FastCruiseCurve(),
                52f,
                Curve(52f, 37f),
                EntryMode.Rolling,
                0.25f,
                3.3f,
                new Vector2(-0.05f, 0.07f)
            );
        }

        private static VLiveCameraMotionPresetData CreateCrane(VLiveCameraRigProfile profile, bool descending)
        {
            MotionKnot[] knots =
            {
                Knot(new Vector3(2.5f, 0.55f, 4.1f), new Vector3(-0.4f, 0.85f, 0.1f)),
                Knot(new Vector3(1.65f, 2.15f, 4.55f), new Vector3(-0.55f, 1.0f, 0.4f)),
                Knot(new Vector3(0.1f, 4.35f, 5.7f), new Vector3(-0.75f, 1.05f, 0.65f))
            };

            if (descending)
            {
                knots = ReverseKnots(knots);
            }

            return CreatePerformance(
                descending ? "Crane Drop" : "Crane Rise",
                VLiveCameraShotType.Spline,
                MotionFamily.Crane,
                descending ? ShotSize.Wide : ShotSize.Full,
                ShotEnergy.Dynamic,
                descending ? "高所ワイドから演者の高さへ降下して着地する。" : "演者の低い位置から高所ワイドへ持ち上がる。",
                profile,
                knots,
                4.8f,
                CruiseCurve(),
                descending ? 54f : 42f,
                descending ? Curve(54f, 40f) : Curve(42f, 56f),
                EntryMode.Rolling,
                0.2f,
                4.55f,
                Vector2.up * 0.08f
            );
        }

        private static VLiveCameraMotionPresetData CreatePedestalRise(VLiveCameraRigProfile profile)
        {
            return CreatePerformance(
                "Pedestal Rise",
                VLiveCameraShotType.Spline,
                MotionFamily.Pedestal,
                ShotSize.Medium,
                ShotEnergy.Normal,
                "距離をほぼ保ったまま低い視点から高い視点へ持ち上がる。",
                profile,
                new[]
                {
                    Knot(new Vector3(0f, 0.45f, 3.1f), new Vector3(0.08f, 0.85f, -0.05f)),
                    Knot(new Vector3(0.15f, 1.75f, 3.0f), new Vector3(-0.08f, 0.9f, 0.08f)),
                    Knot(new Vector3(-0.1f, 3.25f, 3.45f), new Vector3(-0.12f, 0.75f, 0.25f))
                },
                3.9f,
                CruiseCurve(),
                42f,
                Curve(42f, 47f),
                EntryMode.Rolling,
                0.15f,
                3.65f,
                Vector2.up * 0.08f
            );
        }

        private static VLiveCameraMotionPresetData CreatePerformance(
            string displayName,
            VLiveCameraShotType shotType,
            MotionFamily family,
            ShotSize size,
            ShotEnergy energy,
            string description,
            VLiveCameraRigProfile profile,
            MotionKnot[] knots,
            float duration,
            AnimationCurve progressCurve,
            float fieldOfView,
            AnimationCurve fieldOfViewCurve,
            EntryMode entryMode,
            float inTime,
            float outTime,
            Vector2 screenPosition)
        {
            return new VLiveCameraMotionPresetData
            {
                Identity = new VLiveCameraMotionIdentity
                {
                    DisplayName = displayName,
                    ShotType = shotType,
                    Family = family,
                    Size = size,
                    Energy = energy,
                    Description = description
                },
                Body = new VLiveCameraBodyTrack
                {
                    Knots = knots,
                    ReferenceSplineLength = CalculateSplineLength(knots),
                    IsClosed = false
                },
                Timing = new VLiveCameraTimingTrack
                {
                    ClipDuration = duration,
                    ProgressCurve = progressCurve,
                    ScaleMode = shotType == VLiveCameraShotType.Spline ? ScaleTimingMode.PreserveSpeed : ScaleTimingMode.PreserveDuration,
                    MinSpeedMultiplier = 0.25f,
                    MaxSpeedMultiplier = 3f,
                    SpeedStep = 0.25f
                },
                Aim = new VLiveCameraAimTrack
                {
                    ScreenPosition = screenPosition,
                    Damping = new Vector2(0.28f, 0.32f),
                    CenterOnActivate = true
                },
                Lens = new VLiveCameraLensTrack
                {
                    Mode = LensMode.FieldOfView,
                    FieldOfView = fieldOfView,
                    FieldOfViewCurve = fieldOfViewCurve
                },
                Roll = new VLiveCameraRollTrack(),
                Activation = new VLiveCameraActivationTrack
                {
                    EntryMode = entryMode,
                    InTime = inTime,
                    OutTime = outTime,
                    ExitBehavior = ExitBehavior.Hold
                },
                RigProfile = profile
            };
        }

        private static MotionKnot Knot(Vector3 position)
        {
            return new MotionKnot(position, Vector3.zero, Vector3.zero, TangentMode.AutoSmooth, Quaternion.identity);
        }

        private static MotionKnot Knot(Vector3 position, Vector3 tangent)
        {
            return new MotionKnot(position, -tangent, tangent, TangentMode.Continuous, Quaternion.identity);
        }

        private static AnimationCurve Curve(float startValue, float endValue)
        {
            return AnimationCurve.EaseInOut(0f, startValue, 1f, endValue);
        }

        private static AnimationCurve CruiseCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0.55f, 0.55f),
                new Keyframe(0.16f, 0.12f, 1f, 1f),
                new Keyframe(0.84f, 0.88f, 1f, 1f),
                new Keyframe(1f, 1f, 0.55f, 0.55f)
            );
        }

        private static AnimationCurve FastCruiseCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0.8f, 0.8f),
                new Keyframe(0.12f, 0.1f, 1.05f, 1.05f),
                new Keyframe(0.88f, 0.92f, 1.05f, 1.05f),
                new Keyframe(1f, 1f, 0.65f, 0.65f)
            );
        }

        private static float CalculateSplineLength(MotionKnot[] knots)
        {
            if (knots == null || knots.Length < 2)
            {
                return 0f;
            }

            var spline = new Spline();
            for (int i = 0; i < knots.Length; i++)
            {
                MotionKnot knot = knots[i];
                var bezierKnot = new BezierKnot(
                    (float3)knot.Position,
                    (float3)knot.TangentIn,
                    (float3)knot.TangentOut,
                    (quaternion)knot.Rotation);
                spline.Add(bezierKnot);
                spline.SetTangentMode(i, knot.TangentMode);

                if (knot.TangentMode == TangentMode.AutoSmooth)
                {
                    spline.SetAutoSmoothTension(i, knot.AutoSmoothTension);
                }
                else
                {
                    spline[i] = bezierKnot;
                }
            }

            return spline.GetLength();
        }

        private static MotionKnot[] ReverseKnots(MotionKnot[] source)
        {
            var reversed = new MotionKnot[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                MotionKnot knot = source[source.Length - 1 - i];
                reversed[i] = new MotionKnot(
                    knot.Position,
                    knot.TangentOut,
                    knot.TangentIn,
                    knot.TangentMode,
                    knot.Rotation,
                    knot.AutoSmoothTension
                );
            }

            return reversed;
        }

        private static void CreateOrUpdatePreset(string fileName, VLiveCameraMotionPresetData data, bool overwriteExisting)
        {
            string assetPath = $"{MotionPresetFolderPath}/{fileName}";
            VLiveCameraMotionPreset preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(assetPath);
            if (preset != null && !overwriteExisting)
            {
                return;
            }

            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<VLiveCameraMotionPreset>();
                preset.Initialize(data);
                AssetDatabase.CreateAsset(preset, assetPath);
            }
            else
            {
                data.RigProfile = preset.RigProfile != null ? preset.RigProfile : data.RigProfile;
                preset.Initialize(data);
            }

            EditorUtility.SetDirty(preset);
        }
    }
}
