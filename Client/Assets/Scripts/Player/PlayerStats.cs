using UnityEngine;

public class PlayerStats: MonoBehaviour, IDamageable
{
    [Header("Player Stats")]
    // CharacterDefinition.baseMaxHealth가 없을 때만 쓰이는 fallback 기본값
    [SerializeField] private int _baseMaxHealth = 100;

    private int _currentHealth;

    // 캐릭터 베이스 보너스 (CharacterDefinition.baseAttackPower)
    private float _baseAttackBonus;

    // 영구 강화 보너스 (UpgradeManager.ApplyBonusesToPlayer 로 설정)
    private int   _bonusMaxHp;
    private float _attackBonus;    // 추가 공격력 퍼센트 (PlayerCombat에서 참조)
    private float _dodgeReduction; // 닷지 쿨타임 감소 초 (PlayerController에서 참조)

    // 변이 보너스 (런 종료 시 MutationManager.ResetForNewRun 으로 초기화)
    private int   _mutationMaxHpBonus;
    private float _mutationAttackBonus;
    private float _mutationMoveSpeedBonus; // PlayerController에서 참조

    // GenePoint: 세션 내 유효, 런 종료 시 초기화
    private int _genePoints;

    public int CurrentHealth    => _currentHealth;
    public int MaxHealth        => _baseMaxHealth + _bonusMaxHp + _mutationMaxHpBonus;
    public bool IsAlive         => _currentHealth > 0;
    public bool IsInvincible { get; set; }

    public float AttackBonus       => _baseAttackBonus + _attackBonus + _mutationAttackBonus;
    public float DodgeReduction    => _dodgeReduction;
    public float MoveSpeedBonus    => _mutationMoveSpeedBonus; // PlayerController에서 읽어서 이속에 가산

    public int GenePoints => _genePoints;

    private void Start()
    {
        // 캐릭터 베이스 스탯을 SelectedCharacter 에서 가져와 적용
        // CharacterDefinition이 미할당이면 Inspector fallback 값 그대로 사용
        var charDef = GameManager.Instance?.SelectedCharacter;
        if (charDef != null)
        {
            _baseMaxHealth   = charDef.baseMaxHealth;
            _baseAttackBonus = charDef.baseAttackPower;
        }

        _currentHealth = _baseMaxHealth;

        // 강화/UpgradeManager가 없는 환경에서도 HUD가 정확한 값을 보도록 즉시 1회 발행
        // ApplyPermanentBonuses 가 뒤따라 호출되면 거기서 다시 한 번 발행됨 (멱등)
        PlayerEvents.HealthChanged(_currentHealth, MaxHealth);

        // 영구 강화 보너스 자동 적용 - GameManager가 보관하는 SelectedCharacterId 기준
        // GameManager는 Persistent 씬의 DontDestroyOnLoad라 게임 씬 진입 시 항상 존재
        if (UpgradeManager.Instance != null)
        {
            int characterId = GameManager.Instance != null ? GameManager.Instance.SelectedCharacterId : 0;
            UpgradeManager.Instance.ApplyBonusesToPlayer(characterId, this);
        }
    }

    // UpgradeManager에서 호출 - 영구 강화 보너스 일괄 적용
    public void ApplyPermanentBonuses(int bonusMaxHp, float attackBonus, float dodgeReduction)
    {
        _bonusMaxHp     = bonusMaxHp;
        _attackBonus    = attackBonus;
        _dodgeReduction = dodgeReduction;
        _currentHealth  = MaxHealth;
        PlayerEvents.HealthChanged(_currentHealth, MaxHealth);
    }



    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;
        if (IsInvincible) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0);

        PlayerEvents.PlayerDamaged(damage);
        PlayerEvents.HealthChanged(_currentHealth, MaxHealth);

        Debug.Log("[PlayerStats] 피격! HP: " + _currentHealth + "/" + MaxHealth);

        if (!IsAlive)
        {
            Die();
        }        
    }

    // 서버 권위 피격 적용 - 로컬에서 직접 계산하지 않고 서버가 확정한 HP로 설정
    public void ApplyServerDamage(int damage, int currentHp)
    {
        if (!IsAlive) return;

        _currentHealth = currentHp;

        PlayerEvents.PlayerDamaged(damage);
        PlayerEvents.HealthChanged(_currentHealth, MaxHealth);

        Debug.Log($"[PlayerStats] 서버 피격 적용: -{damage} HP -> {_currentHealth}/{MaxHealth}");

        if (!IsAlive) Die();
    }

    // MutationManager에서 호출 - 런 중 변이 스탯 보너스 적용
    public void ApplyStatBoost(StatType statType, float value)
    {
        switch (statType)
        {
            case StatType.MaxHp:
                _mutationMaxHpBonus += (int)value;
                // 최대 HP 증가분만큼 현재 HP도 회복
                _currentHealth = Mathf.Min(_currentHealth + (int)value, MaxHealth);
                PlayerEvents.HealthChanged(_currentHealth, MaxHealth);
                break;

            case StatType.AttackDamage:
                _mutationAttackBonus += value;
                break;

            case StatType.MoveSpeed:
                _mutationMoveSpeedBonus += value;
                break;
        }
    }

    // 런 종료 시 MutationManager가 호출 - 변이 보너스 초기화
    public void ResetMutationBonuses()
    {
        _mutationMaxHpBonus       = 0;
        _mutationAttackBonus      = 0;
        _mutationMoveSpeedBonus   = 0;
    }

    public void AddGenePoints(int amount)
    {
        _genePoints += amount;
        PlayerEvents.GenePointsChanged(_genePoints);
    }

    // 런 종료 시 세션 재화 초기화
    public void ResetSessionCurrency()
    {
        _genePoints = 0;
        PlayerEvents.GenePointsChanged(_genePoints);
    }

    public void Die()
    {
        PlayerEvents.PlayerDied();

        // 생존 카운터 감소 - 0이 되면 PlayerRegistry가 게임오버를 확정 (멱등)
        // 사망 애니메이션은 PlayerAnimator가 OnPlayerDied 구독으로 처리
        // Destroy는 GameManager 또는 게임오버 시스템이 담당할 예정
        PlayerRegistry.Instance?.NotifyLocalPlayerDied();
    }
}
