using UnityEngine;

public class RobbyBootstrapper : MonoBehaviour
{
    [SerializeField] private RoomView _roomView;

    private void Awake()
    {
        // 1. Model 생성

        // 2. Presenter 생성
        RoomPresenter presenter = new RoomPresenter(_roomView);

        // 3. View 초기화
        _roomView.Init(presenter);
    }

}
