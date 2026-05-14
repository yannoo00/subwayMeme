using System;
using UnityEngine;

// 플레이어 관련 이벤트
public static class PlayerEvents
{
    // 로컬 플레이어 스폰 직후 발행 - vcam target 바인딩 등 외부 시스템에서 구독
    // 구독자는 이미 스폰된 상태에서도 LocalPlayerTransform로 즉시 wire up 가능
    public static event Action<Transform> OnLocalPlayerSpawned;
    public static Transform LocalPlayerTransform { get; private set; }

    public static event Action<int, int> OnHealthChanged;           // 현재, 최대
    public static event Action OnPlayerDied;
    public static event Action<int> OnPlayerDamaged;                // 데미지량

    // 로컬 플레이어 사망 후 관전 모드 진입/대상 전환 시 발행. 대상의 NetworkPlayer 전달
    public static event Action<NetworkPlayer> OnSpectateTargetChanged;

    // 슬롯 내용 변경 (픽업/드랍/스왑). slotIndex: 0 or 1 / data: null이면 빈 슬롯
    public static event Action<int, WeaponData> OnWeaponSlotChanged;

    // 활성 슬롯 변경 (1/2 키 전환). data는 활성 슬롯의 WeaponData (빈 슬롯이면 null)
    // HUD가 활성 무기 타입을 알아야 탄약 패널 가시성을 결정 가능
    public static event Action<int, WeaponData> OnActiveSlotChanged;

    // skillSlotIndex: 0 or 1 (3번키/4번키) / data: null이면 빈 슬롯
    public static event Action<int, SkillData> OnSkillSlotChanged;

    // 탄약 변경 (발사 / 재장전 완료 시)
    public static event Action<int, int> OnAmmoChanged;             // 현재, 최대

    // 재장전 시작 - HUD가 자체 시간 누적으로 진행률을 그리도록 duration만 전달
    public static event Action<float> OnReloadStarted;              // duration(초)
    public static event Action OnReloadFinished;

    public static event Action<bool> OnAimStateChanged;

    //재화 관련 
    public static event Action<int> OnGenePointsChanged;
    public static event Action<int> OnEvolutionPointsChanged;

    public static void LocalPlayerSpawned(Transform t)
    {
        LocalPlayerTransform = t;
        OnLocalPlayerSpawned?.Invoke(t);
    }

    public static void HealthChanged(int current, int max) => OnHealthChanged?.Invoke(current, max);
    public static void GenePointsChanged(int amount) => OnGenePointsChanged?.Invoke(amount);
    public static void EvolutionPointsChanged(int amount) => OnEvolutionPointsChanged?.Invoke(amount);
    public static void PlayerDied() => OnPlayerDied?.Invoke();
    public static void PlayerDamaged(int damage) => OnPlayerDamaged?.Invoke(damage);
    public static void SpectateTargetChanged(NetworkPlayer target) => OnSpectateTargetChanged?.Invoke(target);
    public static void WeaponSlotChanged(int slotIndex, WeaponData data) => OnWeaponSlotChanged?.Invoke(slotIndex, data);
    public static void ActiveSlotChanged(int slotIndex, WeaponData data) => OnActiveSlotChanged?.Invoke(slotIndex, data);
    public static void SkillSlotChanged(int skillSlotIndex, SkillData data) => OnSkillSlotChanged?.Invoke(skillSlotIndex, data);
    public static void AmmoChanged(int current, int max) => OnAmmoChanged?.Invoke(current, max);
    public static void ReloadStarted(float duration) => OnReloadStarted?.Invoke(duration);
    public static void ReloadFinished() => OnReloadFinished?.Invoke();
    public static void AimStateChanged(bool isAiming) => OnAimStateChanged?.Invoke(isAiming);
}
