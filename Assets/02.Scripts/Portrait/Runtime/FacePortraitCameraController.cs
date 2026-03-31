using UnityEngine;

public static class FacePortraitCameraController
{
    public static void Apply(Camera captureCamera, Transform faceAnchor, FacePortraitProfile profile)
    {
        if (captureCamera == null || faceAnchor == null || profile == null)
        {
            return;
        }

        captureCamera.orthographic = true;
        captureCamera.orthographicSize = profile.OrthographicSize;
        captureCamera.transform.position = faceAnchor.position + profile.CameraOffset;
        captureCamera.transform.rotation = Quaternion.Euler(profile.CameraEulerAngles);
    }
}
