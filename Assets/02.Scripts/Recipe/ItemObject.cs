using UnityEngine;

namespace DontDillyDally.Data
{
    // 월드에 배치되는 실제 아이템 오브젝트의 공통 부모입니다.
    // 표시 이름, 모델, BoxCollider 값을 공통으로 관리합니다.
    public abstract class ItemObject : MonoBehaviour
    {
        [Header("아이템 공통 정보")]
        [Tooltip("인스펙터와 씬에서 사용할 아이템 표시 이름")]
        public string DisplayName;

        [Tooltip("모델을 배치할 루트 Transform")]
        public Transform ModelRoot;

        [Tooltip("현재 아이템에 연결된 모델 프리팹")]
        public GameObject ModelPrefab;

        [Tooltip("타입별로 BoxCollider 값을 주입받을 대상 콜라이더")]
        public BoxCollider TargetBoxCollider;

        protected GameObject CurrentModelInstance;

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

        public void ApplyBoxCollider(Vector3 center, Vector3 size)
        {
            BoxCollider targetCollider = GetTargetBoxCollider();
            if (targetCollider == null)
                return;

            targetCollider.center = center;
            targetCollider.size = size;
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
        }

        protected virtual void Awake()
        {
            if (ModelPrefab != null)
                RefreshModel();
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
    }
}
