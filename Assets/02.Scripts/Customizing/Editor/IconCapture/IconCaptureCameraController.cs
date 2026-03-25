using UnityEngine;

/// <summary>
/// 아이콘 캡처 전용 카메라 컨트롤러
/// - 카메라 위치/각도 세팅
/// - RenderTexture 연결
/// - 투명 배경 세팅
/// - 아이템별 카메라 오프셋 지원
/// </summary>
public class IconCaptureCameraController
{
    // ========================================
    // 필드
    // ========================================

    private Camera _captureCamera;
    private RenderTexture _renderTexture;
    private readonly int _resolution;

    // 기본 카메라 세팅값
    private readonly Vector3 _defaultCameraPosition = new Vector3(0, 1.0f, 2.5f);
    private readonly Vector3 _defaultCameraRotation = new Vector3(10f, 180f, 0f);
    private readonly float _defaultOrthographicSize = 0.8f;

    // ========================================
    // 프로퍼티
    // ========================================

    public Camera CaptureCamera => _captureCamera;
    public RenderTexture RenderTexture => _renderTexture;

    // ========================================
    // 생성자
    // ========================================

    /// <summary>
    /// 카메라 컨트롤러 생성
    /// </summary>
    /// <param name="resolution">아이콘 해상도 (정사각형)</param>
    public IconCaptureCameraController(int resolution)
    {
        _resolution = resolution;
    }

    // ========================================
    // 공개 메서드
    // ========================================

    /// <summary>
    /// 캡처용 카메라와 RenderTexture 초기화
    /// </summary>
    public void Initialize()
    {
        CreateRenderTexture();
        CreateCaptureCamera();
        ConfigureCameraForTransparentBackground();
    }

    /// <summary>
    /// 특정 타겟을 바라보도록 카메라 조정
    /// </summary>
    /// <param name="target">캡처 대상 오브젝트</param>
    /// <param name="customizingType">커스터마이징 타입 (카메라 각도 조정용)</param>
    public void FocusOnTarget(GameObject target, CustomizingType customizingType)
    {
        if (target == null)
        {
            Debug.LogWarning("[IconCaptureCameraController] 타겟이 null입니다.");
            return;
        }

        // 바운딩 박스 계산
        Bounds bounds = CalculateBounds(target);
        Vector3 targetCenter = bounds.size != Vector3.zero ? bounds.center : target.transform.position;

        // 타입별 카메라 오프셋 적용
        var offset = GetCameraOffsetForType(customizingType);

        // 카메라 위치 = 타겟 중심 + 오프셋
        _captureCamera.transform.position = targetCenter + offset.cameraOffset;

        // 타겟 중심을 바라봄
        _captureCamera.transform.LookAt(targetCenter);

        // 바운딩 박스 크기에 맞게 Orthographic 크기 조정
        if (bounds.size != Vector3.zero)
        {
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y);
            _captureCamera.orthographicSize = maxSize * offset.sizeMultiplier;
        }
        else
        {
            _captureCamera.orthographicSize = offset.defaultOrthoSize;
        }

