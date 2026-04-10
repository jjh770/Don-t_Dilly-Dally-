using DontDillyDally.Data;
using DontDillyDally.StageFlow;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace DontDillyDally.UI
{
    /// <summary>
    /// 레시피 리스트 전체 UI를 관리합니다.
    /// RecipeListViewport 아래에 레시피 UI를 동적 생성합니다.
    /// </summary>
    public class UI_Recipe : MonoBehaviour
    {
        [Header("UI 구조")]
        [SerializeField] private RectTransform _recipeListScrollView;
        [SerializeField] private RectTransform _recipeListViewport;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("프리팹")]
        [SerializeField] private UI_RecipeEntry _recipePrefab;

        [Header("아이콘 테이블")]
        [SerializeField] private MaterialIconTable _iconTable;

        [Header("레이아웃")]
        [SerializeField] private float _recipeSpacing = 10f;
        [SerializeField] private RectOffset _padding = new RectOffset(10, 10, 10, 10);

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly List<UI_RecipeEntry> _recipeEntries = new List<UI_RecipeEntry>();

        private StageFlowManager _stageFlowManager;
        private bool _isStageDataBound;
        private int _currentRecipeIndex = -1;

        public IReadOnlyList<UI_RecipeEntry> RecipeEntries => _recipeEntries;

        private void Start()
        {
            SetupLayout();
            TryBind();
            RefreshUI();
        }

        private void Update()
        {
            if (_stageFlowManager == null)
            {
                TryBind();
                RefreshUI();
            }
        }

        private void OnDestroy()
        {
            if (_stageFlowManager != null && _isStageDataBound)
            {
                _stageFlowManager.OnStageDataChanged -= HandleStageDataChanged;
            }

            _disposables.Dispose();
        }

        private void SetupLayout()
        {
            if (_recipeListViewport == null)
            {
                return;
            }

            VerticalLayoutGroup vlg = _recipeListViewport.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = _recipeListViewport.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.spacing = _recipeSpacing;
            vlg.padding = _padding;

            ContentSizeFitter csf = _recipeListViewport.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = _recipeListViewport.gameObject.AddComponent<ContentSizeFitter>();
            }

            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private void TryBind()
        {
            if (_stageFlowManager != null || StageFlowManager.Instance == null || !StageFlowManager.Instance.IsInitialized)
            {
                return;
            }

            _stageFlowManager = StageFlowManager.Instance;
            _stageFlowManager.OnStageDataChanged += HandleStageDataChanged;
            _isStageDataBound = true;

            _stageFlowManager.CurrentPhase
                .Subscribe(_ => RefreshUI())
                .AddTo(_disposables);

            _stageFlowManager.CurrentPatientIndex
                .Subscribe(_ => OnPatientChanged())
                .AddTo(_disposables);

            _stageFlowManager.CurrentRecipeIndex
                .Subscribe(index => OnRecipeIndexChanged(index))
                .AddTo(_disposables);

            RefreshUI();
        }

        private void HandleStageDataChanged(StageRuntimeData _)
        {
            _currentRecipeIndex = -1;
            ClearAllRecipes();
            RefreshUI();
        }

        private void OnPatientChanged()
        {
            _currentRecipeIndex = -1;
            ClearAllRecipes();
            RefreshUI();
        }

        private void OnRecipeIndexChanged(int newIndex)
        {
            if (newIndex == _currentRecipeIndex)
            {
                return;
            }

            int previousIndex = _currentRecipeIndex;
            _currentRecipeIndex = newIndex;

            if (previousIndex >= 0 && previousIndex < _recipeEntries.Count)
            {
                _recipeEntries[previousIndex].MarkComplete();
            }

            if (newIndex >= 0 && newIndex < _recipeEntries.Count)
            {
                _recipeEntries[newIndex].SetCurrent(true);
                ScrollToRecipe(newIndex);
            }

            UpdateCurrentIndicators();
        }

        private void RefreshUI()
        {
            if (_recipeListScrollView == null)
            {
                return;
            }

            if (_stageFlowManager == null ||
                !_stageFlowManager.TryGetCurrentDisease(out DiseaseData disease))
            {
                _recipeListScrollView.gameObject.SetActive(false);
                return;
            }

            EStagePhase phase = _stageFlowManager.CurrentPhase.Value;
            bool shouldShow = phase == EStagePhase.Playing || phase == EStagePhase.PatientTransition;

            _recipeListScrollView.gameObject.SetActive(shouldShow);

            if (!shouldShow)
            {
                return;
            }

            if (_recipeEntries.Count == 0)
            {
                RebuildRecipeList(disease);
            }
        }

        private void RebuildRecipeList(DiseaseData disease)
        {
            if (_recipeListViewport == null || _recipePrefab == null || disease.Recipes == null)
            {
                return;
            }

            ClearAllRecipes();

            int currentIndex = _stageFlowManager.CurrentRecipeIndex.Value;
            _currentRecipeIndex = currentIndex;

            for (int i = 0; i < disease.Recipes.Count; i++)
            {
                RecipeData recipe = disease.Recipes[i];
                bool isCurrent = i == currentIndex;

                UI_RecipeEntry entry = Instantiate(_recipePrefab, _recipeListViewport);
                entry.gameObject.name = $"Recipe_{i}_{recipe.RecipeId}";
                entry.SetData(recipe, _iconTable, isCurrent);

                if (i < currentIndex)
                {
                    entry.MarkComplete();
                }

                _recipeEntries.Add(entry);
            }

            RefreshLayout();
        }

        private void UpdateCurrentIndicators()
        {
            for (int i = 0; i < _recipeEntries.Count; i++)
            {
                bool isCurrent = i == _currentRecipeIndex;
                _recipeEntries[i].SetCurrent(isCurrent);
            }
        }

        public void UpdateMaterialProgress(Dictionary<CraftedMaterialType, int> ownedMaterials)
        {
            if (_currentRecipeIndex >= 0 && _currentRecipeIndex < _recipeEntries.Count)
            {
                _recipeEntries[_currentRecipeIndex].UpdateMaterialProgress(ownedMaterials);
            }
        }

        private void ScrollToRecipe(int index)
        {
            if (_scrollRect == null || index < 0 || index >= _recipeEntries.Count)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            float totalHeight = _recipeListViewport.rect.height;
            float viewportHeight = _scrollRect.viewport.rect.height;

            if (totalHeight <= viewportHeight)
            {
                return;
            }

            RectTransform targetRect = _recipeEntries[index].GetComponent<RectTransform>();
            float targetY = -targetRect.anchoredPosition.y;
            float normalizedPosition = Mathf.Clamp01(targetY / (totalHeight - viewportHeight));

            _scrollRect.verticalNormalizedPosition = 1f - normalizedPosition;
        }

        private void ClearAllRecipes()
        {
            foreach (UI_RecipeEntry entry in _recipeEntries)
            {
                if (entry != null)
                {
                    Destroy(entry.gameObject);
                }
            }

            _recipeEntries.Clear();
        }

        private void RefreshLayout()
        {
            if (_recipeListViewport == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_recipeListViewport);

            RectTransform parent = _recipeListViewport.parent as RectTransform;
            if (parent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }
        }

        public void SetRecipes(List<RecipeData> recipes, int currentIndex = 0)
        {
            if (_recipeListViewport == null || _recipePrefab == null || recipes == null)
            {
                return;
            }

            ClearAllRecipes();
            _currentRecipeIndex = currentIndex;

            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeData recipe = recipes[i];
                bool isCurrent = i == currentIndex;

                UI_RecipeEntry entry = Instantiate(_recipePrefab, _recipeListViewport);
                entry.gameObject.name = $"Recipe_{i}_{recipe.RecipeId}";
                entry.SetData(recipe, _iconTable, isCurrent);

                if (i < currentIndex)
                {
                    entry.MarkComplete();
                }

                _recipeEntries.Add(entry);
            }

            RefreshLayout();

            if (_recipeListScrollView != null)
            {
                _recipeListScrollView.gameObject.SetActive(true);
            }
        }

        public void SetCurrentRecipeIndex(int index)
        {
            if (index < 0 || index >= _recipeEntries.Count || index == _currentRecipeIndex)
            {
                return;
            }

            OnRecipeIndexChanged(index);
        }
    }
}
