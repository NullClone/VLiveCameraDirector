namespace VLiveKit.Camera
{
    /// <summary>
    /// カメラ配置の正面方向を決める基準。
    /// </summary>
    public enum ForwardReferenceMode
    {
        ReferenceForward,
        WorldPlusZ,
        WorldMinusZ,
        CustomReference
    }
}
