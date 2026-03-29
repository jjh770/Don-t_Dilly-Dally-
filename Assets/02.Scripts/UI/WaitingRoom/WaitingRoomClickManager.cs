using System;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class WaitingRoomClickManager : MonoBehaviour
{
    private WaitingRoomPresenter _presenter;

    [SerializeField] private string _playerTag = "Player";
    void Update()
    {
        if (_presenter == null) return;

        if (!_presenter.IsMaster) return;

        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick();
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleCloseCheck();
        }
    }

    private void HandleCloseCheck()
    {
        GameObject clickedUI = GetTopClickedUI();

        //누른 UI가 팝업 UI라면 팝업을 닫는 클릭으로 취급하지 않음.
        if (clickedUI != null && clickedUI.GetComponentInParent<ContextMenuMarker>() != null) return;

        _presenter.ClearSelectedPlayer();
    }

    private void HandleRightClick()
    {
        if (TryGetClickedPlayer(out PhotonView target))
        {
            if (target.IsMine) return;
            _presenter.SelectPlayer(target.Owner, Input.mousePosition);
            return;
        }

        _presenter.ClearSelectedPlayer();
    }

    private bool TryGetClickedPlayer(out PhotonView target)
    {
        target = null;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return false;

        if (!hit.collider.CompareTag(_playerTag)) return false;
        target = hit.collider.GetComponent<PhotonView>();
        return target != null;
    }

    private GameObject GetTopClickedUI()
    {
        if (EventSystem.current == null) return null;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        return results.Count > 0 ? results[0].gameObject : null;
    }


    public void Initialized(WaitingRoomPresenter presenter)
    {
        _presenter = presenter;
    }   
}
