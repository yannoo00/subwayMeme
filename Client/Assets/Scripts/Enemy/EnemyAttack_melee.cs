using UnityEngine;

// 근접 단일 타격 공격
// 쿨타임마다 공격 범위 내 플레이어에게 데미지 적용
public class EnemyAttack_Melee : EnemyAttack
{
    private float _lastAttackTime = -Mathf.Infinity;


    protected override void Awake()
    {
        base.Awake();
    }


    public override void TryAttack()
    {
        if (_enemy?.Data == null) return;
        if (Time.time - _lastAttackTime < _enemy.Data.attackCooldown) return;

        _lastAttackTime = Time.time;
        PerformAttack();
    }


    private void PerformAttack()
    {
        _enemy.Anim?.PlayAttack();

        // OverlapSphere로 범위 내 모든 콜라이더를 검사하는 이유:
        // 직접 참조 대신 물리 쿼리를 사용하면 멀티플레이나 여러 플레이어 지원 시 확장이 쉬움
        Collider[] hits = Physics.OverlapSphere(transform.position, _enemy.Data.attackRange);

        Debug.Log($"[MeleeAttack] OverlapSphere 감지된 콜라이더 수: {hits.Length}");

        foreach (var hit in hits)
        {
            Debug.Log($"  - {hit.gameObject.name} / 태그: {hit.tag}");
            if (!hit.transform.root.CompareTag("Player") && !hit.CompareTag("Player")) continue;

            IDamageable target = hit.GetComponentInParent<IDamageable>();
            target?.TakeDamage(_enemy.Data.attackDamage);
            // 공격 1회당 1명만 타격 (범위 공격이 아닌 단일 타격)
            break;
        }
    }


    private void OnDrawGizmos()
    {
        if (_enemy?.Data == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _enemy.Data.attackRange);
    }
}
