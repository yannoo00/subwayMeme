using UnityEngine;

// 지하철 탑승구 오브젝트에 붙이는 컴포넌트
// 플레이어가 E키를 눌러 탑승 시도 시 StageManager에 탑승 처리를 위임
public class SubwayEntrance : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        // 역 상태일 때만 탑승 가능
        if (StageManager.Instance.CurrentMapType != MapType.Station) return;

        StageManager.Instance.HandlePlayerBoarding();
    }

    public string GetHintText()
    {
        return "E: 탑승";
    }
}
