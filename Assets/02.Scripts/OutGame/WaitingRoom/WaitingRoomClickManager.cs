using UnityEngine;

public class WaitingRoomClickManager : MonoBehaviour
{
    private WaitingRoomPresenter _presenter;

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (!PhotonServerManager.Instance.IsMasterClient) return;

            Vector3 clickPosition = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(clickPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var target = hit.collider.GetComponent<PlayerController>();
                if (target != null)
                {
                    if (_presenter == null)
                    {
                        Debug.Log("[WaitingRoomClickManager] presenter가 null 입니다.");
                    }
                    Debug.Log($"[WaitingRoomClickManager] 클릭 {target.PhotonView.Owner.NickName}");
                    if (target.PhotonView.IsMine) return;
                    _presenter.SelectPlayer(target.PhotonView.Owner, clickPosition);
                } else
                {
                    //_presenter.ClearSelectedPlayer();
                }
            }
        }
    }

    public void Initialized(WaitingRoomPresenter presenter)
    {
        _presenter = presenter;
    }
}
