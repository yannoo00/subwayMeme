// 진화 포인트(세션 한정 카운터)
// 영구 저장 없이 게임 세션 동안만 유지. 픽업 시 누적되고 이벤트로 HUD/UI 통지
public static class CurrencyHelper
{
    private static int _evolutionPoints;

    public static int GetEvolutionPoints() => _evolutionPoints;

    public static void AddEvolutionPoints(int amount)
    {
        if (amount <= 0) return;
        _evolutionPoints += amount;
        PlayerEvents.EvolutionPointsChanged(_evolutionPoints);
    }
}
