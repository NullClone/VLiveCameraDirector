namespace VLiveKit.Camera.Editor
{
    /// <summary>
    /// Motion診断のカテゴリ、重要度、説明を保持します。
    /// </summary>
    public readonly struct VLiveCameraDiagnosticMessage
    {
        // Properties

        public VLiveCameraDiagnosticSeverity Severity { get; }
        public string Category { get; }
        public string Message { get; }


        // Methods

        /// <summary>
        /// 診断メッセージを作成します。
        /// </summary>
        public VLiveCameraDiagnosticMessage(VLiveCameraDiagnosticSeverity severity, string category, string message)
        {
            Severity = severity;
            Category = category;
            Message = message;
        }
    }
}
