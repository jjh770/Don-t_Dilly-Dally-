using UnityEngine;
using UnityEngine.UI;

public class AttendanceTestController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AttendanceRewardSO _rewardTable;

    private Canvas _canvas;
    private Text _statusText;
    private AttendanceRecord _testRecord;
    private AttendanceDomainService _domainService;

    private void Start()
    {
        if (_rewardTable == null)
        {
            Debug.LogError("[AttendanceTest] RewardTable이 할당되지 않았습니다.");
            return;
        }

        _domainService = new AttendanceDomainService(_rewardTable);
        _testRecord = new AttendanceRecord();

        CreateDebugUI();
        UpdateStatusText();
    }

    private void CreateDebugUI()
    {
        // Canvas
        var canvasObj = new GameObject("AttendanceTestCanvas");
        canvasObj.transform.SetParent(transform);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Panel
        var panel = CreatePanel(canvasObj.transform);

        // Title
        CreateText(panel.transform, "[ Attendance Test ]", 20, new Vector2(0, 80));

        // Status Text
        _statusText = CreateText(panel.transform, "", 16, new Vector2(0, 40));

        // Check Button
        CreateButton(panel.transform, "Check Attendance", new Vector2(0, -10), OnCheckButtonClicked);

        // Reset Button
        CreateButton(panel.transform, "Reset", new Vector2(0, -60), OnResetButtonClicked);

        // Close Button
        CreateButton(panel.transform, "Close", new Vector2(0, -110), () => _canvas.gameObject.SetActive(false));
    }

    private GameObject CreatePanel(Transform parent)
    {
        var panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(parent, false);

        var rect = panelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(250, 250);

        var image = panelObj.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.85f);

        return panelObj;
    }

    private Text CreateText(Transform parent, string text, int fontSize, Vector2 position)
    {
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        var rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(230, 50);

        var uiText = textObj.AddComponent<Text>();
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.color = Color.white;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return uiText;
    }

    private Button CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObj = new GameObject(label + "Button");
        buttonObj.transform.SetParent(parent, false);

        var rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(200, 40);

        var image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        var button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        // Button Text
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var uiText = textObj.AddComponent<Text>();
        uiText.text = label;
        uiText.fontSize = 16;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.color = Color.white;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return button;
    }

    private void OnCheckButtonClicked()
    {
        if (_domainService.RewardComplete(_testRecord))
        {
            Debug.Log("[AttendanceTest] 모든 보상 수령 완료");
            UpdateStatusText("모든 보상 수령 완료!");
            return;
        }

        ForceCheckAttendance();
    }

    private void ForceCheckAttendance()
    {
        _testRecord = new AttendanceRecord(
            _testRecord.TotalDays,
            "1900-01-01"
        );

        var reward = _domainService.CheckAndGetReward(_testRecord);

        Debug.Log($"[AttendanceTest] {_testRecord.TotalDays}일차 출석 - 보상: {reward.ItemId}");

        if (!string.IsNullOrEmpty(reward.ItemId) && CustomizingManager.Instance != null)
        {
            CustomizingManager.Instance.UnlockItem(reward.ItemId);
            Debug.Log($"[AttendanceTest] 아이템 해금: {reward.ItemId}");
        }

        UpdateStatusText($"{_testRecord.TotalDays}일차 출석!\n보상: {reward.ItemId}");
    }

    private void OnResetButtonClicked()
    {
        _testRecord = new AttendanceRecord();
        Debug.Log("[AttendanceTest] 출석 기록 초기화");
        UpdateStatusText("초기화 완료!\n현재: 0일차");
    }

    private void UpdateStatusText(string message = null)
    {
        if (_statusText == null) return;
        _statusText.text = message ?? $"현재: {_testRecord.TotalDays}일차";
    }
}
