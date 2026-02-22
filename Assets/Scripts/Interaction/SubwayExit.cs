using UnityEngine;

// 지하철 하차구 오브젝트에 붙이는 컴포넌트
// 플레이어가 E키를 눌러 하차 시 역으로 전환
public class SubwayExit : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        // 지하철 주행 중일 때 && 모든 적을 죽였을 때만 하차 가능(플래그 추가 필요)
        if (StageManager.Instance.CurrentMapType != MapType.Subway) return;

        StageManager.Instance.StartStation();
    }

    public string GetHintText()
    {
        return "E: 하차";
    }
}
