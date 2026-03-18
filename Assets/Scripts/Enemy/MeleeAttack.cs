using UnityEngine;

// 근접 단일 타격 공격
// 쿨타임마다 공격 범위 내 플레이어에게 데미지 적용
public class MeleeAttack : EnemyAttack
{
    private float _lastAttackTime = -Mathf.Infinity;


    protected override void Awake()
    {
        base.Awake();
    }


    public override void TryAttack()
    {
        if (enemy?.Data == null) return;
        if (Time.time - _lastAttackTime < enemy.Data.attackCooldown) return;

        _lastAttackTime = Time.time;
        PerformAttack();
    }


    private void PerformAttack()
    {
        animator?.PlayAttack();

        // OverlapSphere로 범위 내 모든 콜라이더를 검사하는 이유:
        // 직접 참조 대신 물리 쿼리를 사용하면 멀티플레이나 여러 플레이어 지원 시 확장이 쉬움
        Collider[] hits = Physics.OverlapSphere(transform.position, enemy.Data.attackRange);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            IDamageable target = hit.GetComponent<IDamageable>();
            target?.TakeDamage(enemy.Data.attackDamage);
            // 공격 1회당 1명만 타격 (범위 공격이 아닌 단일 타격)
            break;
        }
    }


    private void OnDrawGizmosSelected()
    {
        if (enemy?.Data == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemy.Data.attackRange);
    }
}
