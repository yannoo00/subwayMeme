// 진화 포인트(영구 재화) 조작을 위한 정적 헬퍼
// 획득/소모 시 SaveManager에 즉시 반영하고 UI 이벤트 발행
public static class CurrencyHelper
{
    public static int GetEvolutionPoints()
    {
        if (SaveManager.Instance == null) return 0;
        return SaveManager.Instance.Current.evolutionPoints;
    }

    public static void AddEvolutionPoints(int amount)
    {
        if (amount <= 0 || SaveManager.Instance == null) return;
        SaveManager.Instance.Current.evolutionPoints += amount;
        SaveManager.Instance.Save();
        PlayerEvents.EvolutionPointsChanged(GetEvolutionPoints());
    }

    public static bool TrySpendEvolutionPoints(int amount)
    {
        if (amount <= 0 || SaveManager.Instance == null) return false;
        if (GetEvolutionPoints() < amount) return false;
        SaveManager.Instance.Current.evolutionPoints -= amount;
        SaveManager.Instance.Save();
        PlayerEvents.EvolutionPointsChanged(GetEvolutionPoints());
        return true;
    }
}
