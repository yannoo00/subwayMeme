using UnityEngine;

// 지하철 하차구 오브젝트에 붙이는 컴포넌트
// StageManager.CanExit가 true일 때만 하차 가능 (주행 타이머 종료 후 활성화)
public class SubwayExit : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        if (StageManager.Instance.CurrentMapType != MapType.Subway)
        {
            Debug.Log("[SubwayExit] Subway 씬이 아닌 곳에서 호출됨");
            return;
        }

        if (!StageManager.Instance.CanExit)
        {
            Debug.Log("[SubwayExit] 하차 불가 - 아직 역에 도착하지 않음");
            return;
        }

        StageManager.Instance.StartStation();
    }

    public string GetHintText()
    {
        if (!StageManager.Instance.CanExit)
            return "아직 역에 도착하지 않았습니다";

        return "E: 하차";
    }
}
