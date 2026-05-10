using UnityEngine;

// 강화 시스템 관리 싱글톤
// Inspector에서 UpgradeDefinition ScriptableObject 배열을 직접 할당
// DontDestroyOnLoad: 씬 전환 후에도 업그레이드 데이터 유지
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("강화 정의 목록")]
    [SerializeField] private UpgradeDefinition[] _upgrades;

    // === Unity 생명주기 ===

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // === 조회 메서드 ===

    public UpgradeDefinition[] AllDefinitions => _upgrades;

    public UpgradeDefinition GetDefinition(UpgradeType type)
    {
        if (_upgrades == null) return null;
        foreach (var u in _upgrades)
            if (u != null && u.type == type) return u;
        return null;
    }

    // characterId 캐릭터의 type 강화 현재 레벨 반환
    public int GetLevel(int characterId, UpgradeType type)
    {
        var data = SaveManager.Instance?.Current.GetCharacter(characterId);
        if (data == null) return 0;
        int idx = (int)type;
        return idx < data.upgradeLevels.Length ? data.upgradeLevels[idx] : 0;
    }

    // 현재 진화 포인트와 최대 레벨을 고려해 강화 가능 여부 반환
    public bool CanUpgrade(int characterId, UpgradeType type)
    {
        var def = GetDefinition(type);
        if (def == null) return false;
        int level = GetLevel(characterId, type);
        if (level >= def.maxLevel) return false;
        int cost = def.GetCost(level);
        return cost >= 0 && CurrencyHelper.GetEvolutionPoints() >= cost;
    }

    // 강화 실행: 성공 시 true, 골드 부족/최대 레벨이면 false
    public bool TryUpgrade(int characterId, UpgradeType type)
    {
        if (!CanUpgrade(characterId, type)) return false;

        var def = GetDefinition(type);
        int level = GetLevel(characterId, type);
        int cost = def.GetCost(level);

        if (!CurrencyHelper.TrySpendEvolutionPoints(cost)) return false;

        var data = SaveManager.Instance.Current.GetCharacter(characterId);
        data.upgradeLevels[(int)type]++;
        SaveManager.Instance.Save();

        Debug.Log($"[UpgradeManager] {def.displayName} Lv.{level + 1} 강화 완료");
        return true;
    }

    // 게임 시작 시 플레이어에 영구 강화 보너스 적용
    // PlayerStats.Start() 또는 플레이어 스폰 직후 호출
    public void ApplyBonusesToPlayer(int characterId, PlayerStats stats)
    {
        if (stats == null) return;

        var hpDef    = GetDefinition(UpgradeType.MaxHp);
        var atkDef   = GetDefinition(UpgradeType.AttackDamage);
        var dodgeDef = GetDefinition(UpgradeType.DodgeCooldown);

        int   bonusMaxHp      = hpDef    != null ? (int)hpDef.GetTotalValue(GetLevel(characterId, UpgradeType.MaxHp))         : 0;
        float attackBonus     = atkDef   != null ? atkDef.GetTotalValue(GetLevel(characterId, UpgradeType.AttackDamage))       : 0f;
        float dodgeReduction  = dodgeDef != null ? dodgeDef.GetTotalValue(GetLevel(characterId, UpgradeType.DodgeCooldown))    : 0f;

        stats.ApplyPermanentBonuses(bonusMaxHp, attackBonus, dodgeReduction);
    }
}
