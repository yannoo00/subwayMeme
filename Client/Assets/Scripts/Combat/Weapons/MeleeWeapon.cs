using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : WeaponBase
{
    [Header("Attack Settings")]
    [SerializeField] private Transform _attackPoint;
    [SerializeField] private LayerMask _targetLayer;


    [Header("Debug")]
    [SerializeField] private bool _showDebugGizmo = true;


    // 근접 전용 필드 접근용 typed 프로퍼티 (Inspector에서 MeleeWeaponData만 할당하는 전제)
    private MeleeWeaponData Data => (MeleeWeaponData)_weaponData;


    protected override void PerformAttack()
    {
        Vector3 attackPosition = _attackPoint != null
            ? _attackPoint.position
            : transform.position;

        Collider[] hits = Physics.OverlapSphere(attackPosition, Data.range, _targetLayer);

        var hitEnemyIds = new List<int>();

        foreach (Collider hit in hits)
        {
            if (!IsInAttackAngle(hit.transform)) continue;

            var enemy = hit.GetComponentInParent<Enemy>();
            bool isNetworkEnemy = enemy != null && enemy.NetworkId > 0;

            // 네트워크 적은 서버가 HP 계산 - 로컬 TakeDamage 호출 안 함
            if (!isNetworkEnemy && hit.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(Data.damage);

            if (isNetworkEnemy)
                hitEnemyIds.Add(enemy.NetworkId);

            if (!Data.isSplash) break;
        }

        // 히트 여부와 무관하게 전송 - 다른 플레이어에게 공격 애니메이션 동기화
        var pkt = new GameProto.C_Attack { WeaponId = 0, Damage = Data.damage };
        pkt.HitEnemyIds.AddRange(hitEnemyIds);
        NetworkManager.Instance.SendGame(GameProto.GamePacketId.CAttack, pkt);
    }


    private bool IsInAttackAngle(Transform target)
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle <= Data.attackAngle / 2f;
    }




    //Debug
    private void OnDrawGizmosSelected()
    {
        if (!_showDebugGizmo || _weaponData == null) return;

        Vector3 attackPosition = _attackPoint != null
            ? _attackPoint.position
            : transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPosition, _weaponData.range);
    }
}
