using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using VLiveKit.Camera.Editor;

namespace VLiveKit.Camera.Tests
{
    /// <summary>
    /// 標準8カメPresetアセットの生成と基準契約を検証します。
    /// </summary>
    public class VLiveCameraStandardPresetAssetTests
    {
        // Fields

        private const string PackageRoot = "Assets/toshi.VLiveKit/VLiveCameraDirector";
        private const string MotionRoot = PackageRoot + "/Presets/Motion";
        private const string ExperimentalRoot = MotionRoot + "/Experimental";
        private const string StandardRoot = MotionRoot + "/Standard";

        private static readonly string[] s_experimentalAssetNames =
        {
            "ArcAround",
            "CraneDrop",
            "CraneRise",
            "FixedMedium",
            "PedestalRise",
            "PullOut",
            "PushIn",
            "PushInRolling",
            "TruckLeft",
            "TruckRight"
        };

        private static readonly string[] s_standardAssetNames =
        {
            "01_FOHWide",
            "02_FrontMedium",
            "03_FrontClose",
            "04_KamiAngle",
            "05_ShimoAngle",
            "06_CraneHigh",
            "07_RailLow",
            "08_DetailAccent"
        };


        // Methods

        /// <summary>
        /// 既存PresetをExperimentalへ移動し、標準8カメPresetを生成します。
        /// </summary>
        [Test]
        [Explicit("Preset assets are intentionally updated only when this test is selected explicitly.")]
        public void GenerateStandardEightCameraPresetAssets()
        {
            EnsureFolder(MotionRoot, "Experimental");
            EnsureFolder(MotionRoot, "Standard");
            AssetDatabase.Refresh();

            MoveRootPresetsToExperimental();

            VLiveCameraRigProfile rigProfile = AssetDatabase.LoadAssetAtPath<VLiveCameraRigProfile>(
                PackageRoot + "/Presets/DefaultRigProfile.asset");
            Assert.That(rigProfile, Is.Not.Null, "DefaultRigProfile asset was not found.");

            CreateFixedPreset(
                "01_FOHWide",
                "01 FOH Wide",
                ShotSize.Wide,
                ShotEnergy.Calm,
                new Vector3(0f, 2.65f, 9.5f),
                28f,
                new Vector2(0f, 0.02f),
                new Vector2(0.5f, 0.55f),
                "ステージ、照明、フォーメーションを把握する正面全景。迷った時の最も広い戻り先。",
                rigProfile);

            CreateFixedPreset(
                "02_FrontMedium",
                "02 Front Medium",
                ShotSize.Full,
                ShotEnergy.Calm,
                new Vector3(0f, 1.55f, 5.2f),
                50f,
                new Vector2(0f, 0.05f),
                new Vector2(0.32f, 0.36f),
                "正面から情報を安定して伝える基準ミドル。切り替え判断に迷った時の戻り先。",
                rigProfile);

            CreateFixedPreset(
                "03_FrontClose",
                "03 Front Close",
                ShotSize.BustUp,
                ShotEnergy.Normal,
                new Vector3(0f, 1.65f, 4.25f),
                85f,
                new Vector2(0f, 0.08f),
                new Vector2(0.24f, 0.28f),
                "歌詞、表情、感情を優先する正面寄り。過剰な動きを加えない。",
                rigProfile);

            CreateFixedPreset(
                "04_KamiAngle",
                "04 Kami Angle",
                ShotSize.BustUp,
                ShotEnergy.Normal,
                new Vector3(-3.65f, 1.6f, 4.85f),
                58f,
                new Vector2(0.04f, 0.07f),
                new Vector2(0.34f, 0.38f),
                "客席から見て右、演者から見て左の上手側から、衣装、髪型、腕、プロップを見せる斜め固定画。",
                rigProfile);

            CreateFixedPreset(
                "05_ShimoAngle",
                "05 Shimo Angle",
                ShotSize.BustUp,
                ShotEnergy.Normal,
                new Vector3(3.65f, 1.6f, 4.85f),
                58f,
                new Vector2(-0.04f, 0.07f),
                new Vector2(0.34f, 0.38f),
                "客席から見て左、演者から見て右の下手側から、衣装、髪型、腕、プロップを見せる斜め固定画。",
                rigProfile);

            CreateMotionPreset(
                "06_CraneHigh",
                "06 Crane High",
                MotionFamily.Crane,
                ShotSize.Wide,
                ShotEnergy.Dynamic,
                6.2f,
                new[]
                {
                    CreateBrokenKnot(
                        new Vector3(4.2f, 1.2f, 6.6f),
                        new Vector3(0.4f, -0.8f, -0.15f),
                        new Vector3(-0.55f, 1.05f, 0.18f)),
                    CreateBrokenKnot(
                        new Vector3(3f, 3.45f, 7.05f),
                        new Vector3(0.65f, -1.15f, -0.22f),
                        new Vector3(-0.7f, 1.05f, 0.3f)),
                    CreateBrokenKnot(
                        new Vector3(1.25f, 5.55f, 7.8f),
                        new Vector3(0.75f, -0.9f, -0.36f),
                        new Vector3(-0.35f, 0.4f, 0.18f))
                },
                CreateCraneProgressCurve(),
                32f,
                new Vector2(0f, 0.03f),
                new Vector2(0.42f, 0.46f),
                "群舞、隊形変化、照明構造を見せるクレーン上昇。終端へ向けて減速し、俯瞰の決め画で止まる。",
                rigProfile);

            CreateMotionPreset(
                "07_RailLow",
                "07 Rail Low",
                MotionFamily.Truck,
                ShotSize.Full,
                ShotEnergy.Dynamic,
                5.8f,
                new[]
                {
                    CreateBrokenKnot(
                        new Vector3(-3.8f, 0.62f, 5.15f),
                        new Vector3(-1.1f, -0.08f, 0.3f),
                        new Vector3(1.25f, 0.08f, -0.34f)),
                    CreateBrokenKnot(
                        new Vector3(0f, 0.72f, 4.15f),
                        new Vector3(-1.5f, -0.04f, 0.05f),
                        new Vector3(1.5f, 0.04f, -0.05f)),
                    CreateBrokenKnot(
                        new Vector3(3.8f, 0.72f, 5.15f),
                        new Vector3(-1.25f, 0f, -0.34f),
                        new Vector3(1.1f, 0.05f, 0.3f))
                },
                CreateRailProgressCurve(),
                40f,
                new Vector2(0f, 0.07f),
                new Vector2(0.3f, 0.34f),
                "低いレール位置から迫力と衣装のシルエットを見せる横移動。終端の決め画へ自然に収束する。",
                rigProfile);

            CreateFixedPreset(
                "08_DetailAccent",
                "08 Detail Accent",
                ShotSize.CloseUp,
                ShotEnergy.Normal,
                new Vector3(1.35f, 1.75f, 3.5f),
                100f,
                new Vector2(-0.08f, 0.1f),
                new Vector2(0.22f, 0.26f),
                "表情やディテールを短く拾う味付け枠。呼吸、遊び、視線移動の緩衝に用途を限定する。",
                rigProfile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AssertStandardPresetAssets();
            AssertExperimentalPresetAssets();
        }

        /// <summary>
        /// 標準8カメPresetが基準契約を満たすことを検証します。
        /// </summary>
        [Test]
        public void StandardEightCameraPresetAssetsMatchBaselineContract()
        {
            AssertStandardPresetAssets();
            AssertExperimentalPresetAssets();
        }

        private static void EnsureFolder(string parent, string childName)
        {
            string path = parent + "/" + childName;
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string guid = AssetDatabase.CreateFolder(parent, childName);
            Assert.That(guid, Is.Not.Empty, $"Failed to create folder '{path}'.");
        }

        private static void MoveRootPresetsToExperimental()
        {
            string[] guids = AssetDatabase.FindAssets("t:VLiveCameraMotionPreset", new[] { MotionRoot });
            foreach (string guid in guids)
            {
                string source = AssetDatabase.GUIDToAssetPath(guid);
                string directory = Path.GetDirectoryName(source)?.Replace('\\', '/');
                if (!string.Equals(directory, MotionRoot, StringComparison.Ordinal))
                {
                    continue;
                }

                string destination = ExperimentalRoot + "/" + Path.GetFileName(source);
                Assert.That(
                    AssetDatabase.LoadMainAssetAtPath(destination),
                    Is.Null,
                    $"Destination asset already exists: '{destination}'.");

                string error = AssetDatabase.MoveAsset(source, destination);
                Assert.That(error, Is.Empty, $"Failed to move '{source}' to '{destination}'.");
            }
        }

        private static void CreateFixedPreset(
            string assetName,
            string displayName,
            ShotSize shotSize,
            ShotEnergy energy,
            Vector3 position,
            float focalLength,
            Vector2 screenPosition,
            Vector2 damping,
            string description,
            VLiveCameraRigProfile rigProfile)
        {
            VLiveCameraMotionPresetData data = CreateCommonData(
                displayName,
                VLiveCameraShotType.Fixed,
                MotionFamily.Fixed,
                shotSize,
                energy,
                4f,
                AnimationCurve.Linear(0f, 0f, 1f, 1f),
                focalLength,
                screenPosition,
                damping,
                description,
                rigProfile);
            data.Body = new VLiveCameraBodyTrack
            {
                Knots = new[] { new MotionKnot(position) },
                IsClosed = false,
                ReferenceSplineLength = 0f,
                StartDistance = 0f,
                EndDistance = 0f
            };

            CreateOrUpdateAsset(assetName, data);
        }

        private static void CreateMotionPreset(
            string assetName,
            string displayName,
            MotionFamily family,
            ShotSize shotSize,
            ShotEnergy energy,
            float duration,
            MotionKnot[] knots,
            AnimationCurve progressCurve,
            float focalLength,
            Vector2 screenPosition,
            Vector2 damping,
            string description,
            VLiveCameraRigProfile rigProfile)
        {
            VLiveCameraMotionPresetData data = CreateCommonData(
                displayName,
                VLiveCameraShotType.Spline,
                family,
                shotSize,
                energy,
                duration,
                progressCurve,
                focalLength,
                screenPosition,
                damping,
                description,
                rigProfile);
            data.Body = new VLiveCameraBodyTrack
            {
                Knots = knots,
                IsClosed = false,
                ReferenceSplineLength = CalculateSplineLength(knots),
                StartDistance = 0f,
                EndDistance = 0f
            };

            CreateOrUpdateAsset(assetName, data);
        }

        private static VLiveCameraMotionPresetData CreateCommonData(
            string displayName,
            VLiveCameraShotType shotType,
            MotionFamily family,
            ShotSize shotSize,
            ShotEnergy energy,
            float duration,
            AnimationCurve progressCurve,
            float focalLength,
            Vector2 screenPosition,
            Vector2 damping,
            string description,
            VLiveCameraRigProfile rigProfile)
        {
            return new VLiveCameraMotionPresetData
            {
                Identity = new VLiveCameraMotionIdentity
                {
                    DisplayName = displayName,
                    ShotType = shotType,
                    Family = family,
                    Size = shotSize,
                    Energy = energy,
                    Description = description
                },
                Timing = new VLiveCameraTimingTrack
                {
                    ClipDuration = duration,
                    ProgressCurve = progressCurve,
                    ScaleMode = ScaleTimingMode.PreserveDuration,
                    MinSpeedMultiplier = 0.25f,
                    MaxSpeedMultiplier = 2f,
                    SpeedStep = 0.25f
                },
                Aim = new VLiveCameraAimTrack
                {
                    AimOffset = Vector3.zero,
                    ScreenPosition = screenPosition,
                    DeadZoneEnabled = true,
                    DeadZoneSize = new Vector2(0.08f, 0.06f),
                    HardLimitsEnabled = true,
                    HardLimitsSize = new Vector2(0.72f, 0.68f),
                    HardLimitsOffset = Vector2.zero,
                    Damping = damping,
                    LookaheadEnabled = false,
                    LookaheadTime = 0f,
                    LookaheadSmoothing = 0f,
                    CenterOnActivate = true
                },
                Lens = new VLiveCameraLensTrack
                {
                    Mode = LensMode.FocalLength,
                    FocalLength = focalLength,
                    FieldOfView = 40f,
                    FieldOfViewCurve = new AnimationCurve(),
                    FocalLengthCurve = new AnimationCurve()
                },
                Roll = new VLiveCameraRollTrack
                {
                    Mode = RollMode.MaintainHorizon,
                    Curve = new AnimationCurve()
                },
                Activation = new VLiveCameraActivationTrack
                {
                    EntryMode = EntryMode.Static,
                    InTime = 0f,
                    OutTime = duration,
                    ExitBehavior = ExitBehavior.Hold
                },
                RigProfile = rigProfile
            };
        }

        private static void CreateOrUpdateAsset(string assetName, VLiveCameraMotionPresetData data)
        {
            string path = StandardRoot + "/" + assetName + ".asset";
            VLiveCameraMotionPreset preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(path);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<VLiveCameraMotionPreset>();
                AssetDatabase.CreateAsset(preset, path);
            }

            preset.name = assetName;
            preset.Initialize(data);
            EditorUtility.SetDirty(preset);
        }

