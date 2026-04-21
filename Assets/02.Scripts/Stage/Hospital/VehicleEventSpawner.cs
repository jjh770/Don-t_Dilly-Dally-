using Photon.Pun;
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(PhotonView))]
public class VehicleEventSpawner : MonoBehaviourPun
{
    [Serializable]
    public class VehicleDef
    {
        public GameObject prefab;
        public float spawnDelay;

        [Header("진입")]
        public SplineContainer enterSpline;
        public float enterSpeed = 10f;
        public bool enterReversed;

        [Header("대기")]
        public float waitDuration;

        [Header("퇴장 (비우면 진입 완료 후 파괴)")]
        public SplineContainer exitSpline;
        public float exitSpeed = 10f;
        public bool exitReversed;
    }

    [Serializable]
    public class VehicleEvent
    {
        public string name;
        public VehicleDef[] vehicles;
    }

    [Header("이벤트 목록")]
    [SerializeField] private VehicleEvent[] _events;

    [Header("스폰 주기")]
    [SerializeField] private float _startDelay = 5f;
    [SerializeField] private float _minInterval = 20f;
    [SerializeField] private float _maxInterval = 40f;

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(TriggerLoop());
        }
    }

    private IEnumerator TriggerLoop()
    {
        if (_startDelay > 0f)
        {
            yield return new WaitForSeconds(_startDelay);
        }

        while (true)
        {
            if (_events == null || _events.Length == 0)
            {
                yield break;
            }

            int eventIndex = UnityEngine.Random.Range(0, _events.Length);
            photonView.RPC(nameof(RPC_RunEvent), RpcTarget.All, eventIndex);

            float interval = UnityEngine.Random.Range(_minInterval, _maxInterval);
            yield return new WaitForSeconds(interval);
        }
    }

    [PunRPC]
    private void RPC_RunEvent(int eventIndex)
    {
        if (_events == null || eventIndex < 0 || eventIndex >= _events.Length)
        {
            return;
        }

        VehicleEvent evt = _events[eventIndex];
        if (evt == null || evt.vehicles == null)
        {
            return;
        }

        foreach (VehicleDef def in evt.vehicles)
        {
            if (def == null || def.prefab == null || def.enterSpline == null)
            {
                continue;
            }

            StartCoroutine(RunVehicle(def));
        }
    }

    private IEnumerator RunVehicle(VehicleDef def)
    {
        if (def.spawnDelay > 0f)
        {
            yield return new WaitForSeconds(def.spawnDelay);
        }

        Vector3 startPosition = (Vector3)def.enterSpline.EvaluatePosition(0f);
        GameObject spawned = Instantiate(def.prefab, startPosition, Quaternion.identity, transform);

        yield return MoveAlong(spawned, def.enterSpline, def.enterSpeed, def.enterReversed);

        if (spawned == null)
        {
            yield break;
        }

        if (def.waitDuration > 0f)
        {
            yield return new WaitForSeconds(def.waitDuration);
        }

        if (def.exitSpline != null && spawned != null)
        {
            yield return MoveAlong(spawned, def.exitSpline, def.exitSpeed, def.exitReversed);
        }

        if (spawned != null)
        {
            Destroy(spawned);
        }
    }

    private IEnumerator MoveAlong(GameObject target, SplineContainer spline, float speed, bool reverseFacing)
    {
        if (target == null)
        {
            yield break;
        }

        bool finished = false;
        VehiclePathMover mover = target.AddComponent<VehiclePathMover>();
        mover.Initialize(spline, speed, reverseFacing, () => finished = true);

        while (!finished && target != null)
        {
            yield return null;
        }

        if (target != null)
        {
            Destroy(mover);
        }
    }
}
