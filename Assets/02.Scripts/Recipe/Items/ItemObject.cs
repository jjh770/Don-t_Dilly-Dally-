using Photon.Pun;
using UnityEngine;

namespace DontDillyDally.Data
{
    // 월드에 배치되는 아이템 오브젝트의 공통 베이스 클래스입니다.
    // 표시 이름, 모델 프리팹, 박스 콜라이더 설정을 공통으로 관리합니다.
    public abstract class ItemObject : MonoBehaviour, IRecyclable
    {
        private const int InvalidLayer = -1;
        private const string SupplyItemLayerName = "SupplyItem";
        private const string InteractableItemLayerName = "InteractableItem";

        protected delegate bool PresentationResolver<TItemType>(TItemType itemType, out string displayName, out GameObject modelPrefab);

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
        private BoxCollider _defaultBoxCollider;
        private PhotonView _photonView;
        private NetworkItemOwnership _networkItemOwnership;
        private NetworkItemState _networkItemState;
        private int _currentAssignedLayer = InvalidLayer;
        private bool _isPendingRecycle;

        public event System.Action ModelRefreshed;
        public event System.Action<ItemObject> Recycled;

        public bool HasLeftSource => _networkItemState != null && _networkItemState.HasLeftSource;
        public PhotonView PhotonView => _photonView;
        public int ViewId => PhotonView != null ? PhotonView.ViewID : -1;
        public bool HasPhotonView => PhotonView != null;
        public NetworkItemOwnership NetworkOwnership => _networkItemOwnership;
        public NetworkItemState NetworkState => _networkItemState;
        public bool IsPendingRecycle => _isPendingRecycle;

        protected virtual void Awake()
        {
            _photonView = GetComponent<PhotonView>();
            _defaultBoxCollider = GetComponent<BoxCollider>();
            ResetRecycleState();

            if (ModelPrefab != null)
            {
                RefreshModel();
            }

            _networkItemState = GetComponent<NetworkItemState>();
            if (_networkItemState == null)
            {
                _networkItemState = gameObject.AddComponent<NetworkItemState>();
            }

            _networkItemOwnership = GetComponent<NetworkItemOwnership>();
            if (_networkItemOwnership == null)
            {
                _networkItemOwnership = gameObject.AddComponent<NetworkItemOwnership>();
            }
        }

        protected virtual void OnEnable()
        {
            ResetRecycleState();
        }

        public virtual void Initialize(string displayName, GameObject modelPrefab = null)
        {
            DisplayName = displayName;
            SetModelPrefab(modelPrefab);
        }

        public virtual void PrepareForRecycle()
        {
            NotifyRecycled();

            transform.SetParent(null, true);

            IRecyclable[] recyclables = GetComponents<IRecyclable>();
            foreach (IRecyclable recyclable in recyclables)
            {
                if (ReferenceEquals(recyclable, this))
                {
                    continue;
                }

                recyclable.PrepareForRecycle();
            }
        }

        public bool TryBeginRecycle()
        {
            if (_isPendingRecycle)
            {
                return false;
            }

            _isPendingRecycle = true;
            return true;
        }

        public void ResetRecycleState()
        {
            _isPendingRecycle = false;
        }

        public void NotifyRecycled()
        {
            Recycled?.Invoke(this);
            Recycled = null;
        }