        private static MotionKnot CreateBrokenKnot(Vector3 position, Vector3 tangentIn, Vector3 tangentOut)
        {
            return new MotionKnot(
                position,
                tangentIn,
                tangentOut,
                TangentMode.Broken,
                Quaternion.identity);
        }

        private static AnimationCurve CreateCraneProgressCurve()
        {
            return CreateClampedCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.2f, 0.09f, 0.82f, 0.82f),
                new Keyframe(0.7f, 0.78f, 1.18f, 1.18f),
                new Keyframe(1f, 1f, 0f, 0f));
        }

        private static AnimationCurve CreateRailProgressCurve()
        {
            return CreateClampedCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.18f, 0.08f, 0.78f, 0.78f),
                new Keyframe(0.74f, 0.84f, 1.05f, 1.05f),
                new Keyframe(1f, 1f, 0f, 0f));
        }

        private static AnimationCurve CreateClampedCurve(params Keyframe[] keys)
        {
            return new AnimationCurve(keys)
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
        }

        private static float CalculateSplineLength(MotionKnot[] knots)
        {
            var spline = new Spline();
            for (int i = 0; i < knots.Length; i++)
            {
                MotionKnot source = knots[i];
                var knot = new BezierKnot(
                    (float3)source.Position,
                    (float3)source.TangentIn,
                    (float3)source.TangentOut,
                    (quaternion)source.Rotation);

                spline.Add(knot);
                spline.SetTangentMode(i, source.TangentMode);

                if (source.TangentMode == TangentMode.AutoSmooth)
                {
                    spline.SetAutoSmoothTension(i, Mathf.Clamp01(source.AutoSmoothTension));
                }
                else
                {
                    spline[i] = knot;
                }
            }

            return spline.GetLength();
        }

        private static void AssertStandardPresetAssets()
        {
            AssertAssetNames(StandardRoot, s_standardAssetNames);

            AssertRole("01_FOHWide", VLiveCameraShotType.Fixed, ShotSize.Wide, 28f);
            AssertRole("02_FrontMedium", VLiveCameraShotType.Fixed, ShotSize.Full, 50f);
            AssertRole("03_FrontClose", VLiveCameraShotType.Fixed, ShotSize.BustUp, 85f);
            AssertRole("04_KamiAngle", VLiveCameraShotType.Fixed, ShotSize.BustUp, 58f);
            AssertRole("05_ShimoAngle", VLiveCameraShotType.Fixed, ShotSize.BustUp, 58f);
            AssertRole("06_CraneHigh", VLiveCameraShotType.Spline, ShotSize.Wide, 32f);
            AssertRole("07_RailLow", VLiveCameraShotType.Spline, ShotSize.Full, 40f);
            AssertRole("08_DetailAccent", VLiveCameraShotType.Fixed, ShotSize.CloseUp, 100f);

            VLiveCameraRigProfile expectedRigProfile = AssetDatabase.LoadAssetAtPath<VLiveCameraRigProfile>(
                PackageRoot + "/Presets/DefaultRigProfile.asset");
            foreach (string assetName in s_standardAssetNames)
            {
                string path = StandardRoot + "/" + assetName + ".asset";
                VLiveCameraMotionPreset preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(path);
                Assert.That(preset, Is.Not.Null, $"Standard preset was not found: '{path}'.");
                Assert.That(preset.LensMode, Is.EqualTo(LensMode.FocalLength), preset.DisplayName);
                Assert.That(preset.RigProfile, Is.SameAs(expectedRigProfile), preset.DisplayName);
                Assert.That(preset.EntryMode, Is.EqualTo(EntryMode.Static), preset.DisplayName);
                Assert.That(preset.ExitBehavior, Is.EqualTo(ExitBehavior.Hold), preset.DisplayName);

                VLiveCameraMotionValidationReport report = VLiveCameraMotionValidator.ValidatePreset(
                    preset,
                    preset.ReferenceSplineLength);
                string diagnostics = string.Join(
                    " | ",
                    report.Messages.Select(message => $"{message.Severity}: {message.Category}: {message.Message}"));
                Assert.That(report.HasErrors, Is.False, $"{preset.DisplayName}: {diagnostics}");

                if (preset.ShotType == VLiveCameraShotType.Spline)
                {
                    Assert.That(preset.Knots, Has.Length.GreaterThanOrEqualTo(2), preset.DisplayName);
                    Assert.That(preset.ReferenceSplineLength, Is.GreaterThan(0f), preset.DisplayName);

                    Keyframe firstKey = preset.ProgressCurve.keys[0];
                    Keyframe lastKey = preset.ProgressCurve.keys[preset.ProgressCurve.length - 1];
                    Assert.That(firstKey.outTangent, Is.EqualTo(0f).Within(0.0001f), preset.DisplayName);
                    Assert.That(lastKey.inTangent, Is.EqualTo(0f).Within(0.0001f), preset.DisplayName);
                    Assert.That(preset.ProgressCurve.Evaluate(1f), Is.EqualTo(1f).Within(0.0001f), preset.DisplayName);
                }
                else
                {
                    Assert.That(preset.Knots, Has.Length.EqualTo(1), preset.DisplayName);
                    Assert.That(preset.ReferenceSplineLength, Is.EqualTo(0f).Within(0.0001f), preset.DisplayName);
                }
            }
        }

        private static void AssertRole(
            string assetName,
            VLiveCameraShotType expectedShotType,
            ShotSize expectedSize,
            float expectedFocalLength)
        {
            string path = StandardRoot + "/" + assetName + ".asset";
            VLiveCameraMotionPreset preset = AssetDatabase.LoadAssetAtPath<VLiveCameraMotionPreset>(path);

            Assert.That(preset, Is.Not.Null, $"Standard preset was not found: '{path}'.");
            Assert.That(preset.ShotType, Is.EqualTo(expectedShotType), assetName);
            Assert.That(preset.Size, Is.EqualTo(expectedSize), assetName);
            Assert.That(preset.FocalLength, Is.EqualTo(expectedFocalLength).Within(0.001f), assetName);
        }

        private static void AssertExperimentalPresetAssets()
        {
            AssertAssetNames(ExperimentalRoot, s_experimentalAssetNames);
        }

        private static void AssertAssetNames(string folder, string[] expectedNames)
        {
            string[] actualNames = AssetDatabase.FindAssets("t:VLiveCameraMotionPreset", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => string.Equals(
                    Path.GetDirectoryName(path)?.Replace('\\', '/'),
                    folder,
                    StringComparison.Ordinal))
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            string[] sortedExpectedNames = expectedNames.OrderBy(name => name, StringComparer.Ordinal).ToArray();

            Assert.That(actualNames, Is.EqualTo(sortedExpectedNames), folder);
        }
    }
}
