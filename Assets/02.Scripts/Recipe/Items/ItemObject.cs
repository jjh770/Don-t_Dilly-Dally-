using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 월드에 배치되는 아이템 오브젝트의 공통 베이스 클래스입니다.
    // 표시 이름, 모델 프리팹, 박스 콜라이더 설정을 공통으로 관리합니다.
    public abstract class ItemObject : MonoBehaviour
    {
        private const int InvalidLayer = -1;
        private const string SupplyItemLayerName = "SupplyItem";
        private const string InteractableItemLayerName = "InteractableItem";

        protected delegate bool PresentationResolver<TItemType>(
            TItemType itemType,
            out string displayName,
            out GameObject modelPrefab);

        [Header("아이템 공통 정보")]
        [Tooltip("인스펙터와 UI에서 사용할 아이템 표시 이름")]
        public string DisplayName;

        [Tooltip("모델을 배치할 루트 Transform")]
        public Transform ModelRoot;

        [Tooltip("현재 아이템에 연결된 모델 프리팹")]
        public GameObject ModelPrefab;

        [Tooltip("모델 프리팹의 BoxCollider 값을 복사해 적용할 대상 콜라이더")]
        public BoxCollider TargetBoxCollider;

        protected GameObject CurrentModelInstance;
        private NetworkItemOwnership _networkItemOwnership;
        private NetworkItemState _networkItemState;
        private int _currentAssignedLayer = InvalidLayer;

        public bool HasLeftSource => _networkItemState != null && _networkItemState.HasLeftSource;
        public NetworkItemOwnership NetworkOwnership => _networkItemOwnership;
        public NetworkItemState NetworkState => _networkItemState;

        protected virtual void Awake()
        {
            if (ModelPrefab != null)
                RefreshModel();
            _networkItemState = GetComponent<NetworkItemState>();

            if (_networkItemState == null)
                _networkItemState = gameObject.AddComponent<NetworkItemState>();

            _networkItemOwnership = GetComponent<NetworkItemOwnership>();

            if (_networkItemOwnership == null)
                _networkItemOwnership = gameObject.AddComponent<NetworkItemOwnership>();
        }

        public virtual void Initialize(string displayName, GameObject modelPrefab = null)
        {
            DisplayName = displayName;
            SetModelPrefab(modelPrefab);
        }

        public virtual void SetModelPrefab(GameObject modelPrefab)
        {
            ModelPrefab = modelPrefab;
            RefreshModel();
        }

        protected void InitializeWithPresentation<TItemType>(
            TItemType itemType,
            string fallbackDisplayName,
            PresentationResolver<TItemType> presentationResolver = null)
        {
            string resolvedDisplayName = string.IsNullOrWhiteSpace(DisplayName)
                ? fallbackDisplayName
                : DisplayName;

            GameObject resolvedModelPrefab = ModelPrefab;

            if (presentationResolver != null &&
                presentationResolver(
                    itemType,
                    out string catalogDisplayName,
                    out GameObject catalogModelPrefab))
            {
                if (!string.IsNullOrWhiteSpace(catalogDisplayName))
                    resolvedDisplayName = catalogDisplayName;

                if (catalogModelPrefab != null)
                    resolvedModelPrefab = catalogModelPrefab;
            }

            TryApplyBoxColliderFromModelPrefab(resolvedModelPrefab);

            DisplayName = resolvedDisplayName;
            SetModelPrefab(resolvedModelPrefab);
        }

        public void ApplyBoxCollider(Vector3 center, Vector3 size)
        {
            BoxCollider targetCollider = GetTargetBoxCollider();
            if (targetCollider == null)
                return;

            targetCollider.center = center;
            targetCollider.size = size;
        }

        public bool IsStillAt(Transform expectedParent)
        {
            return transform.parent == expectedParent;
        }

        public bool TryApplyBoxColliderFromModelPrefab(GameObject modelPrefab)
        {
            if (modelPrefab == null)
                return false;

            BoxCollider sourceCollider = modelPrefab.GetComponent<BoxCollider>();
            if (sourceCollider == null)
                return false;

            ApplyBoxCollider(sourceCollider.center, sourceCollider.size);
            return true;
        }

        public virtual void RefreshModel()
        {
            ClearCurrentModel();

            if (ModelPrefab == null)
                return;

            Transform parent = ModelRoot != null ? ModelRoot : transform;
            CurrentModelInstance = Instantiate(ModelPrefab, parent);
            CurrentModelInstance.name = $"{name}_Model";
            CurrentModelInstance.transform.localPosition = Vector3.zero;
            CurrentModelInstance.transform.localRotation = Quaternion.identity;
            CurrentModelInstance.transform.localScale = Vector3.one;
            DisableModelColliders();

            if (_currentAssignedLayer != InvalidLayer)
                ApplyLayerRecursively(_currentAssignedLayer);
        }

        protected void ClearCurrentModel()
        {
            if (CurrentModelInstance != null)
                Destroy(CurrentModelInstance);
        }

        private void DisableModelColliders()
        {
            if (CurrentModelInstance == null)
                return;

            Collider[] modelColliders = CurrentModelInstance.GetComponentsInChildren<Collider>(true);
            foreach (Collider modelCollider in modelColliders)
            {
                modelCollider.enabled = false;
            }
        }

        private BoxCollider GetTargetBoxCollider()
        {
            if (TargetBoxCollider != null)
                return TargetBoxCollider;

            return GetComponent<BoxCollider>();
        }

        public void ResetSourceState()
        {
            _networkItemState?.ResetSourceState();
        }

        public void SetAsSupplyItem()
        {
            SetItemLayer(LayerMask.NameToLayer(SupplyItemLayerName));
        }

        public void NotifyLeftSource()
        {
            _networkItemOwnership?.NotifyLeftSource();
        }

        public void SetAsInteractableItem()
        {
            SetItemLayer(LayerMask.NameToLayer(InteractableItemLayerName));
        }

        public void SetItemLayer(int layer)
        {
            if (layer == InvalidLayer)
                return;

            _currentAssignedLayer = layer;
            ApplyLayerRecursively(layer);
        }

        private void ApplyLayerRecursively(int layer)
        {
            ApplyLayerRecursively(transform, layer);
        }

        private static void ApplyLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;

            for (int i = 0; i < root.childCount; i++)
            {
                ApplyLayerRecursively(root.GetChild(i), layer);
            }
        }
    }
}
