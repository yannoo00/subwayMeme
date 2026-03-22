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

        StageManager.Instance.HandlePlayerBoarding();
    }


    public string GetHintText()
    {
        if (!_isSubwayArrived)
            return "지하철을 기다리는 중...";

        return "E: 탑승";
    }
}
