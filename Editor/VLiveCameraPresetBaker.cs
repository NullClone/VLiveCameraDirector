using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// Scene上で調整されたShot（Camera、Spline、Aim Proxy、適用済みMotion設定）から
    /// 新しい再利用可能Motion Preset Assetを生成・保存するEditor専用ベイカー。
    /// </summary>
    public static class VLiveCameraPresetBaker
    {
        // Fields

        public const string DefaultPresetFolder = "Assets/toshi.VLiveKit/VLiveCameraUnit/Presets";


        // Methods

        /// <summary>
        /// Scene上のShotから新しいMotion Preset Assetを作成・保存します。
        /// 既存Assetを暗黙に上書きせず、ユニークなパスで新規作成します。
        /// </summary>
        /// <param name="rig">所属するVLiveCameraRig。</param>
        /// <param name="shot">元となるVLiveCameraShot。</param>
        /// <param name="targetAssetPath">保存先パス（省略時はPresetsフォルダ内に自動生成）。</param>
        /// <param name="showConfirmationDialog">保存実行前に確認ダイアログを表示するかどうか。</param>
        /// <returns>生成されたVLiveCameraMotionPreset。</returns>
        public static VLiveCameraMotionPreset BakePresetFromShot(
            VLiveCameraRig rig,
            VLiveCameraShot shot,
            string targetAssetPath = null,
            bool showConfirmationDialog = true)
        {
            if (shot == null)
            {
                Debug.LogError("[VLiveCameraPresetBaker] Shot が指定されていないため Preset を作成できません。");
                return null;
            }

            Vector3 targetPos = (rig != null && rig.PerformerTarget != null) ? rig.PerformerTarget.position : Vector3.zero;
            Quaternion orientation = rig != null ? rig.GetReferenceOrientation() : Quaternion.identity;
            float distanceScale = (rig != null && rig.DistanceScale > 0.001f) ? rig.DistanceScale : 1.0f;
            float motionScale = (rig != null && rig.MotionScale > 0.001f) ? rig.MotionScale : 1.0f;
            float targetHeight = (rig != null) ? rig.TargetHeight : shot.AppliedTargetHeight;

            VLiveCameraAppliedMotion applied = shot.AppliedMotion ?? new VLiveCameraAppliedMotion();

            // 1. Body Track (Spline Knots)
            MotionKnot[] knots;
            float refSplineLength = 0f;
            bool isClosed = false;

            if (shot.Type == VLiveCameraShot.ShotType.Spline && shot.SplineDolly != null && shot.SplineDolly.Spline != null)
            {
                SplineContainer container = shot.SplineDolly.Spline;
                Spline spline = container.Spline;
                isClosed = spline.Closed;
                refSplineLength = spline.GetLength();

                int knotCount = spline.Count;
                knots = new MotionKnot[knotCount];

                if (knotCount > 0)
                {
                    Vector3 worldP0 = container.transform.TransformPoint((Vector3)spline[0].Position);
                    Vector3 relWorld0 = worldP0 - targetPos;
                    Vector3 scaledP0 = Quaternion.Inverse(orientation) * relWorld0;
                    Vector3 p0 = new Vector3(scaledP0.x / distanceScale, scaledP0.y, scaledP0.z / distanceScale);

                    for (int i = 0; i < knotCount; i++)
                    {
                        Vector3 pi;
                        if (i == 0)
                        {
                            pi = p0;
                        }
                        else
                        {
                            Vector3 worldPi = container.transform.TransformPoint((Vector3)spline[i].Position);
                            Vector3 relWorldPi = worldPi - targetPos;
                            Vector3 scaledPi = Quaternion.Inverse(orientation) * relWorldPi;
                            Vector3 deltaScaled = scaledPi - scaledP0;
                            pi = p0 + deltaScaled / motionScale;
                        }

                        Vector3 worldTanIn = container.transform.TransformVector((Vector3)spline[i].TangentIn);
                        Vector3 presetTanIn = (Quaternion.Inverse(orientation) * worldTanIn) / motionScale;

                        Vector3 worldTanOut = container.transform.TransformVector((Vector3)spline[i].TangentOut);
                        Vector3 presetTanOut = (Quaternion.Inverse(orientation) * worldTanOut) / motionScale;

                        TangentMode mode = spline.GetTangentMode(i);
                        Quaternion worldRot = container.transform.rotation * (Quaternion)spline[i].Rotation;
                        Quaternion presetRot = Quaternion.Inverse(orientation) * worldRot;

                        knots[i] = new MotionKnot(pi, presetTanIn, presetTanOut, mode, presetRot);
                    }
                }
            }
            else
            {
                // Fixed Shot
                Vector3 camWorld = shot.CinemachineCamera != null ? shot.CinemachineCamera.transform.position : shot.transform.position;
                Vector3 relWorld = camWorld - targetPos;
                Vector3 scaledP0 = Quaternion.Inverse(orientation) * relWorld;
                Vector3 p0 = new Vector3(scaledP0.x / distanceScale, scaledP0.y, scaledP0.z / distanceScale);

                Quaternion camRot = shot.CinemachineCamera != null ? shot.CinemachineCamera.transform.rotation : shot.transform.rotation;
                Quaternion presetRot = Quaternion.Inverse(orientation) * camRot;

                knots = new MotionKnot[]
                {
                    new MotionKnot(p0, Vector3.zero, Vector3.zero, TangentMode.Linear, presetRot)
                };
            }

            // 2. Aim Offset
            Vector3 presetAimOffset = applied.AimOffset;
            if (shot.AimProxy != null)
            {
                Vector3 aimWorld = shot.AimProxy.position;
                Vector3 relAim = aimWorld - targetPos;
                Vector3 aboveHeight = relAim - Vector3.up * targetHeight;
                presetAimOffset = Quaternion.Inverse(orientation) * aboveHeight;
            }

            // 3. Screen Position & Lens
            Vector2 screenPos = applied.ScreenPosition;
            if (shot.RotationComposer != null)
            {
                screenPos = shot.RotationComposer.Composition.ScreenPosition;
            }

            float fov = applied.FieldOfView;
            if (shot.CinemachineCamera != null)
            {
                fov = shot.CinemachineCamera.Lens.FieldOfView;
            }

            // 4. Asset Path
            if (string.IsNullOrEmpty(targetAssetPath))
            {
                if (!Directory.Exists(DefaultPresetFolder))
                {
                    Directory.CreateDirectory(DefaultPresetFolder);
                }

                string safeShotName = string.IsNullOrEmpty(shot.ShotName) ? "NewPreset" : shot.ShotName.Replace(" ", "_");
                targetAssetPath = $"{DefaultPresetFolder}/{safeShotName}_Preset.asset";
            }

            targetAssetPath = AssetDatabase.GenerateUniqueAssetPath(targetAssetPath);

            // 5. 確認ダイアログ (Source Shotと保存先パスを表示)
            if (showConfirmationDialog)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Bake Preset (プリセット保存確認)",
                    $"Source Shot: {shot.ShotName}\n保存先パス: {targetAssetPath}\n\nこのShotから新規Motion Presetを生成して保存しますか？",
                    "保存 (Save)",
                    "キャンセル (Cancel)"
                );

                if (!proceed)
                {
                    return null;
                }
            }

            // 6. Presetインスタンス作成と保存
            var preset = ScriptableObject.CreateInstance<VLiveCameraMotionPreset>();
            preset.Initialize(
                shot.ShotName + " (Baked)",
                shot.Type,
                applied.Family,
                ShotSize.Medium,
                ShotEnergy.Normal,
                $"Baked from scene Shot '{shot.ShotName}'",
                shot.AppliedPreset != null ? shot.AppliedPreset.RigProfile : null,
                knots,
                isClosed,
                refSplineLength,
                applied.ClipDuration,
                applied.ProgressCurve,
                applied.ScaleMode,
                presetAimOffset,
                screenPos,
                fov,
                applied.EntryMode,
                applied.InTime,
                applied.OutTime,
                applied.ExitBehavior,
                applied.StartDistance,
                applied.EndDistance
            );

            AssetDatabase.CreateAsset(preset, targetAssetPath);
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssetIfDirty(preset);

            Debug.Log($"[VLiveCameraPresetBaker] Shot '{shot.ShotName}' から新しい Motion Preset を保存しました: {targetAssetPath}");
            return preset;
        }
    }
}
