using UnityEngine;

public class RobbyBootstrapper : MonoBehaviour
{
    [SerializeField] private RoomView roomView;

    private void Awake()
    {
        // 1. Model 积己

        // 2. Presenter 积己
        RoomPresenter presenter = new RoomPresenter(roomView);

        // 3. View 檬扁拳
        roomView.Init(presenter);
    }

}
