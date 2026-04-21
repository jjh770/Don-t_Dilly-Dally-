using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RandomIntervalParticlePlayer : MonoBehaviour
{
    [Header("Interval (seconds)")]
    [SerializeField] private float _minInterval = 3f;
    [SerializeField] private float _maxInterval = 8f;

    [Header("Startup")]
    [SerializeField] private bool _randomizeInitialDelay = true;
    [SerializeField] private float _maxInitialDelay = 5f;

    private ParticleSystem _fx;
    private Coroutine _loop;

    private void Awake()
    {
        _fx = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        _loop = StartCoroutine(RunLoop());
    }

    private void OnDisable()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    private IEnumerator RunLoop()
    {
        // Stagger the first fire so co-located particles do not trigger on the same frame.
        if (_randomizeInitialDelay)
        {
            yield return new WaitForSeconds(Random.Range(0f, _maxInitialDelay));
        }

        while (true)
        {
            FxHelper.Play(_fx);
            yield return new WaitForSeconds(Random.Range(_minInterval, _maxInterval));
        }
    }
}
