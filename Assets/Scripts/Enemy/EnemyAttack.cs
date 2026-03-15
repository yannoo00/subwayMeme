using UnityEngine;

[RequireComponent(typeof(EnemyAI))]
public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackCooldown = 1.5f;

    private Animator _animator;
    private float _lastAttackTime = -Mathf.Infinity;


    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }


    // EnemyAI가 Attack 상태일 때 매 프레임 호출
    // 쿨타임이 지났을 때만 실제 공격 실행
    public void TryAttack()
    {
        if (Time.time - _lastAttackTime < _attackCooldown) return;

        _lastAttackTime = Time.time;
        PerformAttack();
    }


    private void PerformAttack()
    {
        _animator?.SetTrigger(AnimatorParams.Attack);

        // 공격 범위 내 플레이어 탐색 후 데미지 적용
        // OverlapSphere로 범위 내 모든 콜라이더를 검사하는 이유:
        // 직접 참조 대신 물리 쿼리를 사용하면 나중에 멀티플레이나 여러 플레이어 지원 시 확장이 쉬움
        Collider[] hits = Physics.OverlapSphere(transform.position, _attackRange);
        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target == null) continue;

            // 자기 자신(적)은 제외
            if (hit.gameObject == gameObject) continue;

            // PlayerStats만 데미지 받도록 레이어 또는 태그로 필터링
            if (!hit.CompareTag("Player")) continue;

            target.TakeDamage(_damage);
            // 공격 1회당 1명만 타격 (범위 공격이 아닌 단일 타격)
            break;
        }
    }


    // Scene 뷰에서 공격 범위 시각화 (개발 편의용)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
