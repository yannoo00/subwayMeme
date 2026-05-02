using UnityEngine;

// 골드(재화) 조작을 위한 정적 헬퍼
// 실제 획득/소모 타이밍은 게임 기획 확정 후 각 시스템에서 직접 호출
// 예: 적 처치 -> CurrencyHelper.AddGold(10)
//     업그레이드 구매 -> CurrencyHelper.TrySpendGold(cost)
public static class CurrencyHelper
{
    public static int GetGold()
    {
        if (SaveManager.Instance == null) return 0;
        return SaveManager.Instance.Current.gold;
    }

    // amount 만큼 골드 추가 후 즉시 저장
    public static void AddGold(int amount)
    {
        if (amount <= 0 || SaveManager.Instance == null) return;
        SaveManager.Instance.Current.gold += amount;
        SaveManager.Instance.Save();
        Debug.Log($"[CurrencyHelper] +{amount} 골드 획득. 잔액: {GetGold()}");
    }

    // 골드가 충분하면 소모 후 true 반환, 부족하면 false 반환
    public static bool TrySpendGold(int amount)
    {
        if (amount <= 0 || SaveManager.Instance == null) return false;
        if (GetGold() < amount)
        {
            Debug.Log($"[CurrencyHelper] 골드 부족: 필요 {amount}, 보유 {GetGold()}");
            return false;
        }
        SaveManager.Instance.Current.gold -= amount;
        SaveManager.Instance.Save();
        Debug.Log($"[CurrencyHelper] -{amount} 골드 사용. 잔액: {GetGold()}");
        return true;
    }
}
