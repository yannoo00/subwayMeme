using System;

// 플레이어 관련 이벤트
public static class PlayerEvents
{
    public static event Action<int, int> OnHealthChanged;           // 현재, 최대
    public static event Action OnPlayerDied;
    public static event Action<int> OnPlayerDamaged;                // 데미지량

    // slotIndex: 0 or 1 / data: null이면 Fist(빈 슬롯)
    public static event Action<int, WeaponData> OnWeaponSlotChanged;

    // skillSlotIndex: 0 or 1 (3번키/4번키) / data: null이면 빈 슬롯
    public static event Action<int, SkillData> OnSkillSlotChanged;

    public static void HealthChanged(int current, int max) => OnHealthChanged?.Invoke(current, max);
    public static void PlayerDied() => OnPlayerDied?.Invoke();
    public static void PlayerDamaged(int damage) => OnPlayerDamaged?.Invoke(damage);
    public static void WeaponSlotChanged(int slotIndex, WeaponData data) => OnWeaponSlotChanged?.Invoke(slotIndex, data);
    public static void SkillSlotChanged(int skillSlotIndex, SkillData data) => OnSkillSlotChanged?.Invoke(skillSlotIndex, data);
}
