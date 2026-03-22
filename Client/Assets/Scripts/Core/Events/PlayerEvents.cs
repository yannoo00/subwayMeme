using System;

// 플레이어 관련 이벤트
public static class PlayerEvents
{
    public static event Action<int, int> OnHealthChanged;  // 현재, 최대
    public static event Action OnPlayerDied;
    public static event Action<int> OnPlayerDamaged;       // 데미지량

    public static void HealthChanged(int current, int max) => OnHealthChanged?.Invoke(current, max);
    public static void PlayerDied() => OnPlayerDied?.Invoke();
    public static void PlayerDamaged(int damage) => OnPlayerDamaged?.Invoke(damage);
}
