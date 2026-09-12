using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// Motion Presetの3D KnotをScene上のSplineContainerへ具体化します。
    /// </summary>
    public static class VLiveCameraSplineBuilder
    {
        // Methods

        /// <summary>
        /// Presetの接線、接線モード、回転を保持しながらRig基準空間へSplineを再構築します。
        /// </summary>
        public static bool Populate(SplineContainer splineContainer, VLiveCameraRig rig, VLiveCameraMotionPreset preset)
        {
            if (splineContainer == null || rig == null || preset == null)
            {
                return false;
            }

            Spline spline = splineContainer.Spline;
            spline.Clear();

            MotionKnot[] knots = preset.Knots;
            if (knots == null || knots.Length == 0)
            {
                Debug.LogError($"[VLiveCameraSplineBuilder] Preset '{preset.DisplayName}' has no knots. Spline generation was skipped.");
                EditorUtility.SetDirty(splineContainer);
                return false;
            }

            spline.Closed = preset.IsClosed;

            Vector3 firstPosition = knots[0].Position;
            Vector3 scaledFirstPosition = VLiveCameraMotionSpace.ScaleStartPosition(firstPosition, rig.DistanceScale);
            Quaternion orientation = rig.GetReferenceOrientation();
            Vector3 targetPosition = rig.PerformerTarget != null ? rig.PerformerTarget.position : Vector3.zero;

            for (int i = 0; i < knots.Length; i++)
            {
                MotionKnot source = knots[i];
                Vector3 scaledPosition = scaledFirstPosition;
                if (i > 0)
                {
                    Vector3 delta = source.Position - firstPosition;
                    scaledPosition += VLiveCameraMotionSpace.ScaleMotion(delta, rig.MotionScale, rig.VerticalMotionScale);
                }

                Vector3 worldPosition = targetPosition + orientation * scaledPosition;
                Vector3 localPosition = splineContainer.transform.InverseTransformPoint(worldPosition);
                Quaternion worldRotation = orientation * source.Rotation;
                Quaternion localRotation = Quaternion.Inverse(splineContainer.transform.rotation) * worldRotation;

                Vector3 tangentIn = TransformTangentToSpline(
                    source.TangentIn,
                    source.Rotation,
                    orientation,
                    splineContainer.transform,
                    localRotation,
                    rig.MotionScale,
                    rig.VerticalMotionScale);
                Vector3 tangentOut = TransformTangentToSpline(
                    source.TangentOut,
                    source.Rotation,
                    orientation,
                    splineContainer.transform,
                    localRotation,
                    rig.MotionScale,
                    rig.VerticalMotionScale);
                var knot = new BezierKnot((float3)localPosition, (float3)tangentIn, (float3)tangentOut, (quaternion)localRotation);

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

            EditorUtility.SetDirty(splineContainer);
            return true;
        }

        private static Vector3 TransformTangentToSpline(
            Vector3 tangent,
            Quaternion sourceRotation,
            Quaternion rigOrientation,
            Transform splineTransform,
            Quaternion localKnotRotation,
            float horizontalScale,
            float verticalScale)
        {
            Vector3 presetTangent = sourceRotation * tangent;
            Vector3 scaledTangent = VLiveCameraMotionSpace.ScaleMotion(
                presetTangent,
                horizontalScale,
                verticalScale);
            Vector3 worldTangent = rigOrientation * scaledTangent;
            Vector3 splineTangent = splineTransform.worldToLocalMatrix.MultiplyVector(worldTangent);
            return Quaternion.Inverse(localKnotRotation) * splineTangent;
        }
    }
}
