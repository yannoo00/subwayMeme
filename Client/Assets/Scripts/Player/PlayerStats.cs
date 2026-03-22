using UnityEngine;

public class PlayerStats: MonoBehaviour, IDamageable
{
    [Header("Player Stats")]
    [SerializeField] private int _currentHealth = 100;
    [SerializeField] private int _maxHealth = 100;


    public int CurrentHealth    => _currentHealth;
    public int MaxHealth        => _maxHealth;
    public bool IsAlive         => _currentHealth > 0;
    // 닷지 중 무적 여부 (PlayerController에서 설정)
    public bool IsInvincible { get; set; }



    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;
        if (IsInvincible) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0);

        PlayerEvents.PlayerDamaged(damage);
        PlayerEvents.HealthChanged(_currentHealth, _maxHealth);

        Debug.Log("[PlayerStats] 피격! HP: " + _currentHealth + "/" + _maxHealth);

        if (!IsAlive)
        {
            Die();
        }        
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
