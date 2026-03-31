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
    [SerializeField] private Vector3 _mainLightEulerAngles = new Vector3(25f, -35f, 0f);
    [SerializeField] private Color _mainLightColor = Color.white;
    [SerializeField, Min(0f)] private float _mainLightIntensity = 1.2f;

    public string FaceAnchorName => _faceAnchorName;
    public Vector3 CameraOffset => _cameraOffset;
    public Vector3 CameraEulerAngles => _cameraEulerAngles;
    public Vector3 CharacterEulerAngles => _characterEulerAngles;
    public float OrthographicSize => _orthographicSize;
    public Vector3 MainLightEulerAngles => _mainLightEulerAngles;
    public Color MainLightColor => _mainLightColor;
    public float MainLightIntensity => _mainLightIntensity;
}
