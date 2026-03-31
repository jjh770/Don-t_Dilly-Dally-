using System;
using UnityEngine;

[Serializable]
public class FacePortraitProfile
{
    [SerializeField] private string _faceAnchorName = "Head";
    [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 0.25f, -0.6f);
    [SerializeField] private Vector3 _cameraEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 _characterEulerAngles = new Vector3(0f, 180f, 0f);
    [SerializeField, Min(0.01f)] private float _orthographicSize = 0.25f;

    public string FaceAnchorName => _faceAnchorName;
    public Vector3 CameraOffset => _cameraOffset;
    public Vector3 CameraEulerAngles => _cameraEulerAngles;
    public Vector3 CharacterEulerAngles => _characterEulerAngles;
    public float OrthographicSize => _orthographicSize;
}
