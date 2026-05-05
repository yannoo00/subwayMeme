using UnityEngine;

// 바닥에 드랍된 재화 아이템 (진화 포인트 / 유전자 포인트)
// 플레이어가 트리거 범위에 들어오면 자동 획득
// Collider는 IsTrigger = true 로 설정 필요
public class CurrencyPickup : MonoBehaviour
{
    private DropType _type;
    private int _amount;

    public void Init(DropType type, int amount)
    {
        _type   = type;
        _amount = amount;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var stats = other.GetComponent<PlayerStats>();
        if (stats == null) return;

        switch (_type)
        {
            case DropType.EvolutionPoint:
                stats.AddEvolutionPoints(_amount);
                break;
            case DropType.GenePoint:
                stats.AddGenePoints(_amount);
                break;
        }

        Destroy(gameObject);
    }
}
