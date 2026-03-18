using UnityEngine;

// 적 오브젝트의 중심 클래스
// 데이터 허브 역할: EnemyData를 저장하고 다른 컴포넌트에 getter 제공
// 체력/피격/사망 관리 담당
public abstract class Enemy : MonoBehaviour, IDamageable
{
    [Header("Data")]
    [SerializeField] private EnemyData _data;

    private int _currentHealth;
    private int _maxHealth;

    public EnemyData Data           => _data;
    public int       CurrentHealth  => _currentHealth;
    public int       MaxHealth      => _maxHealth;
    public bool      IsAlive        => _currentHealth > 0;

    protected EnemyMove   _move;
    protected EnemyAI     _ai;
    protected EnemyAttack _attack;


    protected virtual void Awake()
    {
        _move   = GetComponent<EnemyMove>();
        _ai     = GetComponent<EnemyAI>();
        _attack = GetComponent<EnemyAttack>();

        if (_data != null)
        {
            _currentHealth  = _data.maxHealth;
            _maxHealth      = _data.maxHealth;
        }
            
        else
            Debug.LogWarning($"[Enemy] {gameObject.name} 에 EnemyData가 없습니다.");
    }


    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(_currentHealth, 0);
        Debug.Log($"[Enemy] {gameObject.name} 피격! HP: {_currentHealth}/{_data.maxHealth}");

        if (!IsAlive)
            Die();
    }


    // 자식 클래스가 사망 연출을 바꾸고 싶을 때 override
    public virtual void Die()
    {
        CombatEvents.EnemyDied(gameObject);

        // AI가 있으면 사망 연출 후 제거를 AI에 위임
        if (_ai != null)
            _ai.OnDeath();
        else
            Destroy(gameObject);
    }
}
