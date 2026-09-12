using System.Collections.Generic;

namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// Motion診断の測定値とメッセージを保持します。
    /// </summary>
    public class VLiveCameraMotionValidationReport
    {
        // Properties

        public List<VLiveCameraDiagnosticMessage> Messages { get; } = new List<VLiveCameraDiagnosticMessage>();
        public float Duration { get; set; }
        public float SplineLength { get; set; }
        public float MaxSpeed { get; set; }
        public float MaxAcceleration { get; set; }
        public float MaxJerk { get; set; }
        public float MinFieldOfView { get; set; }
        public float MaxFieldOfView { get; set; }
        public bool HasErrors { get; set; }
        public bool HasWarnings { get; set; }
    }
}
