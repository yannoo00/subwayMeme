using UnityEngine;

public class StationController : MonoBehaviour
{
    private void OnEnable()
    {
        StageEvents.OnStationArrived += HandleStationArrived;
    }

    private void OnDisable()
    {
        StageEvents.OnStationArrived -= HandleStationArrived;
    }

    private void HandleStationArrived(StageNode node)
    {
        Debug.Log($"[StationController] 역 도착 - floor: {node.floor}, type: {node.type}");

        switch (node.type)
        {
            case NodeType.shop:
                ActivateShop();
                break;

            case NodeType.heal:
                ActivateHeal();
                break;

            case NodeType.trap:
                ActivateTrap();
                break;

            case NodeType.box:
                ActivateBox();
                break;

            default:
                Debug.LogWarning($"[StationController] 처리되지 않은 NodeType: {node.type}");
                break;
        }
    }

    private void ActivateShop()
    {
        // TODO: 상점 UI 활성화
        Debug.Log("[StationController] 상점 활성화");
    }

    private void ActivateHeal()
    {
        // TODO: 회복 오브젝트 활성화
        Debug.Log("[StationController] 회복 스테이션 활성화");
    }

    private void ActivateTrap()
    {
        // TODO: 함정 이벤트 처리
        Debug.Log("[StationController] 함정 이벤트 활성화");
    }

    private void ActivateBox()
    {
        // TODO: 보물 상자 오브젝트 활성화
        Debug.Log("[StationController] 보물 상자 활성화");
    }
}