        public void RequestRecycleOnMaster()
        {
            if (_photonView == null || !PhotonNetwork.InRoom || PhotonNetwork.MasterClient == null)
            {
                return;
            }

            _photonView.RPC(nameof(RPC_RequestRecycleOnMaster), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        [PunRPC]
        public void RPC_RequestRecycleOnMaster(int requesterActorNumber, PhotonMessageInfo info)
        {
            if (info.Sender == null || info.Sender.ActorNumber != requesterActorNumber)
            {
                return;
            }

            ItemRecycleUtility.TryRecycleAsMaster(this);
        }

        protected void ResetReusableItemState()
        {
            DisplayName = null;
            ModelPrefab = null;
            ClearCurrentModel();
        }

        public virtual void SetModelPrefab(GameObject modelPrefab)
        {
            ModelPrefab = modelPrefab;
            RefreshModel();
        }

        protected void InitializeWithPresentation<TItemType>(TItemType itemType, string fallbackDisplayName, PresentationResolver<TItemType> presentationResolver = null)
        {
            string resolvedDisplayName = string.IsNullOrWhiteSpace(DisplayName)
                ? fallbackDisplayName
                : DisplayName;

            GameObject resolvedModelPrefab = ModelPrefab;

            if (presentationResolver != null &&
                presentationResolver(itemType, out string catalogDisplayName, out GameObject catalogModelPrefab))
            {
                if (!string.IsNullOrWhiteSpace(catalogDisplayName))
                {
                    resolvedDisplayName = catalogDisplayName;
                }

                if (catalogModelPrefab != null)
                {
                    resolvedModelPrefab = catalogModelPrefab;
                }
            }

            TryApplyBoxColliderFromModelPrefab(resolvedModelPrefab);

            DisplayName = resolvedDisplayName;
            SetModelPrefab(resolvedModelPrefab);
        }

        public void ApplyBoxCollider(Vector3 center, Vector3 size)
        {
            BoxCollider targetCollider = GetTargetBoxCollider();
            if (targetCollider == null)
            {
                return;
            }

            targetCollider.center = center;
            targetCollider.size = size;
        }

        public bool IsStillAt(Transform expectedParent)
        {
            return transform.parent == expectedParent;
        }

        public bool IsRuntimeModelCollider(Collider collider)
        {
            if (collider == null || CurrentModelInstance == null)
            {
                return false;
            }

            Transform colliderTransform = collider.transform;
            Transform modelTransform = CurrentModelInstance.transform;
            return colliderTransform == modelTransform || colliderTransform.IsChildOf(modelTransform);
        }

        public bool TryApplyBoxColliderFromModelPrefab(GameObject modelPrefab)
        {
            if (modelPrefab == null)
            {
                return false;
            }

            BoxCollider sourceCollider = modelPrefab.GetComponent<BoxCollider>();
            if (sourceCollider == null)
            {
                return false;
            }

            ApplyBoxCollider(sourceCollider.center, sourceCollider.size);
            return true;
        }

        protected void ApplyOverrideMaterials(Material[] overrideMaterials)
        {
            if (overrideMaterials == null || overrideMaterials.Length == 0 || CurrentModelInstance == null)
            {
                return;
            }

            Renderer[] renderers = CurrentModelInstance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Material[] targetMaterials = BuildOverrideMaterials(renderer.sharedMaterials, overrideMaterials);
                renderer.materials = targetMaterials;
            }
        }

        private static Material[] BuildOverrideMaterials(Material[] currentMaterials, Material[] overrideMaterials)
        {
            if (currentMaterials == null || currentMaterials.Length == 0)
            {
                Material[] singleMaterial = new Material[overrideMaterials.Length];
                for (int i = 0; i < overrideMaterials.Length; i++)
                {
                    singleMaterial[i] = overrideMaterials[i];
                }

                return singleMaterial;
            }

            Material[] result = new Material[currentMaterials.Length];
            for (int i = 0; i < currentMaterials.Length; i++)
            {
                Material selectedMaterial = i < overrideMaterials.Length
                    ? overrideMaterials[i]
                    : overrideMaterials[overrideMaterials.Length - 1];

                result[i] = selectedMaterial != null ? selectedMaterial : currentMaterials[i];
            }

            return result;
        }

        public virtual void RefreshModel()
        {
            ClearCurrentModel();

            if (ModelPrefab == null)
            {
                return;
            }

            Transform parent = ModelRoot != null ? ModelRoot : transform;
            CurrentModelInstance = Instantiate(ModelPrefab, parent);
            CurrentModelInstance.name = GetModelInstanceName();
            CurrentModelInstance.transform.localPosition = Vector3.zero;
            CurrentModelInstance.transform.localRotation = Quaternion.identity;
            CurrentModelInstance.transform.localScale = Vector3.one;
            DisableModelColliders();

            if (_currentAssignedLayer != InvalidLayer)
            {
                ApplyLayerRecursively(_currentAssignedLayer);
            }

            ModelRefreshed?.Invoke();
        }

        private string GetModelInstanceName()
        {
            if (ModelPrefab != null && !string.IsNullOrWhiteSpace(ModelPrefab.name))
            {
                return $"{ModelPrefab.name}_Model";
            }

            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                return $"{DisplayName}_Model";
            }

            return $"{name}_Model";
        }

        protected void ClearCurrentModel()
        {
            if (CurrentModelInstance != null)
            {
                Destroy(CurrentModelInstance);
                CurrentModelInstance = null;
            }
        }

        private void DisableModelColliders()
        {
            if (CurrentModelInstance == null)
            {
                return;
            }

            Collider[] modelColliders = CurrentModelInstance.GetComponentsInChildren<Collider>(true);
            foreach (Collider modelCollider in modelColliders)
            {
                modelCollider.enabled = false;
            }
        }

        private BoxCollider GetTargetBoxCollider()
        {
            if (TargetBoxCollider != null)
            {
                return TargetBoxCollider;
            }

            return _defaultBoxCollider;
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
            {
                return;
            }

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