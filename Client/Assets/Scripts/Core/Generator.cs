using UnityEngine;

// 각 Station에 배치되는 발전기
// 파괴되면 게임 오버
// 태그: "Generator"
public class Generator : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private int _maxHp = 500;

    private int _currentHp;

    // === IDamageable ===

    public int  CurrentHealth => _currentHp;
    public int  MaxHealth     => _maxHp;
    public bool IsAlive       => _currentHp > 0;

    // === Unity 생명주기 ===

    private void Awake()
    {
        _currentHp = _maxHp;
    }

    private void Start()
    {
        // 씬 시작 시 HUD에 초기 HP 알림
        GameEvents.GeneratorDamaged(_currentHp, _maxHp);
    }

    // === IDamageable 구현 ===

    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;

        _currentHp = Mathf.Max(0, _currentHp - damage);
        GameEvents.GeneratorDamaged(_currentHp, _maxHp);

        if (_currentHp == 0)
            Die();
    }

    public void Die()
    {
        GameManager.Instance.EndGame();
    }
}
