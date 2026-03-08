using UnityEngine;

// 지하철 하차구 오브젝트에 붙이는 컴포넌트
// 플레이어가 E키를 눌러 하차 시 역으로 전환
public class SubwayExit : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        // MapType 체크
        if (StageManager.Instance.CurrentMapType != MapType.Subway)
        {
            Debug.Log("you are using subwayExit but you are not in subway");
            return;
        }

    
        // 지하철에 적이 남아있으면 하차 불가
        if (SpawnManager.Instance.AliveEnemyCount > 0) 
        {
            Debug.Log("하차 불가 - 적이 남아있음");
            return;
        }

        // station 시작
        StageManager.Instance.StartStation();
    }

    public string GetHintText()
    {
        if (SpawnManager.Instance.AliveEnemyCount > 0)
            return "적을 모두 처치하세요";

        return "E: 하차";
    }
}
