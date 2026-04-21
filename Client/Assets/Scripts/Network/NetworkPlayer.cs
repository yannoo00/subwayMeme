using UnityEngine;

// 원격 플레이어 오브젝트에 붙는 컴포넌트
// Step 1: 식별 정보만 보관
// Step 2에서 SetTargetState() + 위치 보간(Lerp) 추가 예정
public class NetworkPlayer : MonoBehaviour
{
    public int PlayerId { get; private set; }
    public string PlayerName { get; private set; }

    public void Init(int playerId, string playerName)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        gameObject.name = $"RemotePlayer_{playerId}_{playerName}";
    }
}
