using GameProto;
using UnityEngine;

// 지하철 탑승구 오브젝트에 붙이는 컴포넌트
// OnSubwayArrived 이벤트 이후에만 탑승 가능 (역 웨이브 타이머 종료 후 활성화)
public class SubwayEntrance : MonoBehaviour, IInteractable
{
    private bool _isSubwayArrived = false;


    private void OnEnable()
    {
        StageEvents.OnSubwayArrived += HandleSubwayArrived;
    }


    private void OnDisable()
    {
        StageEvents.OnSubwayArrived -= HandleSubwayArrived;
        _isSubwayArrived = false;
    }


    private void HandleSubwayArrived(StageNode node)
    {
        _isSubwayArrived = true;
        Debug.Log("[SubwayEntrance] 지하철 도착 - 탑승 가능");
    }


    public void Interact()
    {
        if (StageManager.Instance.CurrentMapType != MapType.Station) return;

        if (!_isSubwayArrived)
        {
            Debug.Log("[SubwayEntrance] 탑승 불가 - 지하철이 아직 도착하지 않음");
            return;
        }

        // 상호작용하면 내가 탔다는걸 보내줌
        // 지금은 계속 누르면 계속 보내는데 이것도 조절 필요
        NetworkManager.Instance.SendGame(GamePacketId.CBoardSubway, new C_BoardSubway());

        // 방장만 노선도 UI 열기 (경로 선택 권한)
        if (NetworkManager.Instance.IsHost)
        {
            StageManager.Instance.HandlePlayerBoarding();
        }
            
        else
        {
            //여기에다가 방장이 아닌 유저는 기냥 대기중? 뭐 그런 UI 띄우든가 해야지 . 
        }
    }


    public string GetHintText()
    {
        if (!_isSubwayArrived)
            return "지하철을 기다리는 중...";

        return "E: 탑승";
    }
}
