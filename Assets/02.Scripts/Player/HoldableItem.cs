using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class HoldableItem : MonoBehaviour, IHoldable
{
    public bool IsInteracting { get; private set; }
    public Transform Transform => transform;

    [Header("던지기 설정")]
    [SerializeField] private float _upAngle = 0.5f;
    [SerializeField] private float _ignoreCollisionDuration = 0.3f;

    private Rigidbody _rigidbody;
    private Collider _collider;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }

    public void Interact(Transform holdPoint)
    {
        IsInteracting = true;
        _rigidbody.isKinematic = true;
        _collider.enabled = false;

        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void StopInteract()
    {
        Drop();
    }

    public void Hold(Transform holdPoint)
    {
        Interact(holdPoint);
    }

    public void Throw(Vector3 direction, float force, Collider[] throwerColliders = null)
    {
        IsInteracting = false;
        transform.SetParent(null);
        _rigidbody.isKinematic = false;
        _collider.enabled = true;

        if (throwerColliders != null)
        {
            StartCoroutine(IgnoreCollisionTemporarily(throwerColliders));
        }

        Vector3 throwDirection = (direction + Vector3.up * _upAngle).normalized;
        _rigidbody.AddForce(throwDirection * force, ForceMode.Impulse);
    }

    
    private IEnumerator IgnoreCollisionTemporarily(Collider[] colliders)
    {
        // 던지자마자 잠깐 플레이어 콜라이더 무시 (충돌 안 하게)
        SetCollisionWithThrower(colliders, true);

        yield return new WaitForSeconds(_ignoreCollisionDuration);

        SetCollisionWithThrower(colliders, false);
    }

    private void SetCollisionWithThrower(Collider[] colliders, bool Isignore)
    {
        if (_collider == null) return;

        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                Physics.IgnoreCollision(_collider, col, Isignore);
            }
        }
    }

    public void Drop()
    {
        IsInteracting = false;
        transform.SetParent(null);
        _rigidbody.isKinematic = false;
        _collider.enabled = true;
    }
}
