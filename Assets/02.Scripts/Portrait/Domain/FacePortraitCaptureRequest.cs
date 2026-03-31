public readonly struct FacePortraitCaptureRequest
{
    public PlayerAppearanceSnapshot Snapshot { get; }
    public FacePortraitProfile Profile { get; }
    public int Resolution { get; }
    public int CaptureLayer { get; }

    public FacePortraitCaptureRequest(PlayerAppearanceSnapshot snapshot, FacePortraitProfile profile, int resolution, int captureLayer)
    {
        Snapshot = snapshot;
        Profile = profile;
        Resolution = resolution;
        CaptureLayer = captureLayer;
    }
}
