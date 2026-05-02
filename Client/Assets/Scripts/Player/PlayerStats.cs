using UnityEngine;

public class PlayerStats: MonoBehaviour, IDamageable
{
    [Header("Player Stats")]
    [SerializeField] private int _currentHealth = 100;
    [SerializeField] private int _maxHealth = 100;

    // 영구 강화 보너스 (UpgradeManager.ApplyBonusesToPlayer 로 설정)
    private int   _bonusMaxHp;
    private float _attackBonus;    // 추가 공격력 퍼센트 (PlayerCombat에서 참조)
    private float _dodgeReduction; // 닷지 쿨타임 감소 초 (PlayerController에서 참조)

    public int CurrentHealth    => _currentHealth;
    public int MaxHealth        => _maxHealth + _bonusMaxHp;
    public bool IsAlive         => _currentHealth > 0;
    // 닷지 중 무적 여부 (PlayerController에서 설정)
    public bool IsInvincible { get; set; }

    public float AttackBonus    => _attackBonus;
    public float DodgeReduction => _dodgeReduction;

    private void Start()
    {
        // 영구 강화 보너스 자동 적용 (캐릭터 ID 0 고정, 추후 캐릭터 선택 시 변경)
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.ApplyBonusesToPlayer(0, this);
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

    public void Die()
    {
        PlayerEvents.PlayerDied();

        // Animator가 있으면 Death 애니메이션 재생
        // EnemyAI와 달리 플레이어는 게임오버 처리가 별도로 필요하므로
        // Destroy는 GameManager 또는 게임오버 시스템이 담당할 예정
        Animator animator = GetComponentInChildren<Animator>();
        animator?.SetBool(AnimatorParams.IsDead, true);
    }
}
