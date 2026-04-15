using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TemporaryCollisionIgnore : MonoBehaviour
{
    private readonly List<IgnoredCollisionPair> _ignoredCollisionPairs = new();
    private Coroutine _restoreCoroutine;

    private readonly struct IgnoredCollisionPair
    {
        public IgnoredCollisionPair(Collider sourceCollider, Collider targetCollider)
        {
            SourceCollider = sourceCollider;
            TargetCollider = targetCollider;
        }

        public Collider SourceCollider { get; }
        public Collider TargetCollider { get; }
    }

    private void OnDisable()
    {
        Restore();
    }

    public void IgnoreTemporarily(Collider[] sourceColliders, Collider[] targetColliders, float duration)
    {
        Restore();

        if (sourceColliders == null || sourceColliders.Length == 0 ||
            targetColliders == null || targetColliders.Length == 0)
        {
            return;
        }

        for (int i = 0; i < sourceColliders.Length; i++)
        {
            Collider sourceCollider = sourceColliders[i];
            if (sourceCollider == null)
            {
                continue;
            }

            for (int j = 0; j < targetColliders.Length; j++)
            {
                Collider targetCollider = targetColliders[j];
                if (targetCollider == null)
                {
                    continue;
                }

                Physics.IgnoreCollision(sourceCollider, targetCollider, true);
                _ignoredCollisionPairs.Add(new IgnoredCollisionPair(sourceCollider, targetCollider));
            }
        }

        if (_ignoredCollisionPairs.Count > 0 && duration > 0f)
        {
            _restoreCoroutine = StartCoroutine(RestoreAfterDelay(duration));
        }
    }

    public void Restore()
    {
        Restore(true);
    }

    private IEnumerator RestoreAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);

        Restore(false);
        _restoreCoroutine = null;
    }

    private void Restore(bool stopCoroutine)
    {
        if (stopCoroutine && _restoreCoroutine != null)
        {
            StopCoroutine(_restoreCoroutine);
            _restoreCoroutine = null;
        }

        for (int i = 0; i < _ignoredCollisionPairs.Count; i++)
        {
            IgnoredCollisionPair pair = _ignoredCollisionPairs[i];
            if (pair.SourceCollider == null || pair.TargetCollider == null)
            {
                continue;
            }

            Physics.IgnoreCollision(pair.SourceCollider, pair.TargetCollider, false);
        }

        _ignoredCollisionPairs.Clear();
    }
}