        Debug.Log($"[IconCaptureCameraController] 포커스: {target.name}, " +
                  $"Center: {targetCenter}, OrthoSize: {_captureCamera.orthographicSize:F2}");
    }

    /// <summary>
    /// 오브젝트의 전체 바운딩 박스 계산
    /// </summary>
    private Bounds CalculateBounds(GameObject target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return new Bounds(target.transform.position, Vector3.zero);
        }

        Bounds combinedBounds = new Bounds();
        bool initialized = false;

        foreach (var renderer in renderers)
        {
            if (!renderer.enabled)
                continue;

            if (renderer is ParticleSystemRenderer)
                continue;

            if (!initialized)
            {
                combinedBounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        return combinedBounds;
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Cleanup()
    {
        if (_captureCamera != null)
        {
            Object.DestroyImmediate(_captureCamera.gameObject);
            _captureCamera = null;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Object.DestroyImmediate(_renderTexture);
            _renderTexture = null;
        }
    }

    // ========================================
    // 비공개 메서드
    // ========================================

    /// <summary>
    /// RenderTexture 생성
    /// </summary>
    private void CreateRenderTexture()
    {
        _renderTexture = new RenderTexture(_resolution, _resolution, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _renderTexture.Create();

        Debug.Log($"[IconCaptureCameraController] RenderTexture 생성: {_resolution}x{_resolution}");
    }

    /// <summary>
    /// 캡처 전용 카메라 생성
    /// </summary>
    private void CreateCaptureCamera()
    {
        var existingCamera = GameObject.Find("IconCaptureCamera");
        if (existingCamera != null)
        {
            Object.DestroyImmediate(existingCamera);
        }

        var cameraObject = new GameObject("IconCaptureCamera");
        _captureCamera = cameraObject.AddComponent<Camera>();

        _captureCamera.orthographic = true;
        _captureCamera.orthographicSize = _defaultOrthographicSize;
        _captureCamera.nearClipPlane = 0.01f;
        _captureCamera.farClipPlane = 100f;
        _captureCamera.targetTexture = _renderTexture;

        _captureCamera.transform.position = _defaultCameraPosition;
        _captureCamera.transform.eulerAngles = _defaultCameraRotation;

        _captureCamera.enabled = false;

        Debug.Log("[IconCaptureCameraController] 캡처 카메라 생성 완료");
    }

    /// <summary>
    /// 투명 배경을 위한 카메라 설정
    /// </summary>
    private void ConfigureCameraForTransparentBackground()
    {
        _captureCamera.clearFlags = CameraClearFlags.SolidColor;
        _captureCamera.backgroundColor = new Color(0, 0, 0, 0);
    }

    /// <summary>
    /// 커스터마이징 타입별 카메라 오프셋 반환
    /// </summary>
    private CameraOffset GetCameraOffsetForType(CustomizingType type)
    {
        return type switch
        {
            // 모자 - 위에서 비스듬히
            CustomizingType.Hat => new CameraOffset
            {
                cameraOffset = new Vector3(0, 0.3f, 1.2f),
                sizeMultiplier = 0.7f,
                defaultOrthoSize = 0.4f
            },

            // 헤어스타일 - 정면 약간 위
            CustomizingType.HairStyle => new CameraOffset
            {
                cameraOffset = new Vector3(0, 0.2f, 1.2f),
                sizeMultiplier = 0.7f,
                defaultOrthoSize = 0.5f
            },

            // 얼굴 - 정면
            CustomizingType.Faces => new CameraOffset
            {
                cameraOffset = new Vector3(0, 0, 1.0f),
                sizeMultiplier = 0.8f,
                defaultOrthoSize = 0.3f
            },

            // 얼굴 악세사리 - 정면
            CustomizingType.FaceAccessory => new CameraOffset
            {
                cameraOffset = new Vector3(0, 0, 1.0f),
                sizeMultiplier = 0.8f,
                defaultOrthoSize = 0.3f
            },

            // 안경 - 정면
            CustomizingType.Glasses => new CameraOffset
            {
                cameraOffset = new Vector3(0, 0, 1.0f),
                sizeMultiplier = 0.8f,
                defaultOrthoSize = 0.3f
            },

            // 신발 - 위에서 비스듬히
            CustomizingType.Shoes => new CameraOffset
            {
                cameraOffset = new Vector3(0, 0.5f, 1.0f),
                sizeMultiplier = 0.8f,
                defaultOrthoSize = 0.3f
            },

            // 코스튬 - 전신
            CustomizingType.Costumes => new CameraOffset
            {
                cameraOffset = new Vector3(0, 0.2f, 2.0f),
                sizeMultiplier = 0.6f,
                defaultOrthoSize = 1.0f
            },

            // 스킨 컬러 (Body) - 전신
            CustomizingType.SkinColor => new CameraOffset
            {
                cameraOffset = new Vector3(0, 0.2f, 2.0f),
                sizeMultiplier = 0.6f,
                defaultOrthoSize = 1.0f
            },

            // 기본값
            _ => new CameraOffset
            {
                cameraOffset = new Vector3(0, 0, 1.5f),
                sizeMultiplier = 0.7f,
                defaultOrthoSize = 0.5f
            }
        };
    }

    // ========================================
    // 내부 구조체
    // ========================================

    private struct CameraOffset
    {
        public Vector3 cameraOffset;     // 타겟 중심에서 카메라까지의 오프셋
        public float sizeMultiplier;     // 바운딩 박스 크기 대비 OrthoSize 배율
        public float defaultOrthoSize;   // 바운딩 박스가 없을 때 기본값
    }
}
