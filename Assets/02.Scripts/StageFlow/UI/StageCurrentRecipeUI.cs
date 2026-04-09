using DG.Tweening;
using DontDillyDally.Data;
using DontDillyDally.StageFlow;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class StageCurrentRecipeUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private RectTransform _panelRoot;
    [SerializeField] private RectTransform _recipeListRoot;

    [Header("레시피 항목 프리팹")]
    [SerializeField] private RecipeItemEntry _recipeItemPrefab;

    [Header("아이콘")]
    [SerializeField] private MaterialIconTable _iconTable;

    [Header("색상")]
    [SerializeField] private Color _currentTextColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color _pendingTextColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);
    [SerializeField] private Color _currentBgColor = new Color(1f, 0.95f, 0.7f, 0.95f);
    [SerializeField] private Color _defaultBgColor = new Color(1f, 1f, 1f, 0.6f);

    [Header("애니메이션 - 완료")]
    [SerializeField] private float _completeFlyUpDistance = 150f;
    [SerializeField] private float _completeFlyUpDuration = 0.4f;
    [SerializeField] private float _slideLeftDelay = 0.15f;
    [SerializeField] private Ease _flyUpEase = Ease.OutBack;

    [Header("애니메이션 - 등장")]
    [SerializeField] private float _enterSlideDistance = 80f;
    [SerializeField] private float _enterDuration = 0.35f;
    [SerializeField] private float _enterStagger = 0.08f;
    [SerializeField] private Ease _enterEase = Ease.OutCubic;

    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private readonly List<RecipeItemEntry> _visibleItems = new List<RecipeItemEntry>();

    private StageFlowManager _stageFlowManager;
    private int _lastRecipeIndex = -1;
    private bool _isAnimating;
    private bool _isStageDataBound;

    private void Start()
    {
        if (!TryBind())
        {
            StageFlowBootstrapper.StageFlowReady += HandleStageFlowReady;
        }

        RefreshUi(false);
    }

    private void OnDestroy()
    {
        StageFlowBootstrapper.StageFlowReady -= HandleStageFlowReady;

        if (_stageFlowManager != null && _isStageDataBound)
        {
            _stageFlowManager.OnStageDataChanged -= HandleStageDataChanged;
        }

        _disposables.Dispose();
        DOTween.Kill(this);
    }

    private bool TryBind()
    {
        if (_stageFlowManager != null ||
            StageFlowBootstrapper.Instance == null ||
            !StageFlowBootstrapper.Instance.IsStageFlowReady ||
            StageFlowManager.Instance == null ||
            !StageFlowManager.Instance.IsInitialized)
        {
            return false;
        }

        _stageFlowManager = StageFlowManager.Instance;
        _stageFlowManager.OnStageDataChanged += HandleStageDataChanged;
        _isStageDataBound = true;

        _stageFlowManager.CurrentPhase
            .Subscribe(_ => RefreshUi(false))
            .AddTo(_disposables);

        _stageFlowManager.CurrentPatientIndex
            .Subscribe(_ => OnPatientChanged())
            .AddTo(_disposables);

        _stageFlowManager.CurrentRecipeIndex
            .Subscribe(newIndex => OnRecipeIndexChanged(newIndex))
            .AddTo(_disposables);

        RefreshUi(false);
        StageFlowBootstrapper.StageFlowReady -= HandleStageFlowReady;
        return true;
    }

    private void HandleStageFlowReady()
    {
        TryBind();
    }

    private void HandleStageDataChanged(StageRuntimeData _)
    {
        _lastRecipeIndex = -1;
        ClearAllItems();
        RefreshUi(false);
    }

    // ================================================================
    //  이벤트 핸들링
    // ================================================================

    private void OnPatientChanged()
    {
        _lastRecipeIndex = -1;
        ClearAllItems();
        RefreshUi(false);
    }

    private void OnRecipeIndexChanged(int newIndex)
    {
        if (_isAnimating)
        {
            return;
        }

        if (_lastRecipeIndex < 0)
        {
            _lastRecipeIndex = newIndex;
            RefreshUi(false);
            return;
        }

        if (newIndex > _lastRecipeIndex && _visibleItems.Count > 0)
        {
            _lastRecipeIndex = newIndex;
            AnimateRecipeComplete();
        }
        else
        {
            _lastRecipeIndex = newIndex;
            RefreshUi(false);
        }
    }

    // ================================================================
    //  UI 갱신
    // ================================================================

    private void RefreshUi(bool keepExisting)
    {
        if (_panelRoot == null)
        {
            return;
        }

        if (_stageFlowManager == null ||
            !_stageFlowManager.TryGetCurrentDisease(out DiseaseData disease))
        {
            _panelRoot.gameObject.SetActive(false);
            return;
        }

        EStagePhase phase = _stageFlowManager.CurrentPhase.Value;
        bool shouldShow =
            phase == EStagePhase.Playing ||
            phase == EStagePhase.PatientTransition;

        _panelRoot.gameObject.SetActive(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        if (!keepExisting)
        {
            RebuildRecipeList(disease);
        }
    }

    /// <summary>
    /// currentRecipeIndex부터 남은 레시피만 가로 리스트에 표시합니다.
    /// RecipeListRoot에 HorizontalLayoutGroup을 사용하세요.
    /// </summary>
    private void RebuildRecipeList(DiseaseData disease)
    {
        if (_recipeListRoot == null || _recipeItemPrefab == null || disease.Recipes == null)
        {
            return;
        }

        ClearAllItems();

        int currentRecipeIndex = _stageFlowManager.CurrentRecipeIndex.Value;
        int recipeCount = disease.Recipes.Count;

        for (int i = currentRecipeIndex; i < recipeCount; i++)
        {
            RecipeItemEntry item = Instantiate(_recipeItemPrefab, _recipeListRoot);
            item.gameObject.name = $"RecipeItem_{i}";

            RecipeData recipe = disease.Recipes[i];
            bool isCurrent = i == currentRecipeIndex;

            string recipeName = string.IsNullOrWhiteSpace(recipe.DisplayName)
                ? recipe.RecipeId
                : recipe.DisplayName;

            int displayOrder = i - currentRecipeIndex + 1;

            item.SetData(
                $"{displayOrder}. {recipeName}",
                recipe.RequiredMaterials,
                _iconTable,
                isCurrent ? _currentTextColor : _pendingTextColor,
                isCurrent ? _currentBgColor : _defaultBgColor,
                isCurrent,
                recipe.RequiresSterilizedTray);

            _visibleItems.Add(item);

            AnimateItemEnter(item, i - currentRecipeIndex);
        }

        RefreshLayoutImmediate();
    }

    /// <summary>
    /// 등장: 아래에서 위로 슬라이드인 + 페이드인 (순차 딜레이)
    /// </summary>
    private void AnimateItemEnter(RecipeItemEntry item, int order)
    {
        RectTransform rt = item.GetComponent<RectTransform>();
        CanvasGroup cg = item.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = item.gameObject.AddComponent<CanvasGroup>();
        }

        Vector2 originalPos = rt.anchoredPosition;
        rt.anchoredPosition = new Vector2(originalPos.x, originalPos.y - _enterSlideDistance);
        cg.alpha = 0f;

        float delay = order * _enterStagger;

        rt.DOAnchorPosY(originalPos.y, _enterDuration)
            .SetEase(_enterEase)
            .SetDelay(delay)
            .SetId(this);

        cg.DOFade(1f, _enterDuration)
            .SetDelay(delay)
            .SetId(this);
    }

    // ================================================================
    //  완료 애니메이션
    // ================================================================

    /// <summary>
    /// 완료: 위로 Y+150 날아가며 페이드아웃 → 삭제 → 남은 항목이 왼쪽으로 밀려 채움
    /// HorizontalLayoutGroup이 자동 재배치합니다.
    /// </summary>
    private void AnimateRecipeComplete()
    {
        if (_visibleItems.Count == 0)
        {
            return;
        }

        _isAnimating = true;

        RecipeItemEntry completedItem = _visibleItems[0];
        _visibleItems.RemoveAt(0);

        RectTransform completedRt = completedItem.GetComponent<RectTransform>();
        CanvasGroup cg = completedItem.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = completedItem.gameObject.AddComponent<CanvasGroup>();
        }

        Sequence seq = DOTween.Sequence().SetId(this);

        // 위로 날아가며 페이드 아웃
        seq.Append(
            completedRt.DOAnchorPosY(
                completedRt.anchoredPosition.y + _completeFlyUpDistance,
                _completeFlyUpDuration)
            .SetEase(_flyUpEase));

        seq.Join(
            cg.DOFade(0f, _completeFlyUpDuration));

        // 삭제 + 남은 항목 갱신 (HorizontalLayoutGroup이 왼쪽으로 재배치)
        seq.AppendCallback(() =>
        {
            RemoveItemFromLayout(completedItem);
            UpdateRemainingItems();
            RefreshLayoutImmediate();
            Destroy(completedItem.gameObject);
        });

        seq.AppendInterval(_slideLeftDelay);

        seq.AppendCallback(() =>
        {
            _isAnimating = false;
        });

        seq.Play();
    }

    private void UpdateRemainingItems()
    {
        if (_stageFlowManager == null ||
            !_stageFlowManager.TryGetCurrentDisease(out DiseaseData disease))
        {
            return;
        }

        int currentRecipeIndex = _stageFlowManager.CurrentRecipeIndex.Value;

        for (int i = 0; i < _visibleItems.Count; i++)
        {
            int recipeDataIndex = currentRecipeIndex + i;
            if (recipeDataIndex >= disease.Recipes.Count)
            {
                _visibleItems[i].gameObject.SetActive(false);
                continue;
            }

            RecipeData recipe = disease.Recipes[recipeDataIndex];
            bool isCurrent = i == 0;

            string recipeName = string.IsNullOrWhiteSpace(recipe.DisplayName)
                ? recipe.RecipeId
                : recipe.DisplayName;


            _visibleItems[i].SetData(
                $"{i + 1}. {recipeName}",
                recipe.RequiredMaterials,
                _iconTable,
                isCurrent ? _currentTextColor : _pendingTextColor,
                isCurrent ? _currentBgColor : _defaultBgColor,
                isCurrent,
                recipe.RequiresSterilizedTray);
        }

        RefreshLayoutImmediate();
    }

    // ================================================================
    //  정리
    // ================================================================

    private void ClearAllItems()
    {
        for (int i = 0; i < _visibleItems.Count; i++)
        {
            if (_visibleItems[i] != null)
            {
                RemoveItemFromLayout(_visibleItems[i]);
                Destroy(_visibleItems[i].gameObject);
            }
        }

        _visibleItems.Clear();
        RefreshLayoutImmediate();
    }

    private void RemoveItemFromLayout(RecipeItemEntry item)
    {
        if (item == null)
        {
            return;
        }

        LayoutElement layoutElement = item.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

        item.gameObject.SetActive(false);
    }

    private void RefreshLayoutImmediate()
    {
        if (_recipeListRoot == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_recipeListRoot);

        RectTransform parent = _recipeListRoot.parent as RectTransform;
        if (parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }
    }
}
